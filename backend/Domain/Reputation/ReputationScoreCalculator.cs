namespace Backend.Domain.Reputation;

/// <summary>
/// Computes the aggregate reputation score surfaced by the profile widget.
///
/// The formula is intentionally simple and stable so the same value can be
/// reproduced on the client when the score is missing from the API response:
///
///     score = round(completedTasks * 10 + averageRating * reviewCount * 2)
///
/// Inputs are clamped (rating to [0, 5], counts to non-negative) so malformed
/// data cannot produce negative or NaN scores.
/// </summary>
public static class ReputationScoreCalculator
{
    public const int CompletedTaskWeight = 10;
    public const int ReviewQualityWeight = 2;

    public static int Compute(decimal averageRating, int reviewCount, int completedTaskCount)
    {
        var clampedRating = averageRating switch
        {
            < 0m => 0m,
            > 5m => 5m,
            _ => averageRating,
        };

        var safeReviewCount = Math.Max(0, reviewCount);
        var safeCompletedCount = Math.Max(0, completedTaskCount);

        var score = (safeCompletedCount * (decimal)CompletedTaskWeight)
                    + (clampedRating * safeReviewCount * ReviewQualityWeight);

        return (int)Math.Round(score, MidpointRounding.AwayFromZero);
    }
}
