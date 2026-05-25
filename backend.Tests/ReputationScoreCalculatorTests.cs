using Backend.Domain.Reputation;
using Xunit;

namespace backend.Tests;

public sealed class ReputationScoreCalculatorTests
{
    [Fact]
    public void Compute_ReturnsZero_ForBrandNewProfile()
    {
        var score = ReputationScoreCalculator.Compute(0m, 0, 0);
        Assert.Equal(0, score);
    }

    [Fact]
    public void Compute_AddsCompletedTaskWeight_WhenNoReviews()
    {
        // 9 completed × 10 + 0 × 0 × 2 = 90
        var score = ReputationScoreCalculator.Compute(0m, 0, 9);
        Assert.Equal(90, score);
    }

    [Fact]
    public void Compute_RewardsReviewQuality()
    {
        // 0 completed × 10 + 4.5 × 4 × 2 = 36
        var score = ReputationScoreCalculator.Compute(4.5m, 4, 0);
        Assert.Equal(36, score);
    }

    [Fact]
    public void Compute_CombinesCompletedAndReviewComponents()
    {
        // 9 × 10 + 4.8 × 12 × 2 = 90 + 115.2 → 205 (rounded to the nearest integer)
        var score = ReputationScoreCalculator.Compute(4.8m, 12, 9);
        Assert.Equal(205, score);
    }

    [Theory]
    [InlineData(-1, 0, 0)] // negative rating clamped
    [InlineData(6.5, 4, 2)] // rating above 5 clamped
    public void Compute_ClampsRatingToZeroFive(decimal rating, int reviewCount, int completed)
    {
        var score = ReputationScoreCalculator.Compute(rating, reviewCount, completed);
        // Clamping must keep score non-negative and finite
        Assert.True(score >= 0);
    }

    [Fact]
    public void Compute_TreatsRatingAboveFive_AsFive()
    {
        // Above-range rating should be clamped to 5
        var clamped = ReputationScoreCalculator.Compute(5m, 10, 0);
        var raw = ReputationScoreCalculator.Compute(7m, 10, 0);
        Assert.Equal(clamped, raw);
    }

    [Fact]
    public void Compute_TreatsNegativeRating_AsZero()
    {
        var withZero = ReputationScoreCalculator.Compute(0m, 10, 3);
        var withNegative = ReputationScoreCalculator.Compute(-2m, 10, 3);
        Assert.Equal(withZero, withNegative);
    }

    [Fact]
    public void Compute_TreatsNegativeCounts_AsZero()
    {
        var fromZero = ReputationScoreCalculator.Compute(4m, 0, 0);
        var fromNegative = ReputationScoreCalculator.Compute(4m, -5, -3);
        Assert.Equal(fromZero, fromNegative);
        Assert.Equal(0, fromNegative);
    }

    [Fact]
    public void Compute_RoundsHalfAwayFromZero()
    {
        // 4.5 × 1 × 2 = 9.0 (no rounding); use a case that lands on .5
        // 0.5 × 1 × 2 = 1.0 → still integer; force .5 via 0.25 × 2 × 2 = 1.0 — also integer
        // The current formula always lands on integer or 0.5 multiples; pick one:
        // 4.75 × 1 × 2 = 9.5 → 10 with AwayFromZero
        var score = ReputationScoreCalculator.Compute(4.75m, 1, 0);
        Assert.Equal(10, score);
    }

    [Fact]
    public void Compute_HandlesLargeCounts_WithoutOverflow()
    {
        var score = ReputationScoreCalculator.Compute(5m, 100_000, 50_000);
        // 50000 × 10 + 5 × 100000 × 2 = 500000 + 1000000 = 1_500_000
        Assert.Equal(1_500_000, score);
    }
}
