using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Admin.Analytics;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class AdminAnalyticsControllerTests
{
    [Fact]
    public async Task GetKpiAsync_ReturnsNotFound_WhenReadModelIsEmpty()
    {
        // Note: KpiCurrent maps to a PostgreSQL view (kpi_current_v) that always returns
        // a single aggregate row in production. An empty result can only happen when the
        // view is unavailable or the database is in an unexpected state.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = new AdminAnalyticsController(db);

        var result = await controller.GetKpiAsync(cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetTaskStatusChart_ReturnsEmptyEntries_WhenNoTasks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db);

        var result = await controller.GetTaskStatusChartAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChartDataResponse>(ok.Value);
        Assert.Empty(response.Entries);
    }

    [Fact]
    public async Task GetTaskStatusChart_ReturnsGroupedCounts()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var (catId, profileId) = await SeedMinimalRequiredEntities(db, cancellationToken);

        db.Tasks.Add(MakeTask(catId, profileId, DomainTaskStatus.Open));
        db.Tasks.Add(MakeTask(catId, profileId, DomainTaskStatus.Open));
        db.Tasks.Add(MakeTask(catId, profileId, DomainTaskStatus.Completed));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetTaskStatusChartAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChartDataResponse>(ok.Value);

        Assert.Equal(2, response.Entries.Count);

        var openEntry = response.Entries.First(e => e.Label == "Open");
        Assert.Equal(2, openEntry.Count);

        var completedEntry = response.Entries.First(e => e.Label == "Completed");
        Assert.Equal(1, completedEntry.Count);
    }

    [Fact]
    public async Task GetCategoryDemandChart_ReturnsTopCategories()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var profileId = Guid.NewGuid();
        db.Profiles.Add(new UserProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            DisplayName = "Test",
            IsProfileCompleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var catAId = Guid.NewGuid();
        var catBId = Guid.NewGuid();
        db.Categories.Add(new Category
        {
            Id = catAId,
            Code = "cat-a",
            Name = "Category A",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        db.Categories.Add(new Category
        {
            Id = catBId,
            Code = "cat-b",
            Name = "Category B",
            Icon = Category.DefaultIcon,
            SortOrder = 2,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        db.Tasks.Add(MakeTask(catAId, profileId, DomainTaskStatus.Open));
        db.Tasks.Add(MakeTask(catAId, profileId, DomainTaskStatus.Open));
        db.Tasks.Add(MakeTask(catBId, profileId, DomainTaskStatus.Open));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetCategoryDemandChartAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChartDataResponse>(ok.Value);

        Assert.Equal(2, response.Entries.Count);

        var topEntry = response.Entries.First();
        Assert.Equal("Category A", topEntry.Label);
        Assert.Equal(2, topEntry.Count);
        Assert.Equal(100.0, topEntry.Pct);
    }

    [Fact]
    public async Task GetCompensationMixChart_ReturnsGroupedByType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var (catId, profileId) = await SeedMinimalRequiredEntities(db, cancellationToken);

        db.Tasks.Add(MakeTask(catId, profileId, DomainTaskStatus.Open, CompensationType.Voluntary));
        db.Tasks.Add(MakeTask(catId, profileId, DomainTaskStatus.Open, CompensationType.Voluntary));
        db.Tasks.Add(MakeTask(catId, profileId, DomainTaskStatus.Open, CompensationType.Paid));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db);
        var result = await controller.GetCompensationMixChartAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ChartDataResponse>(ok.Value);

        Assert.Equal(2, response.Entries.Count);

        var voluntaryEntry = response.Entries.First(e => e.Label == "Voluntary");
        Assert.Equal(2, voluntaryEntry.Count);

        var paidEntry = response.Entries.First(e => e.Label == "Paid");
        Assert.Equal(1, paidEntry.Count);
    }

    private static async Task<(Guid CatId, Guid ProfileId)> SeedMinimalRequiredEntities(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var catId = Guid.NewGuid();
        db.Categories.Add(new Category
        {
            Id = catId,
            Code = "test",
            Name = "Test",
            Icon = Category.DefaultIcon,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        var profileId = Guid.NewGuid();
        db.Profiles.Add(new UserProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            DisplayName = "Test",
            IsProfileCompleted = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
        return (catId, profileId);
    }

    private static CommunityTask MakeTask(
        Guid catId,
        Guid profileId,
        DomainTaskStatus status,
        CompensationType compensation = CompensationType.Voluntary) =>
        new()
        {
            Id = Guid.NewGuid(),
            PublicCode = Guid.NewGuid().ToString("N"),
            SeekerProfileId = profileId,
            CategoryId = catId,
            Title = "T",
            Description = "D",
            Status = status,
            CompensationType = compensation,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AdminAnalyticsController CreateController(AppDbContext db) =>
        new(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Admin"),
                    ], "TestAuth")),
                },
            },
        };
}
