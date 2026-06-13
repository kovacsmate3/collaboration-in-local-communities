namespace Backend.Application.Reviews;

/// <summary>
/// Awards points for star ratings above a neutral baseline: each star over the baseline is
/// worth <see cref="PointsPerStar"/> points, so good work is rewarded while ratings at or
/// below the baseline yield nothing (never a penalty).
/// </summary>
public sealed class ReviewQualityBonusCalculator : IReviewQualityBonusCalculator
{
    public const int NeutralRating = 3;
    public const int PointsPerStar = 3;

    public int Calculate(int rating)
    {
        return Math.Max(0, rating - NeutralRating) * PointsPerStar;
    }
}
