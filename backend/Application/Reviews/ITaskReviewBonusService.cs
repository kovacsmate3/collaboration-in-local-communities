using Backend.Domain.Entities;

namespace Backend.Application.Reviews;

/// <summary>
/// Stages the helper's review-quality points bonus when a seeker reviews them on a completed task.
/// Idempotent: a task/helper pair can only earn one <c>ReviewQualityBonus</c> entry (enforced both
/// by an in-context pre-check and by the partial unique index <c>ux_points_ledger_task_review_bonus_once</c>).
/// </summary>
public interface ITaskReviewBonusService
{
    /// <summary>
    /// Stages a <c>ReviewQualityBonus</c> ledger entry for the accepted helper of
    /// <paramref name="task"/> when <paramref name="reviewerProfileId"/> is the seeker and the
    /// rating earns a positive bonus. The caller owns the transaction and must call
    /// <c>SaveChangesAsync</c> for the entry to be persisted.
    /// </summary>
    /// <param name="task">The completed task being reviewed.</param>
    /// <param name="reviewerProfileId">The profile submitting the review.</param>
    /// <param name="rating">The star rating given (1-5).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The bonus amount staged, or <c>null</c> when no bonus applies (reviewer is not the seeker,
    /// the task has no accepted helper, the rating earns nothing, or a bonus already exists).
    /// </returns>
    Task<int?> StageReviewBonusAsync(
        CommunityTask task,
        Guid reviewerProfileId,
        int rating,
        CancellationToken cancellationToken);
}
