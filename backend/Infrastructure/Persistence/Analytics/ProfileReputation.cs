using Backend.Domain.Reputation;

namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class ProfileReputation
{
    public Guid ProfileId { get; set; }
    public decimal AverageRating { get; set; }
    public long ReviewCount { get; set; }
    public long CompletedTaskCount { get; set; }

    /// <summary>
    /// Gets the aggregate reputation score derived from the other three fields via
    /// <see cref="ReputationScoreCalculator"/>. Computed on demand so it
    /// stays in sync with whatever the persisted aggregates are.
    /// </summary>
    public int Score => ReputationScoreCalculator.Compute(
        AverageRating,
        checked((int)Math.Min(ReviewCount, int.MaxValue)),
        checked((int)Math.Min(CompletedTaskCount, int.MaxValue)));
}
