using Backend.Domain.Entities;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Terms;

public static class TermsVersionQueries
{
    // Activates any scheduled version whose effectiveFrom has passed.
    // Call this before reading terms state so callers see the current picture.
    public static async Task ActivateScheduledAsync(
        this AppDbContext db,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        // A superseded (old) version still has its PublishedAt set, so we must
        // compare against the currently active version's PublishedAt to tell a
        // scheduled successor apart from an old one. Without this, the query
        // would reactivate any previously-active version on every call.
        var currentActivePublishedAt = await db.TermsVersions
            .Where(t => t.IsActive)
            .Select(t => (DateTimeOffset?)t.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var due = await db.TermsVersions
            .Where(t => !t.IsActive
                && t.PublishedAt != null
                && t.EffectiveFrom <= now
                && (currentActivePublishedAt == null || t.PublishedAt > currentActivePublishedAt))
            .OrderByDescending(t => t.EffectiveFrom)
            .ThenByDescending(t => t.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (due is null)
        {
            return;
        }

        // Deactivate in a separate SaveChanges before activating the new version.
        // The partial unique index (only one is_active=true row allowed) is checked
        // per statement, so both changes cannot land in the same batch.
        var currentlyActive = await db.TermsVersions
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var active in currentlyActive)
        {
            active.IsActive = false;
            active.UpdatedAt = now;
        }

        if (currentlyActive.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        try
        {
            due.IsActive = true;
            due.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsUniqueConstraintViolation(ex, "ux_terms_versions_single_active"))
        {
            // A concurrent request activated this version between our two saves; that's fine.
            db.Entry(due).State = EntityState.Detached;
        }
    }

    public static IQueryable<TermsVersion> CurrentCandidates(
        this IQueryable<TermsVersion> query,
        DateTimeOffset now)
    {
        return query.Where(terms => terms.IsActive && terms.EffectiveFrom <= now);
    }

    public static Task<TermsVersion?> GetCurrentAsync(
        this IQueryable<TermsVersion> query,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return query.CurrentCandidates(now)
            .OrderByDescending(terms => terms.EffectiveFrom)
            .ThenByDescending(terms => terms.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static Task<UserTermsAcceptance?> GetMajorMinorAcceptanceAsync(
        this IQueryable<UserTermsAcceptance> query,
        Guid userId,
        int majorVersion,
        int minorVersion,
        CancellationToken cancellationToken)
    {
        return query
            .Where(a => a.UserId == userId
                && a.TermsVersion.MajorVersion == majorVersion
                && a.TermsVersion.MinorVersion == minorVersion)
            .OrderByDescending(a => a.AcceptedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
