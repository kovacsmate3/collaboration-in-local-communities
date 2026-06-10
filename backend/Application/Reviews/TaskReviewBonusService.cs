using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backend.Application.Reviews;

public sealed class TaskReviewBonusService(
    AppDbContext db,
    IReviewQualityBonusCalculator calculator) : ITaskReviewBonusService
{
    public async Task<int?> StageReviewBonusAsync(
        CommunityTask task,
        Guid reviewerProfileId,
        int rating,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        // Only the seeker's review of the helper rewards the helper. A helper's review of
        // the seeker says nothing about the helper's own work, so it earns no bonus.
        if (reviewerProfileId != task.SeekerProfileId)
        {
            return null;
        }

        if (task.AcceptedHelperProfileId is not { } helperProfileId)
        {
            return null;
        }

        var bonus = calculator.Calculate(rating);
        if (bonus <= 0)
        {
            return null;
        }

        // Check the change tracker first so an entry staged earlier in this same DbContext
        // (but not yet saved) is treated as an existing award. AnyAsync would miss it.
        var stagedLocally = db.PointsLedger.Local.Any(
            entry => entry.TaskId == task.Id
                && entry.ProfileId == helperProfileId
                && entry.EntryType == PointEntryType.ReviewQualityBonus);

        if (stagedLocally)
        {
            return null;
        }

        var alreadyAwarded = await db.PointsLedger.AnyAsync(
            entry => entry.TaskId == task.Id
                && entry.ProfileId == helperProfileId
                && entry.EntryType == PointEntryType.ReviewQualityBonus,
            cancellationToken);

        if (alreadyAwarded)
        {
            return null;
        }

        db.PointsLedger.Add(new PointsLedgerEntry
        {
            ProfileId = helperProfileId,
            TaskId = task.Id,
            Amount = bonus,
            EntryType = PointEntryType.ReviewQualityBonus,
            Description = $"Quality bonus for a {rating}-star review on task {task.PublicCode}"
        });

        return bonus;
    }
}
