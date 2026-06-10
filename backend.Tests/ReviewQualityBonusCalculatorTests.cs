using Backend.Application.Reviews;
using Xunit;

namespace backend.Tests;

public sealed class ReviewQualityBonusCalculatorTests
{
    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 0)]
    [InlineData(3, 0)]
    [InlineData(4, ReviewQualityBonusCalculator.PointsPerStar)]
    [InlineData(5, 2 * ReviewQualityBonusCalculator.PointsPerStar)]
    public void Calculate_RewardsOnlyAboveNeutralRating(int rating, int expected)
    {
        var calculator = new ReviewQualityBonusCalculator();

        Assert.Equal(expected, calculator.Calculate(rating));
    }
}
