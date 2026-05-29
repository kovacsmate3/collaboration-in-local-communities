using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Tasks;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

/// <summary>
/// Boundary-case coverage for the Task API proximity filter (issue #43):
/// radius 0, no matches, and exact overlap. The behavioural tests run against
/// the EF Core InMemory provider, which client-evaluates the NetTopologySuite
/// distance predicate (Euclidean in the SRID-4326 coordinate plane) — so the
/// inclusive `&lt;= radius` boundary semantics are validated without a live
/// PostGIS instance. The dedicated math tests assert the same predicate on raw
/// geometries built exactly as the controller builds them.
/// </summary>
public sealed class TaskProximityTests
{
    // ------------------------------------------------------------------
    // Validation boundary: radius must be strictly positive.
    // ------------------------------------------------------------------

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public async Task ListAsync_RejectsNonPositiveRadius(double radiusMeters)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 47.4979,
            longitude: 19.0402,
            radiusMeters: radiusMeters,
            cancellationToken: cancellationToken);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains("radiusMeters", problem.Errors.Keys);
    }

    // ------------------------------------------------------------------
    // Behavioural boundaries through the controller.
    // ------------------------------------------------------------------

    [Fact]
    public async Task ListAsync_ReturnsTasksWithinRadius_OrderedByDistance_AndExcludesFarTasks()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedPosterAsync(db, cancellationToken);

        // Origin at (lat 0, lon 0). Distances below are Euclidean degrees.
        var atOrigin = AddTaskWithLocation(db, profileId, categoryId, now, "Exact overlap", lat: 0, lon: 0);
        var near = AddTaskWithLocation(db, profileId, categoryId, now, "Near", lat: 0, lon: 0.5);
        var far = AddTaskWithLocation(db, profileId, categoryId, now, "Far", lat: 5, lon: 5);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 0,
            longitude: 0,
            radiusMeters: 1,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();

        // Far task excluded; exact-overlap and near returned, nearest first.
        Assert.Equal(["Exact overlap", "Near"], tasks.Select(t => t.Title));
        Assert.DoesNotContain(tasks, t => t.Id == far.Id);
        Assert.Contains(tasks, t => t.Id == atOrigin.Id);
        Assert.Contains(tasks, t => t.Id == near.Id);
    }

    [Fact]
    public async Task ListAsync_IncludesTaskExactlyOnRadiusBoundary()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedPosterAsync(db, cancellationToken);

        // Exactly `radius` away from the origin (distance == radius): the filter
        // is inclusive, so this must be returned.
        AddTaskWithLocation(db, profileId, categoryId, now, "On boundary", lat: 0, lon: 1);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 0,
            longitude: 0,
            radiusMeters: 1,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();
        Assert.Equal(["On boundary"], tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task ListAsync_ReturnsNoTasks_WhenNoneWithinRadius()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedPosterAsync(db, cancellationToken);

        AddTaskWithLocation(db, profileId, categoryId, now, "Far A", lat: 10, lon: 10);
        AddTaskWithLocation(db, profileId, categoryId, now, "Far B", lat: -20, lon: 30);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 0,
            longitude: 0,
            radiusMeters: 1,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value);
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task ListAsync_ExcludesTasksWithoutLocation_WhenProximityFilterApplied()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedPosterAsync(db, cancellationToken);

        AddTaskWithLocation(db, profileId, categoryId, now, "Located", lat: 0, lon: 0);
        AddTaskWithLocation(db, profileId, categoryId, now, "No location", lat: null, lon: null);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 0,
            longitude: 0,
            radiusMeters: 1,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();
        Assert.Equal(["Located"], tasks.Select(t => t.Title));
    }

    // ------------------------------------------------------------------
    // Proximity math: the inclusive predicate the controller relies on,
    // exercised on geometries built exactly like TasksController.BuildLocation.
    // ------------------------------------------------------------------

    [Fact]
    public void IsWithinDistance_ExactOverlap_IsAlwaysWithin()
    {
        var origin = CreatePoint(lat: 47.4979, lon: 19.0402);
        var sameSpot = CreatePoint(lat: 47.4979, lon: 19.0402);

        Assert.Equal(0d, sameSpot.Distance(origin));
        Assert.True(sameSpot.IsWithinDistance(origin, 0));
        Assert.True(sameSpot.IsWithinDistance(origin, 1));
    }

    [Fact]
    public void IsWithinDistance_IsInclusiveAtExactBoundary()
    {
        var origin = CreatePoint(lat: 0, lon: 0);
        var onBoundary = CreatePoint(lat: 0, lon: 1); // distance == 1

        Assert.Equal(1d, onBoundary.Distance(origin));
        Assert.True(onBoundary.IsWithinDistance(origin, 1));
        Assert.False(onBoundary.IsWithinDistance(origin, 0.999));
    }

    [Fact]
    public void IsWithinDistance_NoMatch_WhenOutsideRadius()
    {
        var origin = CreatePoint(lat: 0, lon: 0);
        var far = CreatePoint(lat: 5, lon: 5);

        Assert.False(far.IsWithinDistance(origin, 1));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static Point CreatePoint(double lat, double lon)
    {
        // Mirrors TasksController.BuildLocation: Point(longitude, latitude), SRID 4326.
        return new Point(lon, lat) { SRID = 4326 };
    }

    private static CommunityTask AddTaskWithLocation(
        AppDbContext db,
        Guid profileId,
        Guid categoryId,
        DateTimeOffset now,
        string title,
        double? lat,
        double? lon)
    {
        var task = new CommunityTask
        {
            Id = Guid.NewGuid(),
            SeekerProfileId = profileId,
            CategoryId = categoryId,
            Title = title,
            Description = "Needs a hand nearby.",
            CompensationType = CompensationType.Voluntary,
            Status = DomainTaskStatus.Open,
            Location = lat.HasValue && lon.HasValue ? new Point(lon.Value, lat.Value) { SRID = 4326 } : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Tasks.Add(task);
        return task;
    }

    private static async Task<(Guid ProfileId, Guid CategoryId, DateTimeOffset Now)> SeedPosterAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var profileId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Profiles.Add(new UserProfile
        {
            Id = profileId,
            UserId = Guid.NewGuid(),
            DisplayName = "Task seeker",
            CreatedAt = now,
            UpdatedAt = now
        });
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
        await db.SaveChangesAsync(cancellationToken);
        return (profileId, categoryId, now);
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .Options;

        return new AppDbContext(options);
    }
}
