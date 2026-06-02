using System.Security.Claims;
using Backend.Application.TaskCompletion;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Tasks.Completion;

[ApiController]
[Route("api/tasks/{taskId:guid}")]
[Authorize]
public sealed class TaskCompletionController(
    AppDbContext db,
    ITaskCompletionRewardService rewardService) : ControllerBase
{
    [HttpPost("completion-submission")]
    public async Task<IActionResult> SubmitAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .Include(t => t.SeekerProfile)
            .Include(t => t.AcceptedHelperProfile)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.AcceptedHelperProfileId != profile.Id)
        {
            return Forbid();
        }

        if (task.Status != DomainTaskStatus.InProgress)
        {
            var submitDetail = task.Status == DomainTaskStatus.PendingApproval
                ? "This task is already awaiting the seeker's approval."
                : "Only in-progress tasks can be marked as done.";
            return Problem(
                title: "Task cannot be marked as done",
                detail: submitDetail,
                statusCode: StatusCodes.Status409Conflict);
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = task.Status;

        task.Status = DomainTaskStatus.PendingApproval;
        task.UpdatedAt = now;

        db.TaskCompletionConfirmations.Add(new TaskCompletionConfirmation
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ProfileId = profile.Id,
            ConfirmedAt = now
        });

        db.TaskStatusHistory.Add(new TaskStatusHistoryEntry
        {
            TaskId = task.Id,
            OldStatus = oldStatus,
            NewStatus = DomainTaskStatus.PendingApproval,
            ChangedByProfileId = profile.Id,
            Reason = "Helper submitted completion"
        });

        db.AddAuditEvent(
            profile.UserId,
            "task.completion_submitted",
            nameof(CommunityTask),
            task.Id,
            new
            {
                task.SeekerProfileId,
                HelperProfileId = profile.Id,
                SubmittedAt = now
            });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsDuplicateTaskCompletionConfirmation(ex))
        {
            return Problem(
                title: "Already submitted",
                detail: "You have already marked this task as done.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return Ok(TaskResponse.FromTask(task));
    }

    [HttpPost("completion-approval")]
    public async Task<IActionResult> ApproveAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .Include(t => t.SeekerProfile)
            .Include(t => t.AcceptedHelperProfile)
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.SeekerProfileId != profile.Id)
        {
            return Forbid();
        }

        if (task.Status != DomainTaskStatus.PendingApproval)
        {
            var approveDetail = task.Status == DomainTaskStatus.Completed
                ? "This task has already been marked as completed."
                : "Only tasks awaiting approval can be approved.";
            return Problem(
                title: "Task cannot be approved",
                detail: approveDetail,
                statusCode: StatusCodes.Status409Conflict);
        }

        if (task.AcceptedHelperProfileId is null)
        {
            return Problem(
                title: "Task is missing an accepted helper",
                detail: "Approval requires an accepted helper on the task.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = task.Status;

        task.Status = DomainTaskStatus.Completed;
        task.CompletedAt = now;
        task.UpdatedAt = now;

        // Credit the helper — the party who actually performed the work — with a
        // completed task so it feeds their reputation score. The seeker is not
        // credited: posting a task is not "work done" for reputation purposes,
        // mirroring how seeker-received reviews are excluded from the score.
        // This approval path runs at most once per task (any later call sees a
        // Completed status and 409s above), so the counter cannot double-count.
        if (task.AcceptedHelperProfile is not null)
        {
            task.AcceptedHelperProfile.CompletedTaskCount += 1;
            task.AcceptedHelperProfile.UpdatedAt = now;
        }

        var seekerAlreadyConfirmed = await db.TaskCompletionConfirmations.AnyAsync(
            confirmation => confirmation.TaskId == task.Id && confirmation.ProfileId == profile.Id,
            cancellationToken);

        if (!seekerAlreadyConfirmed)
        {
            db.TaskCompletionConfirmations.Add(new TaskCompletionConfirmation
            {
                Id = Guid.NewGuid(),
                TaskId = task.Id,
                ProfileId = profile.Id,
                ConfirmedAt = now
            });
        }

        var awardedPoints = await rewardService.StageCompletionRewardAsync(task, cancellationToken);

        db.TaskStatusHistory.Add(new TaskStatusHistoryEntry
        {
            TaskId = task.Id,
            OldStatus = oldStatus,
            NewStatus = DomainTaskStatus.Completed,
            ChangedByProfileId = profile.Id,
            Reason = "Seeker approved completion"
        });

        db.AddActivityEvent(
            profile.UserId,
            profile.Id,
            ActivityEventType.TaskCompleted,
            nameof(CommunityTask),
            task.Id,
            new
            {
                task.AcceptedHelperProfileId,
                AwardedPoints = awardedPoints
            });

        db.AddAuditEvent(
            profile.UserId,
            "task.completion_approved",
            nameof(CommunityTask),
            task.Id,
            new
            {
                task.SeekerProfileId,
                task.AcceptedHelperProfileId,
                ApprovedAt = now,
                AwardedPoints = awardedPoints
            });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsDuplicateTaskCompletionReward(ex))
        {
            // Belt-and-suspenders for the partial unique index. Treat as idempotent success
            // by re-reading current state.
            await db.Entry(task).ReloadAsync(cancellationToken);
            return Ok(TaskResponse.FromTask(task));
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsDuplicateTaskCompletionConfirmation(ex))
        {
            await db.Entry(task).ReloadAsync(cancellationToken);
            return Ok(TaskResponse.FromTask(task));
        }

        return Ok(TaskResponse.FromTask(task));
    }

    private async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            return null;
        }

        return await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
