using Backend.Common;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Tasks.Applications;

[ApiController]
[Route("api/tasks/{taskId:guid}/applications")]
[Authorize]
public sealed partial class TaskApplicationsController(AppDbContext db) : ControllerBase
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
            TaskId = taskId,
            HelperProfileId = profile.Id,
            Message = StringUtilities.Normalize(request.Message)
        };

        db.TaskApplications.Add(application);

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

        return Created(
            $"/api/tasks/{taskId}/applications/{application.Id}",
            TaskApplicationResponse.FromApplication(application, profile.DisplayName));
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

        return Ok(applications.Select(a => TaskApplicationResponse.FromApplication(a, a.HelperProfile.DisplayName)));
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

        if (isAccept)
        {
            if (task.Status != DomainTaskStatus.Open)
            {
                return Problem(
                    title: "Task is not open",
                    detail: "An application can only be accepted when the task has Open status.",
                    statusCode: StatusCodes.Status409Conflict);
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

            application.Status = TaskApplicationStatus.Accepted;
            application.UpdatedAt = now;

            task.Status = DomainTaskStatus.InProgress;
            task.AcceptedHelperProfileId = application.HelperProfileId;
            task.AcceptedAt = now;
            task.UpdatedAt = now;

            await db.TaskApplications
                .Where(a => a.TaskId == taskId && a.Id != appId && a.Status == TaskApplicationStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(a => a.Status, TaskApplicationStatus.Rejected)
                        .SetProperty(a => a.UpdatedAt, now),
                    cancellationToken);

            db.TaskConversations.Add(new TaskConversation
            {
                TaskId = taskId,
                SeekerProfileId = task.SeekerProfileId,
                HelperProfileId = application.HelperProfileId,
                CosmosConversationId = Guid.NewGuid().ToString()
            });

            db.TaskStatusHistory.Add(new TaskStatusHistoryEntry
            {
                TaskId = taskId,
                OldStatus = DomainTaskStatus.Open,
                NewStatus = DomainTaskStatus.InProgress,
                ChangedByProfileId = profile.Id
            });

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
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

            await db.SaveChangesAsync(cancellationToken);
        }

        var helperName = await db.Profiles
            .AsNoTracking()
            .Where(p => p.Id == application.HelperProfileId)
            .Select(p => p.DisplayName)
            .FirstAsync(cancellationToken);

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

        var taskExists = await db.Tasks
            .AsNoTracking()
            .AnyAsync(t => t.Id == taskId, cancellationToken);

        if (!taskExists)
        {
            return NotFound();
        }

        var application = await db.TaskApplications
            .FirstOrDefaultAsync(a => a.Id == appId && a.TaskId == taskId, cancellationToken);

        if (application is null)
        {
            return NotFound();
        }

        if (application.HelperProfileId != profile.Id)
        {
            return Forbid();
        }

        if (application.Status != TaskApplicationStatus.Pending)
        {
            return Problem(
                title: "Application is not pending",
                detail: "Only pending applications can be withdrawn.",
                statusCode: StatusCodes.Status409Conflict);
        }

        application.Status = TaskApplicationStatus.Withdrawn;
        application.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
