using System.Linq.Expressions;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Identity;

public sealed class RefreshTokenPruner(
    AppDbContext db,
    ILogger<RefreshTokenPruner> logger)
{
    public static Expression<Func<RefreshToken, bool>> CreateExpiredPredicate(DateTimeOffset cutoff)
    {
        return refreshToken => refreshToken.ExpiresAt <= cutoff;
    }

    public static Expression<Func<RefreshToken, bool>> CreateRevokedPredicate(DateTimeOffset cutoff)
    {
        return refreshToken => refreshToken.RevokedAt != null && refreshToken.RevokedAt <= cutoff;
    }

    public async Task<int> PruneAsync(DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var expiredRowsDeleted = await db.RefreshTokens
            .Where(CreateExpiredPredicate(cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        var revokedRowsDeleted = await db.RefreshTokens
            .Where(CreateRevokedPredicate(cutoff))
            .ExecuteDeleteAsync(cancellationToken);

        var deletedRows = expiredRowsDeleted + revokedRowsDeleted;

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Pruned {DeletedRows} refresh tokens at {PrunedAt}; deleted {ExpiredRowsDeleted} expired and {RevokedRowsDeleted} revoked tokens on or before {Cutoff}.",
                deletedRows,
                DateTimeOffset.UtcNow,
                expiredRowsDeleted,
                revokedRowsDeleted,
                cutoff);
        }

        return deletedRows;
    }
}
