using Backend.Application.Reviews;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class TaskReviewBonusServiceTests
{
    [Fact]
    public async Task StageReviewBonusAsync_WhenSeekerReviewsHelper_AddsBonusEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db);
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskReviewBonusService(db, new ReviewQualityBonusCalculator());

        var awarded = await service.StageReviewBonusAsync(task, task.SeekerProfileId, 5, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Equal(2 * ReviewQualityBonusCalculator.PointsPerStar, awarded);

        var entry = Assert.Single(db.PointsLedger);
        Assert.Equal(task.AcceptedHelperProfileId, entry.ProfileId);
        Assert.Equal(task.Id, entry.TaskId);
        Assert.Equal(PointEntryType.ReviewQualityBonus, entry.EntryType);
        Assert.Equal(awarded, entry.Amount);
    }

    [Fact]
    public async Task StageReviewBonusAsync_WhenHelperReviewsSeeker_AwardsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db);
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskReviewBonusService(db, new ReviewQualityBonusCalculator());

        // The helper rating the seeker says nothing about the helper's own work.
        var awarded = await service.StageReviewBonusAsync(
            task, task.AcceptedHelperProfileId!.Value, 5, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Null(awarded);
        Assert.Empty(db.PointsLedger);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task StageReviewBonusAsync_WhenRatingNotAboveNeutral_AwardsNothing(int rating)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db);
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskReviewBonusService(db, new ReviewQualityBonusCalculator());

        var awarded = await service.StageReviewBonusAsync(task, task.SeekerProfileId, rating, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Null(awarded);
        Assert.Empty(db.PointsLedger);
    }

    [Fact]
    public async Task StageReviewBonusAsync_IsIdempotent_WhenAwardIsStagedInSameContext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db);
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskReviewBonusService(db, new ReviewQualityBonusCalculator());

        var firstAward = await service.StageReviewBonusAsync(task, task.SeekerProfileId, 4, cancellationToken);
        var secondAward = await service.StageReviewBonusAsync(task, task.SeekerProfileId, 4, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.NotNull(firstAward);
        Assert.Null(secondAward);
        Assert.Single(db.PointsLedger);
    }

    [Fact]
    public async Task StageReviewBonusAsync_IsIdempotent_WhenAwardAlreadyExists()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db);
        db.PointsLedger.Add(new PointsLedgerEntry
        {
            Id = Guid.NewGuid(),
            ProfileId = task.AcceptedHelperProfileId!.Value,
            TaskId = task.Id,
            Amount = 6,
            EntryType = PointEntryType.ReviewQualityBonus,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskReviewBonusService(db, new ReviewQualityBonusCalculator());

        var awarded = await service.StageReviewBonusAsync(task, task.SeekerProfileId, 5, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Null(awarded);
        Assert.Single(db.PointsLedger);
    }

    [Fact]
    public async Task StageReviewBonusAsync_WhenNoAcceptedHelper_AwardsNothing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var task = SeedCompletedTask(db);
        task.AcceptedHelperProfileId = null;
        await db.SaveChangesAsync(cancellationToken);

        var service = new TaskReviewBonusService(db, new ReviewQualityBonusCalculator());

        var awarded = await service.StageReviewBonusAsync(task, task.SeekerProfileId, 5, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        Assert.Null(awarded);
        Assert.Empty(db.PointsLedger);
    }

    private static CommunityTask SeedCompletedTask(AppDbContext db)
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
            PublicCode = "TASK-TEST-000002",
            SeekerProfileId = seekerId,
            AcceptedHelperProfileId = helperId,
            CategoryId = categoryId,
            Title = "Sample task",
            Description = "Test seed task.",
            CompensationType = CompensationType.Voluntary,
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
