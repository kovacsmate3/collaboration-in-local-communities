using System.Security.Claims;
using Backend.Application.TaskCompletion;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Tasks;
using Backend.Features.Tasks.Completion;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class TaskCompletionControllerTests
{
    [Fact]
    public async Task SubmitAsync_FlipsStatusAndCreatesConfirmation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedInProgressTaskAsync(db, CompensationType.Voluntary, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.SubmitAsync(scenario.TaskId, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TaskResponse>(ok.Value);
        Assert.Equal(nameof(DomainTaskStatus.PendingApproval), response.Status);

        var task = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        Assert.Equal(DomainTaskStatus.PendingApproval, task.Status);
        Assert.Null(task.CompletedAt);

        var confirmation = Assert.Single(db.TaskCompletionConfirmations);
        Assert.Equal(scenario.HelperProfileId, confirmation.ProfileId);
        Assert.Equal(scenario.TaskId, confirmation.TaskId);

        var historyEntry = Assert.Single(db.TaskStatusHistory);
        Assert.Equal(DomainTaskStatus.InProgress, historyEntry.OldStatus);
        Assert.Equal(DomainTaskStatus.PendingApproval, historyEntry.NewStatus);
        Assert.Equal(scenario.HelperProfileId, historyEntry.ChangedByProfileId);

        var audit = Assert.Single(db.AuditEvents);
        Assert.Equal("task.completion_submitted", audit.EventType);

        // No completion-related activity event fires on submission.
        Assert.Empty(db.ActivityEvents);

        // Submission must not award points until the seeker approves.
        Assert.Empty(db.PointsLedger);
    }

    [Fact]
    public async Task SubmitAsync_ReturnsForbid_WhenCallerIsNotAcceptedHelper()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedInProgressTaskAsync(db, CompensationType.Paid, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.SubmitAsync(scenario.TaskId, cancellationToken);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(db.TaskCompletionConfirmations);
    }

    [Theory]
    [InlineData(DomainTaskStatus.Open)]
    [InlineData(DomainTaskStatus.Cancelled)]
    [InlineData(DomainTaskStatus.Completed)]
    [InlineData(DomainTaskStatus.PendingApproval)]
    public async Task SubmitAsync_RejectsNonInProgressStatuses(DomainTaskStatus initialStatus)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedInProgressTaskAsync(db, CompensationType.Paid, cancellationToken);
        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = initialStatus;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.SubmitAsync(scenario.TaskId, cancellationToken);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);

        // No write-through side effects.
        var reloaded = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        Assert.Equal(initialStatus, reloaded.Status);
        Assert.Empty(db.TaskCompletionConfirmations);
        Assert.Empty(db.PointsLedger);
    }

    [Fact]
    public async Task ApproveAsync_CompletesTaskAndAwardsPoints()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedPendingApprovalTaskAsync(db, CompensationType.Voluntary, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.ApproveAsync(scenario.TaskId, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TaskResponse>(ok.Value);
        Assert.Equal(nameof(DomainTaskStatus.Completed), response.Status);

        var task = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        Assert.Equal(DomainTaskStatus.Completed, task.Status);
        Assert.NotNull(task.CompletedAt);

        var entry = Assert.Single(db.PointsLedger);
        Assert.Equal(PointEntryType.TaskCompletedReward, entry.EntryType);
        Assert.Equal(scenario.HelperProfileId, entry.ProfileId);
        Assert.Equal(scenario.TaskId, entry.TaskId);
        Assert.Equal(
            FlatTaskCompletionPointsCalculator.BasePoints
                + FlatTaskCompletionPointsCalculator.VoluntaryBonusPoints,
            entry.Amount);

        Assert.Contains(db.ActivityEvents, a => a.EventType == ActivityEventType.TaskCompleted);
        Assert.Contains(db.AuditEvents, a => a.EventType == "task.completion_approved");

        Assert.Equal(2, db.TaskCompletionConfirmations.Count());
    }

    [Fact]
    public async Task ApproveAsync_IsIdempotent_WhenRetriedAfterReward()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedPendingApprovalTaskAsync(db, CompensationType.Paid, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var firstResult = await controller.ApproveAsync(scenario.TaskId, cancellationToken);
        Assert.IsType<OkObjectResult>(firstResult);

        var secondResult = await controller.ApproveAsync(scenario.TaskId, cancellationToken);
        var problem = Assert.IsType<ObjectResult>(secondResult);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);

        // Reward and completion side effects must remain singletons.
        Assert.Single(db.PointsLedger);
        Assert.Single(db.ActivityEvents.Where(a => a.EventType == ActivityEventType.TaskCompleted));
    }

    [Fact]
    public async Task ApproveAsync_RewardIsSeparateFromPaidCompensation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedPendingApprovalTaskAsync(db, CompensationType.Paid, cancellationToken);
        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.CompensationAmount = 5000m;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.ApproveAsync(scenario.TaskId, cancellationToken);

        Assert.IsType<OkObjectResult>(result);
        var entry = Assert.Single(db.PointsLedger);
        Assert.Equal(FlatTaskCompletionPointsCalculator.BasePoints, entry.Amount);

        // Paid amount on the task is preserved and lives outside the points economy.
        var reloaded = await db.Tasks.AsNoTracking().FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        Assert.Equal(5000m, reloaded.CompensationAmount);
    }

    [Fact]
    public async Task ApproveAsync_ReturnsForbid_WhenCallerIsNotSeeker()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedPendingApprovalTaskAsync(db, CompensationType.Voluntary, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.ApproveAsync(scenario.TaskId, cancellationToken);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(db.PointsLedger);
    }

    [Theory]
    [InlineData(DomainTaskStatus.Open)]
    [InlineData(DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.Cancelled)]
    public async Task ApproveAsync_RejectsNonPendingApprovalStatuses(DomainTaskStatus initialStatus)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedPendingApprovalTaskAsync(db, CompensationType.Voluntary, cancellationToken);
        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = initialStatus;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.ApproveAsync(scenario.TaskId, cancellationToken);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Empty(db.PointsLedger);
    }

    private static async Task<CompletionScenario> SeedInProgressTaskAsync(
        AppDbContext db,
        CompensationType compensationType,
        CancellationToken cancellationToken)
    {
        var categoryId = Guid.NewGuid();
        var seekerUserId = Guid.NewGuid();
        var seekerProfileId = Guid.NewGuid();
        var helperUserId = Guid.NewGuid();
        var helperProfileId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Categories.Add(new Category
        {
            Id = categoryId,
            Code = "help",
            Name = "Help",
            Icon = Category.DefaultIcon,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Profiles.Add(new UserProfile
        {
            Id = seekerProfileId,
            UserId = seekerUserId,
            DisplayName = "Seeker",
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Profiles.Add(new UserProfile
        {
            Id = helperProfileId,
            UserId = helperUserId,
            DisplayName = "Helper",
            CreatedAt = now,
            UpdatedAt = now
        });

        var taskId = Guid.NewGuid();
        db.Tasks.Add(new CommunityTask
        {
            Id = taskId,
            PublicCode = "TASK-TEST-000123",
            SeekerProfileId = seekerProfileId,
            AcceptedHelperProfileId = helperProfileId,
            CategoryId = categoryId,
            Title = "Carry boxes",
            Description = "Need help carrying boxes.",
            CompensationType = compensationType,
            Status = DomainTaskStatus.InProgress,
            CreatedAt = now,
            UpdatedAt = now,
            AcceptedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return new CompletionScenario(taskId, seekerUserId, helperUserId, helperProfileId);
    }

    private static async Task<CompletionScenario> SeedPendingApprovalTaskAsync(
        AppDbContext db,
        CompensationType compensationType,
        CancellationToken cancellationToken)
    {
        var scenario = await SeedInProgressTaskAsync(db, compensationType, cancellationToken);
        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = DomainTaskStatus.PendingApproval;
        db.TaskCompletionConfirmations.Add(new TaskCompletionConfirmation
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ProfileId = scenario.HelperProfileId,
            ConfirmedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return scenario;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .Options;

        return new AppDbContext(options);
    }

    private static TaskCompletionController CreateController(AppDbContext db, Guid userId)
    {
        var rewardService = new TaskCompletionRewardService(db, new FlatTaskCompletionPointsCalculator());
        return new TaskCompletionController(db, rewardService)
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

    private sealed record CompletionScenario(
        Guid TaskId,
        Guid SeekerUserId,
        Guid HelperUserId,
        Guid HelperProfileId);
}
