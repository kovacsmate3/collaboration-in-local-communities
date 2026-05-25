using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Application.TaskCompletion;

public sealed class TaskCompletionRewardService(
    AppDbContext db,
    ITaskCompletionPointsCalculator calculator) : ITaskCompletionRewardService
{
    public async Task<int?> StageCompletionRewardAsync(CommunityTask task, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (task.Status != DomainTaskStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Cannot award completion points for task {task.Id}: status is {task.Status}, not Completed.");
        }

        if (task.AcceptedHelperProfileId is not { } helperProfileId)
        {
            throw new InvalidOperationException(
                $"Cannot award completion points for task {task.Id}: no accepted helper.");
        }

        var alreadyAwarded = await db.PointsLedger.AnyAsync(
            entry => entry.TaskId == task.Id
                && entry.ProfileId == helperProfileId
                && entry.EntryType == PointEntryType.TaskCompletedReward,
            cancellationToken);

        if (alreadyAwarded)
        {
            return null;
        }

        var amount = calculator.Calculate(task);

        db.PointsLedger.Add(new PointsLedgerEntry
        {
            ProfileId = helperProfileId,
            TaskId = task.Id,
            Amount = amount,
            EntryType = PointEntryType.TaskCompletedReward,
            Description = $"Reward for completing task {task.PublicCode}"
        });

        return amount;
    }
}
