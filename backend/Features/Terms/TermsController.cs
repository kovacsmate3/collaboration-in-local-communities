using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Terms;

[ApiController]
[Route("api/terms")]
[Authorize]
public sealed class TermsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Get the currently active terms version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with the active terms version (id, version, title, contentUrl, effectiveFrom).
    /// 404 Not Found if no active terms version exists.
    /// </returns>
    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTermsAsync(CancellationToken cancellationToken)
    {
        var terms = await db.TermsVersions
            .AsNoTracking()
            .GetCurrentAsync(DateTimeOffset.UtcNow, cancellationToken);

        if (terms is null)
        {
            return NotFound();
        }

        return Ok(new ActiveTermsResponse(
            terms.Id,
            terms.Version,
            terms.Title,
            terms.ContentUrl,
            terms.EffectiveFrom));
    }

    /// <summary>
    /// Record the current user's acceptance of a specific terms version.
    /// Idempotent: re-accepting the same version is a no-op.
    /// </summary>
    /// <param name="request">The terms version ID to accept.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK on success (new or existing acceptance).
    /// 400 Bad Request if the specified terms version does not exist.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpPost("accept")]
    public async Task<IActionResult> AcceptTermsAsync(
        AcceptTermsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        if (request.TermsVersionId is not { } termsVersionId)
        {
            ModelState.AddModelError(nameof(request.TermsVersionId), "Terms version ID is required.");
            return ValidationProblem(ModelState);
        }

        var activeTerms = await db.TermsVersions
            .AsNoTracking()
            .GetCurrentAsync(DateTimeOffset.UtcNow, cancellationToken);

        if (activeTerms is null)
        {
            return BadRequest("No active terms version exists.");
        }

        if (activeTerms.Id != termsVersionId)
        {
            return BadRequest("Only the current active terms version can be accepted.");
        }

        var alreadyAccepted = await db.UserTermsAcceptances
            .AsNoTracking()
            .AnyAsync(a => a.UserId == userIdGuid && a.TermsVersionId == termsVersionId, cancellationToken);

        if (alreadyAccepted)
        {
            return Ok();
        }

        db.UserTermsAcceptances.Add(new UserTermsAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userIdGuid,
            TermsVersionId = termsVersionId,
            AcceptedAt = DateTimeOffset.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelpers.IsDuplicateUserTermsAcceptance(exception))
        {
            return Ok();
        }

        return Ok();
    }

    /// <summary>
    /// Return whether the current user has accepted the active terms version.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with hasAccepted and acceptedAt.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpGet("acceptance")]
    public async Task<IActionResult> GetAcceptanceAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var activeTerms = await db.TermsVersions
            .AsNoTracking()
            .GetCurrentAsync(DateTimeOffset.UtcNow, cancellationToken);

        if (activeTerms is null)
        {
            return Ok(new TermsAcceptanceResponse(false, null));
        }

        var acceptance = await db.UserTermsAcceptances
            .AsNoTracking()
            .Where(a => a.UserId == userIdGuid && a.TermsVersionId == activeTerms.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new TermsAcceptanceResponse(
            acceptance is not null,
            acceptance?.AcceptedAt));
    }
}
