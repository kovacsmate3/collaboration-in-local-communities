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
    public async Task PatchAsync_Accept_RejectsTransitionWhenTaskNotOpen_With400()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedScenarioAsync(db, cancellationToken);
        var application = await SeedApplicationAsync(db, scenario, cancellationToken);

        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = DomainTaskStatus.InProgress;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PatchAsync(
            scenario.TaskId,
            application.Id,
            new PatchApplicationRequest("accept"),
            cancellationToken);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.StatusCode);

        var reloaded = await db.TaskApplications.AsNoTracking().FirstAsync(a => a.Id == application.Id, cancellationToken);
        Assert.Equal(TaskApplicationStatus.Pending, reloaded.Status);
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
        return new TestScenario(taskId, seekerUserId, helperUserId, helperProfileId);
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
        Guid HelperUserId,
        Guid HelperProfileId);
}
