using Backend.Domain.Entities;
using Backend.Domain.Enums;

namespace Backend.Application.TaskCompletion;

/// <summary>
/// Completion reward policy: a base reward scaled by the task category's admin-configured
/// weight, plus a flat bonus for voluntary tasks. Voluntary tasks earn extra because the
/// helper receives no compensation outside the points economy, so the platform recognises
/// them more; paid and barter tasks earn only the weighted base because the helper is
/// already compensated by the seeker.
/// </summary>
public sealed class CategoryWeightedTaskCompletionPointsCalculator : ITaskCompletionPointsCalculator
{
    public const int BasePoints = 10;
    public const int VoluntaryBonusPoints = 5;
    public const int MinimumPoints = 1;

    public int Calculate(CommunityTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        // Fall back to the neutral weight when the category navigation is not loaded
        // or carries a non-positive value; real categories are constrained to > 0.
        var weight = task.Category is { PointsWeight: > 0m } category
            ? category.PointsWeight
            : Category.DefaultPointsWeight;

        var weighted = (int)Math.Round(BasePoints * weight, MidpointRounding.AwayFromZero);

        if (task.CompensationType == CompensationType.Voluntary)
        {
            weighted += VoluntaryBonusPoints;
        }

        return Math.Max(MinimumPoints, weighted);
    }
}
