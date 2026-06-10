using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class ProfileReputationViewRefresher(
    AppDbContext db,
    ILogger<ProfileReputationViewRefresher> logger)
{
    // CONCURRENTLY rebuilds the view without taking the exclusive lock that
    // would otherwise block every read of profile_reputation_v during the
    // refresh. It requires the unique index on profile_id added alongside the
    // materialized view (see the MaterializeProfileReputationView migration)
    // and must run outside a transaction, so this executes on its own scope's
    // connection with no ambient transaction.
    public const string RefreshCommand =
        "REFRESH MATERIALIZED VIEW CONCURRENTLY analytics.profile_reputation_v";

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        await db.Database.ExecuteSqlRawAsync(RefreshCommand, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Refreshed materialized view analytics.profile_reputation_v at {RefreshedAt}.",
                DateTimeOffset.UtcNow);
        }
    }
}
