using System.Linq.Expressions;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Identity;

public sealed class RefreshTokenPruner(
    AppDbContext db,
    ILogger<RefreshTokenPruner> logger)
{
    public static Expression<Func<RefreshToken, bool>> CreatePrunablePredicate(DateTimeOffset cutoff)
    {
        return refreshToken =>
            refreshToken.ExpiresAt <= cutoff
            || (refreshToken.RevokedAt != null && refreshToken.RevokedAt <= cutoff);
    }

    public async Task<int> PruneAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var deletedRows = await db.RefreshTokens
            .Where(CreatePrunablePredicate(cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Pruned {DeletedRows} refresh tokens at {PrunedAt}; deleted tokens expired or revoked on or before {Cutoff}.",
                deletedRows,
                DateTimeOffset.UtcNow,
                cutoff);
        }

        return deletedRows;
    }
}
