using Backend.Application.TaskCompletion;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class TaskCompletionRewardServiceTests
{
    [Fact]
    public async Task StageCompletionRewardAsync_AddsExpectedLedgerEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db, CompensationType.Voluntary);
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskCompletionRewardService(db, new FlatTaskCompletionPointsCalculator());

        var awarded = await service.StageCompletionRewardAsync(task, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Equal(
            FlatTaskCompletionPointsCalculator.BasePoints
                + FlatTaskCompletionPointsCalculator.VoluntaryBonusPoints,
            awarded);

        var entry = Assert.Single(db.PointsLedger);
        Assert.Equal(task.Id, entry.TaskId);
        Assert.Equal(task.AcceptedHelperProfileId, entry.ProfileId);
        Assert.Equal(PointEntryType.TaskCompletedReward, entry.EntryType);
        Assert.Equal(awarded, entry.Amount);
    }

    [Fact]
    public async Task StageCompletionRewardAsync_IsIdempotent_WhenAwardIsStagedInSameContext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db, CompensationType.Voluntary);
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskCompletionRewardService(db, new FlatTaskCompletionPointsCalculator());

        var firstAward = await service.StageCompletionRewardAsync(task, cancellationToken);
        // No SaveChanges between calls — the second call must see the change-tracker
        // entry, not just the persisted DB rows.
        var secondAward = await service.StageCompletionRewardAsync(task, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.NotNull(firstAward);
        Assert.Null(secondAward);
        Assert.Single(db.PointsLedger);
    }

    [Fact]
    public async Task StageCompletionRewardAsync_IsIdempotent_WhenAwardAlreadyExists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db, CompensationType.Paid);
        db.PointsLedger.Add(new PointsLedgerEntry
        {
            Id = Guid.NewGuid(),
            ProfileId = task.AcceptedHelperProfileId!.Value,
            TaskId = task.Id,
            Amount = 10,
            EntryType = PointEntryType.TaskCompletedReward,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskCompletionRewardService(db, new FlatTaskCompletionPointsCalculator());

        var awarded = await service.StageCompletionRewardAsync(task, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Null(awarded);
        Assert.Single(db.PointsLedger);
    }

    [Fact]
    public async Task StageCompletionRewardAsync_Throws_WhenTaskNotCompleted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db, CompensationType.Paid);
        task.Status = DomainTaskStatus.InProgress;
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskCompletionRewardService(db, new FlatTaskCompletionPointsCalculator());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StageCompletionRewardAsync(task, cancellationToken));
        Assert.Empty(db.PointsLedger);
    }

    [Fact]
    public async Task StageCompletionRewardAsync_Throws_WhenAcceptedHelperMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db, CompensationType.Paid);
        task.AcceptedHelperProfileId = null;
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskCompletionRewardService(db, new FlatTaskCompletionPointsCalculator());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StageCompletionRewardAsync(task, cancellationToken));
        Assert.Empty(db.PointsLedger);
    }

    private static CommunityTask SeedCompletedTask(AppDbContext db, CompensationType compensationType)
    {
        var categoryId = Guid.NewGuid();
        var seekerId = Guid.NewGuid();
        var helperId = Guid.NewGuid();
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
            Id = seekerId,
            UserId = Guid.NewGuid(),
            DisplayName = "Seeker",
            CreatedAt = now,
            UpdatedAt = now
        });
        db.Profiles.Add(new UserProfile
        {
            Id = helperId,
            UserId = Guid.NewGuid(),
            DisplayName = "Helper",
            CreatedAt = now,
            UpdatedAt = now
        });

        var task = new CommunityTask
        {
            Id = Guid.NewGuid(),
            PublicCode = "TASK-TEST-000001",
            SeekerProfileId = seekerId,
            AcceptedHelperProfileId = helperId,
            CategoryId = categoryId,
            Title = "Sample task",
            Description = "Test seed task.",
            CompensationType = compensationType,
            Status = DomainTaskStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = now
        };
        db.Tasks.Add(task);
        return task;
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .Options;

        return new AppDbContext(options);
    }
}
