using Backend.Domain.Entities;

namespace Backend.Application.TaskCompletion;

/// <summary>
/// Awards the helper's automatic points reward for a completed task.
/// Idempotent: a task/helper pair can only earn one <c>TaskCompletedReward</c> entry
/// (enforced both by an in-context pre-check and by the partial unique index
/// <c>ux_points_ledger_task_reward_once</c>).
/// </summary>
public interface ITaskCompletionRewardService
{
    /// <summary>
    /// Stages a <c>TaskCompletedReward</c> ledger entry for the accepted helper of
    /// <paramref name="task"/> if one does not already exist. The caller owns the
    /// transaction and must call <c>SaveChangesAsync</c> for the entry to be persisted.
    /// </summary>
    /// <param name="task">The task being completed. Must have <c>Status = Completed</c> and a non-null accepted helper.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The amount awarded, or <c>null</c> when the reward was already recorded.</returns>
    Task<int?> StageCompletionRewardAsync(CommunityTask task, CancellationToken cancellationToken);
}
