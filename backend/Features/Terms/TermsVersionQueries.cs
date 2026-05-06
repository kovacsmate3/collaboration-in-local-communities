using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Terms;

public static class TermsVersionQueries
{
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
}
