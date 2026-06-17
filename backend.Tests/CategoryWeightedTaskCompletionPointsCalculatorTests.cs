using Backend.Application.TaskCompletion;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Xunit;

namespace backend.Tests;

public sealed class CategoryWeightedTaskCompletionPointsCalculatorTests
{
    private const int Base = CategoryWeightedTaskCompletionPointsCalculator.BasePoints;
    private const int VoluntaryBonus = CategoryWeightedTaskCompletionPointsCalculator.VoluntaryBonusPoints;

    [Theory]
    [InlineData(CompensationType.Paid, Base)]
    [InlineData(CompensationType.Points, Base)]
    [InlineData(CompensationType.Barter, Base)]
    [InlineData(CompensationType.Voluntary, Base + VoluntaryBonus)]
    public void Calculate_AtNeutralWeight_MatchesBaseAndVoluntaryBonus(
        CompensationType compensationType,
        int expected)
    {
        var calculator = new CategoryWeightedTaskCompletionPointsCalculator();
        var task = new CommunityTask
        {
            CompensationType = compensationType,
            Category = new Category { PointsWeight = 1.0m }
        };

        Assert.Equal(expected, calculator.Calculate(task));
    }

    [Theory]
    [InlineData(1.5, CompensationType.Paid, 15)]
    [InlineData(1.5, CompensationType.Voluntary, 20)]
    [InlineData(1.2, CompensationType.Paid, 12)]
    [InlineData(0.8, CompensationType.Paid, 8)]
    public void Calculate_ScalesBaseByCategoryWeight_ThenAddsVoluntaryBonus(
        decimal weight,
        CompensationType compensationType,
        int expected)
    {
        var calculator = new CategoryWeightedTaskCompletionPointsCalculator();
        var task = new CommunityTask
        {
            CompensationType = compensationType,
            Category = new Category { PointsWeight = weight }
        };

        Assert.Equal(expected, calculator.Calculate(task));
    }

    [Theory]
    [InlineData(1.25, 13)] // 12.5 rounds away from zero
    [InlineData(1.14, 11)] // 11.4 rounds down
    public void Calculate_RoundsHalfAwayFromZero(decimal weight, int expected)
    {
        var calculator = new CategoryWeightedTaskCompletionPointsCalculator();
        var task = new CommunityTask
        {
            CompensationType = CompensationType.Paid,
            Category = new Category { PointsWeight = weight }
        };

        Assert.Equal(expected, calculator.Calculate(task));
    }

    [Fact]
    public void Calculate_NeverReturnsLessThanMinimum()
    {
        var calculator = new CategoryWeightedTaskCompletionPointsCalculator();
        var task = new CommunityTask
        {
            CompensationType = CompensationType.Paid,
            Category = new Category { PointsWeight = 0.04m } // 0.4 rounds to 0, floored to minimum
        };

        Assert.Equal(CategoryWeightedTaskCompletionPointsCalculator.MinimumPoints, calculator.Calculate(task));
    }

    [Theory]
    [InlineData(CompensationType.Paid, Base)]
    [InlineData(CompensationType.Voluntary, Base + VoluntaryBonus)]
    public void Calculate_FallsBackToNeutralWeight_WhenCategoryNotLoaded(
        CompensationType compensationType,
        int expected)
    {
        var calculator = new CategoryWeightedTaskCompletionPointsCalculator();
        var task = new CommunityTask { CompensationType = compensationType };

        Assert.Equal(expected, calculator.Calculate(task));
    }
}
