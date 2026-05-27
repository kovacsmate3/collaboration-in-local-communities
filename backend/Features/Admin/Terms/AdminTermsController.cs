using System.Security.Claims;
using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Features.Terms;
using Backend.Infrastructure.Persistence;
using Ganss.Xss;
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
        await db.ActivateScheduledAsync(DateTimeOffset.UtcNow, cancellationToken);

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
            t.PublishedAt,
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
            terms.PublishedAt,
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
            Content = request.Content is not null ? SanitizeContent(request.Content) : null,
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

        return CreatedAtAction("GetById", new { id = terms.Id }, new AdminTermsVersionDetail(
            terms.Id,
            terms.Version,
            terms.MajorVersion,
            terms.MinorVersion,
            terms.PatchVersion,
            terms.Title,
            terms.Content,
            terms.ContentUrl,
            terms.IsActive,
            terms.PublishedAt,
            terms.EffectiveFrom,
            terms.CreatedAt,
            terms.UpdatedAt,
            0));
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
        terms.Content = request.Content is not null ? SanitizeContent(request.Content) : null;
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
            terms.PublishedAt,
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
            ModelState.AddModelError("content", "Terms content must be set before publishing.");
            return ValidationProblem(ModelState);
        }

        // Only the most recently superseded version may be republished (one-step rollback).
        if (terms.PublishedAt != null)
        {
            var lastSupersededId = await db.TermsVersions
                .Where(t => !t.IsActive && t.PublishedAt != null)
                .OrderByDescending(t => t.PublishedAt)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (terms.Id != lastSupersededId)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Republish not allowed",
                    Detail = "Only the most recently superseded version can be republished.",
                    Status = StatusCodes.Status409Conflict
                });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var isScheduled = terms.EffectiveFrom > now;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Cancel any other pending scheduled versions.
        var otherScheduled = await db.TermsVersions
            .Where(t => !t.IsActive && t.PublishedAt != null && t.EffectiveFrom > now && t.Id != id)
            .ToListAsync(cancellationToken);

        foreach (var s in otherScheduled)
        {
            s.PublishedAt = null;
            s.UpdatedAt = now;
        }

        if (isScheduled)
        {
            // Future-dated: mark as scheduled but keep IsActive = false.
            // Lazy activation will flip it when effectiveFrom is reached.
            terms.PublishedAt = now;
            terms.UpdatedAt = now;
            AddAuditEvent("admin.terms_published", terms.Id, new { terms.Version, terms.Title, IsScheduled = isScheduled });
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Immediate activation: deactivate existing active version(s) first in a
            // separate SaveChanges so the partial unique index (one is_active=true row)
            // is satisfied before we mark the new version active.
            var currentlyActive = await db.TermsVersions
                .Where(t => t.IsActive)
                .ToListAsync(cancellationToken);

            var isRollback = terms.PublishedAt != null;
            foreach (var active in currentlyActive)
            {
                active.IsActive = false;

                // On rollback, demote the superseded version to draft so it can be
                // edited and republished rather than being stuck as "Old".
                if (isRollback)
                {
                    active.PublishedAt = null;
                }

                active.UpdatedAt = now;
            }

            if (currentlyActive.Count > 0)
            {
                await db.SaveChangesAsync(cancellationToken);
            }

            terms.IsActive = true;
            terms.PublishedAt = now;

            // Clamp effectiveFrom to now if it's in the past (republish case).
            if (terms.EffectiveFrom < now)
            {
                terms.EffectiveFrom = now;
            }

            terms.UpdatedAt = now;
            AddAuditEvent("admin.terms_published", terms.Id, new { terms.Version, terms.Title, IsScheduled = isScheduled });
            await db.SaveChangesAsync(cancellationToken);
        }

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
            terms.PublishedAt,
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

    private static string SanitizeContent(string html) => new HtmlSanitizer().Sanitize(html);

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
