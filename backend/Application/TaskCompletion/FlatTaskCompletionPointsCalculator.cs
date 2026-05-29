using Backend.Domain.Entities;
using Backend.Domain.Enums;

namespace Backend.Application.TaskCompletion;

/// <summary>
/// Flat v1 algorithm: every completed task awards a base reward, with a small bonus for
/// voluntary tasks (where the seeker offers no other compensation, so the platform
/// recognises the helper more). Paid and barter tasks award only the base reward because
/// the helper already receives compensation outside the points economy.
/// </summary>
public sealed class FlatTaskCompletionPointsCalculator : ITaskCompletionPointsCalculator
{
    public const int BasePoints = 10;
    public const int VoluntaryBonusPoints = 5;

    public int Calculate(CommunityTask task)
    {
        ArgumentNullException.ThrowIfNull(task);

        var points = BasePoints;
        if (task.CompensationType == CompensationType.Voluntary)
        {
            points += VoluntaryBonusPoints;
        }

        return points;
    }
}
