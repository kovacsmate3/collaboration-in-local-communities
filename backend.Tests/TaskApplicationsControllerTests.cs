using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Tasks.Applications;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class TaskApplicationsControllerTests
{
    [Fact]
    public async Task ApplyAsync_LogsApplicationActivityAndAuditEvent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.ApplyAsync(
            scenario.TaskId,
            new ApplyToTaskRequest(null),
            cancellationToken);

        Assert.IsType<CreatedResult>(result);

        var activity = Assert.Single(db.ActivityEvents);
        Assert.Equal(ActivityEventType.TaskApplicationSubmitted, activity.EventType);
        Assert.Equal(nameof(TaskApplication), activity.EntityType);
        Assert.Equal(scenario.HelperUserId, activity.UserId);

        var audit = Assert.Single(db.AuditEvents);
        Assert.Equal("task_application.submitted", audit.EventType);
        Assert.Equal(nameof(TaskApplication), audit.EntityType);
        Assert.Equal(scenario.HelperUserId, audit.ActorUserId);
        Assert.Contains(scenario.TaskId.ToString(), audit.Payload);
    }

    [Fact]
    public async Task ListMineAsync_ReturnsApplicationsForCurrentHelper()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        db.TaskApplications.Add(new TaskApplication
        {
            Id = Guid.NewGuid(),
            TaskId = scenario.TaskId,
            HelperProfileId = scenario.HelperProfileId,
            Message = "Available tomorrow.",
            Status = TaskApplicationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.ListMineAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var applications = Assert.IsAssignableFrom<IEnumerable<MyTaskApplicationResponse>>(ok.Value).ToList();
        var application = Assert.Single(applications);
        Assert.Equal(scenario.TaskId, application.TaskId);
        Assert.Equal("Carry boxes", application.Task.Title);
    }

    [Fact]
    public async Task PatchAsync_Reject_LogsApplicationActivityAndAuditEvent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var application = await SeedApplicationAsync(db, scenario, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PatchAsync(
            scenario.TaskId,
            application.Id,
            new PatchApplicationRequest("reject"),
            cancellationToken);

        Assert.IsType<OkObjectResult>(result);
        Assert.Contains(db.ActivityEvents, e => e.EventType == ActivityEventType.TaskApplicationRejected);
        Assert.Contains(db.AuditEvents, e => e.EventType == "task_application.rejected");
    }

    [Fact]
    public async Task WithdrawAsync_LogsApplicationActivityAndAuditEvent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var application = await SeedApplicationAsync(db, scenario, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.WithdrawAsync(
            scenario.TaskId,
            application.Id,
            cancellationToken);

        Assert.IsType<NoContentResult>(result);
        Assert.Contains(db.ActivityEvents, e => e.EventType == ActivityEventType.TaskApplicationWithdrawn);
        Assert.Contains(db.AuditEvents, e => e.EventType == "task_application.withdrawn");
    }

    [Fact]
    public async Task WithdrawAsync_PendingApplicationBySeeker_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var application = await SeedApplicationAsync(db, scenario, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.WithdrawAsync(
            scenario.TaskId,
            application.Id,
            cancellationToken);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        var details = Assert.IsType<ProblemDetails>(problem.Value);
        Assert.Equal("Pending application cannot be cancelled by seeker", details.Title);
        Assert.Equal(TaskApplicationStatus.Pending, application.Status);
        Assert.Empty(db.ActivityEvents);
        Assert.Empty(db.AuditEvents);
    }

    [Fact]
    public async Task WithdrawAsync_AcceptedApplicationByHelper_ReopensTask()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var application = await SeedAcceptedApplicationAsync(db, scenario, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.WithdrawAsync(
            scenario.TaskId,
            application.Id,
            cancellationToken);

        Assert.IsType<NoContentResult>(result);
        var task = await db.Tasks.SingleAsync(t => t.Id == scenario.TaskId, cancellationToken);
        Assert.Equal(DomainTaskStatus.Open, task.Status);
        Assert.Null(task.AcceptedHelperProfileId);
        Assert.Null(task.AcceptedAt);
        Assert.Equal(TaskApplicationStatus.Withdrawn, application.Status);
        Assert.Contains(
            db.TaskStatusHistory,
            h => h.TaskId == scenario.TaskId
                && h.OldStatus == DomainTaskStatus.InProgress
                && h.NewStatus == DomainTaskStatus.Open
                && h.ChangedByProfileId == scenario.HelperProfileId);
        Assert.Contains(db.AuditEvents, e => e.EventType == "task_application.withdrawn");
    }

    [Fact]
    public async Task WithdrawAsync_AcceptedApplicationBySeeker_ReopensTask()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var application = await SeedAcceptedApplicationAsync(db, scenario, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.WithdrawAsync(
            scenario.TaskId,
            application.Id,
            cancellationToken);

        Assert.IsType<NoContentResult>(result);
        var task = await db.Tasks.SingleAsync(t => t.Id == scenario.TaskId, cancellationToken);
        Assert.Equal(DomainTaskStatus.Open, task.Status);
        Assert.Null(task.AcceptedHelperProfileId);
        Assert.Null(task.AcceptedAt);
        Assert.Equal(TaskApplicationStatus.Withdrawn, application.Status);
        Assert.Contains(
            db.TaskStatusHistory,
            h => h.TaskId == scenario.TaskId
                && h.OldStatus == DomainTaskStatus.InProgress
                && h.NewStatus == DomainTaskStatus.Open
                && h.ChangedByProfileId == scenario.SeekerProfileId);
        Assert.Contains(db.AuditEvents, e => e.EventType == "task_application.withdrawn");
    }

    private static async Task<TestScenario> SeedScenarioAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var categoryId = Guid.NewGuid();
        db.Categories.Add(new Category
        {
            Id = categoryId,
            Code = "moving",
            Name = "Moving",
            Icon = Category.DefaultIcon,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var seekerUserId = Guid.NewGuid();
        var seekerProfileId = Guid.NewGuid();
        db.Profiles.Add(new UserProfile
        {
            Id = seekerProfileId,
            UserId = seekerUserId,
            DisplayName = "Seeker",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var helperUserId = Guid.NewGuid();
        var helperProfileId = Guid.NewGuid();
        db.Profiles.Add(new UserProfile
        {
            Id = helperProfileId,
            UserId = helperUserId,
            DisplayName = "Helper",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var taskId = Guid.NewGuid();
        db.Tasks.Add(new CommunityTask
        {
            Id = taskId,
            PublicCode = "TASK-TEST-000001",
            SeekerProfileId = seekerProfileId,
            CategoryId = categoryId,
            Title = "Carry boxes",
            Description = "Need help carrying boxes.",
            CompensationType = CompensationType.Voluntary,
            Status = DomainTaskStatus.Open,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return new TestScenario(taskId, seekerUserId, seekerProfileId, helperUserId, helperProfileId);
    }

    private static async Task<TaskApplication> SeedApplicationAsync(
        AppDbContext db,
        TestScenario scenario,
        CancellationToken cancellationToken)
    {
        var application = new TaskApplication
        {
            Id = Guid.NewGuid(),
            TaskId = scenario.TaskId,
            HelperProfileId = scenario.HelperProfileId,
            Status = TaskApplicationStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.TaskApplications.Add(application);
        await db.SaveChangesAsync(cancellationToken);
        return application;
    }

    private static async Task<TaskApplication> SeedAcceptedApplicationAsync(
        AppDbContext db,
        TestScenario scenario,
        CancellationToken cancellationToken)
    {
        var application = await SeedApplicationAsync(db, scenario, cancellationToken);
        application.Status = TaskApplicationStatus.Accepted;
        application.UpdatedAt = DateTimeOffset.UtcNow;

        var task = await db.Tasks.SingleAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = DomainTaskStatus.InProgress;
        task.AcceptedHelperProfileId = scenario.HelperProfileId;
        task.AcceptedAt = DateTimeOffset.UtcNow;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return application;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .Options;

        return new AppDbContext(options);
    }

    private static TaskApplicationsController CreateController(AppDbContext db, Guid userId)
    {
        return new TaskApplicationsController(db, null!, null!)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    ], "TestAuth"))
                }
            }
        };
    }

    private sealed record TestScenario(
        Guid TaskId,
        Guid SeekerUserId,
        Guid SeekerProfileId,
        Guid HelperUserId,
        Guid HelperProfileId);
}
