using Backend.Common;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.Tasks;
using Backend.Features.Conversations;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Tasks.Applications;

[ApiController]
[Route("api/tasks/{taskId:guid}/applications")]
[Authorize]
public sealed partial class TaskApplicationsController(
    AppDbContext db,
    CosmosMessageService cosmosMessages,
    IHubContext<ChatHub> chatHub) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ApplyAsync(
        Guid taskId,
        ApplyToTaskRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.Status != DomainTaskStatus.Open)
        {
            return Problem(
                title: "Task is not open",
                detail: "Applications can only be submitted for tasks with Open status.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (task.SeekerProfileId == profile.Id)
        {
            return Problem(
                title: "Cannot apply to own task",
                detail: "The task seeker cannot apply to their own task.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var application = new TaskApplication
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            HelperProfileId = profile.Id,
            Message = StringUtilities.Normalize(request.Message)
        };

        db.TaskApplications.Add(application);
        db.AddActivityEvent(
            profile.UserId,
            profile.Id,
            ActivityEventType.TaskApplicationSubmitted,
            nameof(TaskApplication),
            application.Id,
            new { application.TaskId, MessageLength = application.Message?.Length ?? 0 });
        db.AddAuditEvent(
            profile.UserId,
            "task_application.submitted",
            nameof(TaskApplication),
            application.Id,
            new
            {
                application.TaskId,
                application.HelperProfileId,
                MessageLength = application.Message?.Length ?? 0
            });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsUniqueConstraintViolation(ex, "ux_task_applications_task_helper"))
        {
            return Problem(
                title: "Already applied",
                detail: "You have already applied to this task.",
                statusCode: StatusCodes.Status409Conflict);
        }

        // Create the conversation thread immediately on apply.
        var conversation = new TaskConversation
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            SeekerProfileId = task.SeekerProfileId,
            HelperProfileId = profile.Id,
            CosmosConversationId = Guid.NewGuid().ToString()
        };
        db.TaskConversations.Add(conversation);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsDuplicateTaskConversation(ex))
        {
            // A conversation already exists (e.g. from a prior StartConversation call).
            db.Entry(conversation).State = EntityState.Detached;
            conversation = await db.TaskConversations
                .FirstAsync(c => c.TaskId == taskId && c.HelperProfileId == profile.Id, cancellationToken);
        }

        // Send the application message as the first real message in the thread.
        if (!string.IsNullOrWhiteSpace(application.Message))
        {
            var document = new MessageDocument
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversation.Id.ToString(),
                SenderProfileId = profile.Id.ToString(),
                SenderDisplayName = profile.DisplayName,
                Content = application.Message,
                SentAt = DateTimeOffset.UtcNow,
            };

            await cosmosMessages.AddAsync(document, cancellationToken);

            conversation.LastMessageContent = document.Content.Length > 500
                ? document.Content[..500]
                : document.Content;
            conversation.LastMessageAt = document.SentAt;
            conversation.HelperLastReadAt = document.SentAt;

            await db.SaveChangesAsync(cancellationToken);

            await chatHub.Clients
                .Group($"user-{task.SeekerProfileId}")
                .SendAsync(
                    "NewMessageNotification",
                    new NewMessageNotification(
                        conversation.Id,
                        profile.DisplayName,
                        document.Content.Length > 100 ? document.Content[..100] : document.Content),
                    cancellationToken);
        }

        return Created(
            $"/api/tasks/{taskId}/applications/{application.Id}",
            TaskApplicationResponse.FromApplication(application, profile.DisplayName, conversation.Id));
    }

    [HttpGet("~/api/task-applications/me")]
    public async Task<IActionResult> ListMineAsync(CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var applications = await db.TaskApplications
            .AsNoTracking()
            .Where(a => a.HelperProfileId == profile.Id)
            .Include(a => a.Task)
                .ThenInclude(t => t.SeekerProfile)
            .Include(a => a.Task)
                .ThenInclude(t => t.AcceptedHelperProfile)
            .Include(a => a.Task)
                .ThenInclude(t => t.Category)
            .OrderByDescending(a => a.UpdatedAt)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var taskIds = applications.Select(a => a.TaskId).ToHashSet();
        var convIdByTask = await db.TaskConversations
            .AsNoTracking()
            .Where(c => taskIds.Contains(c.TaskId) && c.HelperProfileId == profile.Id)
            .Select(c => new { c.TaskId, c.Id })
            .ToDictionaryAsync(c => c.TaskId, c => (Guid?)c.Id, cancellationToken);

        return Ok(applications.Select(a =>
            MyTaskApplicationResponse.FromApplication(
                a,
                profile.DisplayName,
                convIdByTask.GetValueOrDefault(a.TaskId))));
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.SeekerProfileId != profile.Id)
        {
            return Forbid();
        }

        var applications = await db.TaskApplications
            .AsNoTracking()
            .Where(a => a.TaskId == taskId)
            .Include(a => a.HelperProfile)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var helperIds = applications.Select(a => a.HelperProfileId).ToHashSet();
        var convIdByHelper = await db.TaskConversations
            .AsNoTracking()
            .Where(c => c.TaskId == taskId && helperIds.Contains(c.HelperProfileId))
            .Select(c => new { c.HelperProfileId, c.Id })
            .ToDictionaryAsync(c => c.HelperProfileId, c => (Guid?)c.Id, cancellationToken);

        return Ok(applications.Select(a =>
            TaskApplicationResponse.FromApplication(
                a,
                a.HelperProfile.DisplayName,
                convIdByHelper.GetValueOrDefault(a.HelperProfileId))));
    }

    [HttpPatch("{appId:guid}")]
    public async Task<IActionResult> PatchAsync(
        Guid taskId,
        Guid appId,
        PatchApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        if (!TryParseAction(request.Action, out var isAccept))
        {
            ModelState.AddModelError(nameof(request.Action), $"Invalid action '{request.Action}'. Must be 'accept' or 'reject'.");
            return ValidationProblem(ModelState);
        }

        var task = await db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.SeekerProfileId != profile.Id)
        {
            return Forbid();
        }

        var application = await db.TaskApplications
            .FirstOrDefaultAsync(a => a.Id == appId && a.TaskId == taskId, cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        var helperName = await db.Profiles
            .AsNoTracking()
            .Where(p => p.Id == application.HelperProfileId)
            .Select(p => p.DisplayName)
            .FirstAsync(cancellationToken);

        if (isAccept)
        {
            if (!TaskStatusTransitions.CanTransition(task.Status, DomainTaskStatus.InProgress))
            {
                return Problem(
                    title: "Invalid task status transition",
                    detail: "An application can only be accepted when the task has Open status.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (application.Status != TaskApplicationStatus.Pending)
            {
                return Problem(
                    title: "Application is not pending",
                    detail: "Only pending applications can be accepted.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;

            var taskRowsUpdated = await db.Tasks
                .Where(t => t.Id == taskId && t.Status == DomainTaskStatus.Open)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(t => t.Status, DomainTaskStatus.InProgress)
                        .SetProperty(t => t.AcceptedHelperProfileId, application.HelperProfileId)
                        .SetProperty(t => t.AcceptedAt, now)
                        .SetProperty(t => t.UpdatedAt, now),
                    cancellationToken);

            if (taskRowsUpdated == 0)
            {
                return Problem(
                    title: "Task is not open",
                    detail: "The task was already accepted or is no longer open.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var applicationRowsUpdated = await db.TaskApplications
                .Where(a => a.Id == appId && a.TaskId == taskId && a.Status == TaskApplicationStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(a => a.Status, TaskApplicationStatus.Accepted)
                        .SetProperty(a => a.UpdatedAt, now),
                    cancellationToken);

            if (applicationRowsUpdated == 0)
            {
                return Problem(
                    title: "Application is not pending",
                    detail: "Only pending applications can be accepted.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var rejectedCount = await db.TaskApplications
                .Where(a => a.TaskId == taskId && a.Id != appId && a.Status == TaskApplicationStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(a => a.Status, TaskApplicationStatus.Rejected)
                        .SetProperty(a => a.UpdatedAt, now),
                    cancellationToken);

            db.TaskStatusHistory.Add(new TaskStatusHistoryEntry
            {
                TaskId = taskId,
                OldStatus = DomainTaskStatus.Open,
                NewStatus = DomainTaskStatus.InProgress,
                ChangedByProfileId = profile.Id
            });

            db.AddActivityEvent(
                profile.UserId,
                profile.Id,
                ActivityEventType.TaskAccepted,
                nameof(CommunityTask),
                taskId,
                new { application.HelperProfileId, ApplicationId = application.Id });
            db.AddAuditEvent(
                profile.UserId,
                "task_application.accepted",
                nameof(TaskApplication),
                application.Id,
                new
                {
                    application.TaskId,
                    application.HelperProfileId,
                    task.SeekerProfileId,
                    RejectedPendingApplicationCount = rejectedCount
                });

            if (rejectedCount > 0)
            {
                db.AddAuditEvent(
                    profile.UserId,
                    "task_application.auto_rejected",
                    nameof(CommunityTask),
                    taskId,
                    new
                    {
                        AcceptedApplicationId = application.Id,
                        application.HelperProfileId,
                        RejectedPendingApplicationCount = rejectedCount
                    });
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Problem(
                    title: "Task accept conflict",
                    detail: "The task or application changed while the accept request was being processed.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            application.Status = TaskApplicationStatus.Accepted;
            application.UpdatedAt = now;

            // Conversation was already created when the helper applied.
            var conversation = await db.TaskConversations
                .FirstOrDefaultAsync(
                    c => c.TaskId == taskId && c.HelperProfileId == application.HelperProfileId,
                    cancellationToken);

            if (conversation is null)
            {
                // Backward compat: application pre-dates the apply→conversation flow.
                var newConversation = new TaskConversation
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    SeekerProfileId = task.SeekerProfileId,
                    HelperProfileId = application.HelperProfileId,
                    CosmosConversationId = Guid.NewGuid().ToString()
                };
                db.TaskConversations.Add(newConversation);

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                    conversation = newConversation;
                }
                catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsDuplicateTaskConversation(ex))
                {
                    db.Entry(newConversation).State = EntityState.Detached;
                    conversation = await db.TaskConversations
                        .FirstAsync(
                            c => c.TaskId == taskId && c.HelperProfileId == application.HelperProfileId,
                            cancellationToken);
                }
            }

            return Ok(TaskApplicationResponse.FromApplication(application, helperName, conversation.Id));
        }
        else
        {
            if (application.Status != TaskApplicationStatus.Pending)
            {
                return Problem(
                    title: "Application is not pending",
                    detail: "Only pending applications can be rejected.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            application.Status = TaskApplicationStatus.Rejected;
            application.UpdatedAt = DateTimeOffset.UtcNow;

            db.AddActivityEvent(
                profile.UserId,
                profile.Id,
                ActivityEventType.TaskApplicationRejected,
                nameof(TaskApplication),
                application.Id,
                new { application.TaskId, application.HelperProfileId });
            db.AddAuditEvent(
                profile.UserId,
                "task_application.rejected",
                nameof(TaskApplication),
                application.Id,
                new { application.TaskId, application.HelperProfileId });

            await db.SaveChangesAsync(cancellationToken);
        }

        return Ok(TaskApplicationResponse.FromApplication(application, helperName));
    }

    [HttpDelete("{appId:guid}")]
    public async Task<IActionResult> WithdrawAsync(
        Guid taskId,
        Guid appId,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        var application = await db.TaskApplications
            .FirstOrDefaultAsync(a => a.Id == appId && a.TaskId == taskId, cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        var isHelper = application.HelperProfileId == profile.Id;
        var isSeeker = task.SeekerProfileId == profile.Id;
        if (!isHelper && !isSeeker)
        {
            return Forbid();
        }

        if (application.Status == TaskApplicationStatus.Pending && !isHelper)
        {
            return Problem(
                title: "Pending application cannot be cancelled by seeker",
                detail: "Seekers can reject pending applications instead of withdrawing them.",
                statusCode: StatusCodes.Status409Conflict);
        }

        if (application.Status is not TaskApplicationStatus.Pending and not TaskApplicationStatus.Accepted)
        {
            return Problem(
                title: "Application cannot be cancelled",
                detail: "Only pending or accepted applications can be cancelled.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var wasAccepted = application.Status == TaskApplicationStatus.Accepted;

        if (wasAccepted)
        {
            if (task.Status != DomainTaskStatus.InProgress
                || task.AcceptedHelperProfileId != application.HelperProfileId)
            {
                return Problem(
                    title: "Accepted application is not active",
                    detail: "Only the active accepted application can reopen the task.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var oldStatus = task.Status;
            var reason = isSeeker
                ? "Accepted application cancelled by seeker."
                : "Accepted application cancelled by helper.";
            var now = DateTimeOffset.UtcNow;

            task.Status = DomainTaskStatus.Open;
            task.AcceptedHelperProfileId = null;
            task.AcceptedAt = null;
            task.UpdatedAt = now;

            db.TaskStatusHistory.Add(new TaskStatusHistoryEntry
            {
                TaskId = task.Id,
                OldStatus = oldStatus,
                NewStatus = DomainTaskStatus.Open,
                ChangedByProfileId = profile.Id,
                Reason = reason
            });
        }

        application.Status = TaskApplicationStatus.Withdrawn;
        application.UpdatedAt = DateTimeOffset.UtcNow;

        db.AddActivityEvent(
            profile.UserId,
            profile.Id,
            ActivityEventType.TaskApplicationWithdrawn,
            nameof(TaskApplication),
            application.Id,
            new
            {
                application.TaskId,
                application.HelperProfileId,
                WasAccepted = wasAccepted,
                ReopenedTask = wasAccepted && task.Status == DomainTaskStatus.Open,
                CancelledBy = isSeeker ? "Seeker" : "Helper"
            });
        db.AddAuditEvent(
            profile.UserId,
            "task_application.withdrawn",
            nameof(TaskApplication),
            application.Id,
            new
            {
                application.TaskId,
                application.HelperProfileId,
                WasAccepted = wasAccepted,
                ReopenedTask = wasAccepted && task.Status == DomainTaskStatus.Open,
                CancelledBy = isSeeker ? "Seeker" : "Helper"
            });

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
