using Backend.Domain.Entities;

namespace Backend.Application.TaskCompletion;

/// <summary>
/// Calculates the platform points reward earned by the accepted helper when a task is completed.
/// Server-owned and deterministic so the client cannot influence the awarded amount.
/// </summary>
public interface ITaskCompletionPointsCalculator
{
    /// <summary>
    /// Returns the points to award the accepted helper for completing <paramref name="task"/>.
    /// Must return a positive integer; callers must not invoke this for non-completed tasks.
    /// </summary>
    /// <param name="task">The completed task whose accepted helper is being rewarded.</param>
    /// <returns>The number of platform points to credit to the helper.</returns>
    int Calculate(CommunityTask task);
}
