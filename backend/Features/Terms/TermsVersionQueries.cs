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
        var due = await db.TermsVersions
            .Where(t => !t.IsActive && t.PublishedAt != null && t.EffectiveFrom <= now)
            .OrderByDescending(t => t.EffectiveFrom)
            .ThenByDescending(t => t.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (due is null)
        {
            return;
        }

        var currentlyActive = await db.TermsVersions
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var active in currentlyActive)
        {
            active.IsActive = false;
            active.UpdatedAt = now;
        }

        due.IsActive = true;
        due.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
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
