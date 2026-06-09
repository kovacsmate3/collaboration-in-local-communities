using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Profiles;

public sealed partial class ProfilesController
{
    private const int DefaultLedgerPageSize = 20;
    private const int MaxLedgerPageSize = 100;

    /// <summary>
    /// Get the current authenticated user's points balance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with the live balance (sum of all ledger entries, 0 when none).
    /// 404 Not Found if the user has no profile.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpGet("me/points-balance")]
    public async Task<IActionResult> GetOwnPointsBalanceAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var profileId = await db.Profiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userIdGuid)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (profileId is not { } resolvedProfileId)
        {
            return NotFound();
        }

        // Summed live from the immutable ledger so the balance is always correct,
        // regardless of how many reward/adjustment/redemption entries exist. Cast
        // to long before summing so a large history cannot overflow an int total.
        var balance = await db.PointsLedger
            .AsNoTracking()
            .Where(entry => entry.ProfileId == resolvedProfileId)
            .SumAsync(entry => (long)entry.Amount, cancellationToken);

        return Ok(new PointsBalanceResponse(resolvedProfileId, balance));
    }

    /// <summary>
    /// Get the current authenticated user's points ledger history, paginated.
    /// </summary>
    /// <param name="page">1-based page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with a paginated envelope of ledger entries ordered newest first.
    /// 400 Bad Request if pagination parameters are invalid.
    /// 404 Not Found if the user has no profile.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpGet("me/points-ledger")]
    public async Task<IActionResult> GetOwnPointsLedgerAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultLedgerPageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            ModelState.AddModelError(nameof(page), "Page must be at least 1.");
            return ValidationProblem(ModelState);
        }

        if (pageSize < 1 || pageSize > MaxLedgerPageSize)
        {
            ModelState.AddModelError(nameof(pageSize), $"PageSize must be between 1 and {MaxLedgerPageSize}.");
            return ValidationProblem(ModelState);
        }

        // Compute the offset as long so extreme inputs (e.g. page=int.MaxValue)
        // don't silently overflow to a negative int before reaching Skip().
        var offset = (long)(page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            ModelState.AddModelError(nameof(page), "Page is too large.");
            return ValidationProblem(ModelState);
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var profileId = await db.Profiles
            .AsNoTracking()
            .Where(profile => profile.UserId == userIdGuid)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (profileId is not { } resolvedProfileId)
        {
            return NotFound();
        }

        var query = db.PointsLedger
            .AsNoTracking()
            .Where(entry => entry.ProfileId == resolvedProfileId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(entry => entry.CreatedAt)
            .ThenByDescending(entry => entry.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .Select(entry => new PointsLedgerEntryResponse(
                entry.Id,
                entry.Amount,
                entry.EntryType.ToString(),
                entry.Description,
                entry.TaskId,
                entry.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var response = new PointsLedgerPagedResponse(items, totalCount, page, pageSize, totalPages);

        return Ok(response);
    }
}
