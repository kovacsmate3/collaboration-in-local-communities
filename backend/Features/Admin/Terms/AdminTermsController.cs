using System.Security.Claims;
using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Features.Terms;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Admin.Terms;

[ApiController]
[Route("api/admin/terms")]
[Authorize(Roles = "Admin")]
public sealed class AdminTermsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var acceptanceCounts = await FetchLatestAcceptanceCountsAsync(cancellationToken);

        var versions = await db.TermsVersions
            .AsNoTracking()
            .OrderByDescending(t => t.EffectiveFrom)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        var items = versions.Select(t => new AdminTermsVersionListItem(
            t.Id,
            t.Version,
            t.MajorVersion,
            t.MinorVersion,
            t.PatchVersion,
            t.Title,
            t.IsActive,
            t.EffectiveFrom,
            t.CreatedAt,
            acceptanceCounts.GetValueOrDefault(t.Id, 0)));

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var terms = await db.TermsVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (terms is null)
        {
            return NotFound();
        }

        var counts = await FetchLatestAcceptanceCountsAsync(cancellationToken);

        return Ok(new AdminTermsVersionDetail(
            terms.Id,
            terms.Version,
            terms.MajorVersion,
            terms.MinorVersion,
            terms.PatchVersion,
            terms.Title,
            terms.Content,
            terms.ContentUrl,
            terms.IsActive,
            terms.EffectiveFrom,
            terms.CreatedAt,
            terms.UpdatedAt,
            counts.GetValueOrDefault(terms.Id, 0)));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        CreateTermsVersionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EffectiveFrom is not { } effectiveFrom)
        {
            ModelState.AddModelError(nameof(request.EffectiveFrom), "EffectiveFrom is required.");
            return ValidationProblem(ModelState);
        }

        var version = request.Version.Trim();
        var title = request.Title.Trim();

        if (!TermsVersionParser.TryParse(version, out var major, out var minor, out var patch))
        {
            ModelState.AddModelError(nameof(request.Version), "Version must be in x.y.z format (e.g. 1.0.0).");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(request.Title), "Title is required.");
            return ValidationProblem(ModelState);
        }

        var exists = await db.TermsVersions
            .AnyAsync(t => t.MajorVersion == major && t.MinorVersion == minor && t.PatchVersion == patch, cancellationToken);

        if (exists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate version",
                Detail = $"Version {version} already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var terms = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = TermsVersionParser.Format(major, minor, patch),
            MajorVersion = major,
            MinorVersion = minor,
            PatchVersion = patch,
            Title = title,
            Content = request.Content,
            ContentUrl = request.ContentUrl?.Trim(),
            IsActive = false,
            EffectiveFrom = effectiveFrom,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.TermsVersions.Add(terms);
        AddAuditEvent("admin.terms_created", terms.Id, new { terms.Version, terms.Title });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresExceptionHelpers.IsUniqueConstraintViolation(exception, "ux_terms_versions_version_triple"))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate version",
                Detail = $"Version {version} already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var acceptanceCount = 0;
        return CreatedAtAction(nameof(GetByIdAsync), new { id = terms.Id }, new AdminTermsVersionDetail(
            terms.Id,
            terms.Version,
            terms.MajorVersion,
            terms.MinorVersion,
            terms.PatchVersion,
            terms.Title,
            terms.Content,
            terms.ContentUrl,
            terms.IsActive,
            terms.EffectiveFrom,
            terms.CreatedAt,
            terms.UpdatedAt,
            acceptanceCount));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid id,
        UpdateTermsVersionRequest request,
        CancellationToken cancellationToken)
    {
        var terms = await db.TermsVersions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (terms is null)
        {
            return NotFound();
        }

        if (terms.IsActive)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Cannot edit published version",
                Detail = "The active (published) terms version cannot be edited. Create a new draft version instead.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (request.EffectiveFrom is not { } effectiveFrom)
        {
            ModelState.AddModelError(nameof(request.EffectiveFrom), "EffectiveFrom is required.");
            return ValidationProblem(ModelState);
        }

        var version = request.Version.Trim();
        var title = request.Title.Trim();

        if (!TermsVersionParser.TryParse(version, out var major, out var minor, out var patch))
        {
            ModelState.AddModelError(nameof(request.Version), "Version must be in x.y.z format (e.g. 1.0.0).");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(request.Title), "Title is required.");
            return ValidationProblem(ModelState);
        }

        var duplicateExists = await db.TermsVersions
            .AnyAsync(t => t.Id != id && t.MajorVersion == major && t.MinorVersion == minor && t.PatchVersion == patch, cancellationToken);

        if (duplicateExists)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Duplicate version",
                Detail = $"Version {version} already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        terms.Version = TermsVersionParser.Format(major, minor, patch);
        terms.MajorVersion = major;
        terms.MinorVersion = minor;
        terms.PatchVersion = patch;
        terms.Title = title;
        terms.Content = request.Content;
        terms.ContentUrl = request.ContentUrl?.Trim();
        terms.EffectiveFrom = effectiveFrom;
        terms.UpdatedAt = DateTimeOffset.UtcNow;

        AddAuditEvent("admin.terms_updated", terms.Id, new { terms.Version, terms.Title });
        await db.SaveChangesAsync(cancellationToken);

        var counts = await FetchLatestAcceptanceCountsAsync(cancellationToken);

        return Ok(new AdminTermsVersionDetail(
            terms.Id,
            terms.Version,
            terms.MajorVersion,
            terms.MinorVersion,
            terms.PatchVersion,
            terms.Title,
            terms.Content,
            terms.ContentUrl,
            terms.IsActive,
            terms.EffectiveFrom,
            terms.CreatedAt,
            terms.UpdatedAt,
            counts.GetValueOrDefault(id, 0)));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var terms = await db.TermsVersions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (terms is null)
        {
            return NotFound();
        }

        if (terms.IsActive)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Already published",
                Detail = "This terms version is already the active version.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (string.IsNullOrWhiteSpace(terms.Content))
        {
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "Content required",
                Detail = "Terms content must be set before publishing.",
                Status = StatusCodes.Status422UnprocessableEntity
            });
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var currentlyActive = await db.TermsVersions
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var active in currentlyActive)
        {
            active.IsActive = false;
            active.UpdatedAt = DateTimeOffset.UtcNow;
        }

        terms.IsActive = true;
        if (terms.EffectiveFrom > DateTimeOffset.UtcNow)
        {
            // Keep the future effective date as-is — it will become current when reached.
        }
        else
        {
            terms.EffectiveFrom = DateTimeOffset.UtcNow;
        }

        terms.UpdatedAt = DateTimeOffset.UtcNow;

        AddAuditEvent("admin.terms_published", terms.Id, new { terms.Version, terms.Title });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var counts = await FetchLatestAcceptanceCountsAsync(cancellationToken);

        return Ok(new AdminTermsVersionDetail(
            terms.Id,
            terms.Version,
            terms.MajorVersion,
            terms.MinorVersion,
            terms.PatchVersion,
            terms.Title,
            terms.Content,
            terms.ContentUrl,
            terms.IsActive,
            terms.EffectiveFrom,
            terms.CreatedAt,
            terms.UpdatedAt,
            counts.GetValueOrDefault(id, 0)));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var terms = await db.TermsVersions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (terms is null)
        {
            return NotFound();
        }

        if (terms.IsActive)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Cannot delete active version",
                Detail = "The published terms version cannot be deleted. Publish a new version to replace it.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var hasAcceptances = await db.UserTermsAcceptances
            .AnyAsync(a => a.TermsVersionId == id, cancellationToken);

        if (hasAcceptances)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Version has acceptances",
                Detail = "This terms version has been accepted by users and cannot be deleted.",
                Status = StatusCodes.Status409Conflict
            });
        }

        AddAuditEvent("admin.terms_deleted", terms.Id, new { terms.Version, terms.Title });
        db.TermsVersions.Remove(terms);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Guid? GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private void AddAuditEvent(string eventType, Guid entityId, object? payload)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = GetActorUserId(),
            EventType = eventType,
            EntityType = "TermsVersion",
            EntityId = entityId,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }

    // For each user count only their most recent acceptance, so publishing a new version
    // "moves" users from the old version's count to the new one as they re-accept.
    private Task<Dictionary<Guid, int>> FetchLatestAcceptanceCountsAsync(CancellationToken ct) =>
        db.Database
            .SqlQuery<VersionCount>($"""
                SELECT terms_version_id AS "TermsVersionId", CAST(COUNT(*) AS INTEGER) AS "Count"
                FROM (
                    SELECT DISTINCT ON (user_id) terms_version_id
                    FROM data.user_terms_acceptances
                    ORDER BY user_id, accepted_at DESC
                ) latest
                GROUP BY terms_version_id
                """)
            .ToDictionaryAsync(x => x.TermsVersionId, x => x.Count, ct);

    private sealed record VersionCount(Guid TermsVersionId, int Count);
}
