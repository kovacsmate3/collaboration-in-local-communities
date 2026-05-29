using Backend.Application.TaskCompletion;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Xunit;

namespace backend.Tests;

public sealed class FlatTaskCompletionPointsCalculatorTests
{
    [Theory]
    [InlineData(CompensationType.Paid, FlatTaskCompletionPointsCalculator.BasePoints)]
    [InlineData(CompensationType.Points, FlatTaskCompletionPointsCalculator.BasePoints)]
    [InlineData(CompensationType.Barter, FlatTaskCompletionPointsCalculator.BasePoints)]
    [InlineData(
        CompensationType.Voluntary,
        FlatTaskCompletionPointsCalculator.BasePoints + FlatTaskCompletionPointsCalculator.VoluntaryBonusPoints)]
    public void Calculate_ReturnsExpectedAmountForCompensationType(CompensationType compensationType, int expected)
    {
        var calculator = new FlatTaskCompletionPointsCalculator();
        var task = new CommunityTask { CompensationType = compensationType };

        var amount = calculator.Calculate(task);

        Assert.Equal(expected, amount);
    }
}
