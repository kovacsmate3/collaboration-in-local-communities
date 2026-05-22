using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Tasks;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Xunit;

namespace backend.Tests;

public sealed class TasksControllerTests
{
    [Fact]
    public async Task CreateAsync_LogsTaskPostedActivity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var userId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        db.Profiles.Add(new UserProfile
        {
            Id = profileId,
            UserId = userId,
            DisplayName = "Task seeker",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        db.Categories.Add(new Category
        {
            Id = categoryId,
            Code = "help",
            Name = "Help",
            Icon = Category.DefaultIcon,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateTasksController(db, userId);

        var result = await controller.CreateAsync(
            new CreateTaskRequest(
                "Carry boxes",
                "Need help carrying boxes up two flights.",
                categoryId,
                "Voluntary",
                null,
                null,
                null,
                null),
            cancellationToken);

        Assert.IsType<CreatedAtActionResult>(result);

        var activity = Assert.Single(db.ActivityEvents);
        Assert.Equal(userId, activity.UserId);
        Assert.Equal(profileId, activity.ProfileId);
        Assert.Equal(ActivityEventType.TaskPosted, activity.EventType);
        Assert.Equal(nameof(CommunityTask), activity.EntityType);
        Assert.NotEqual(Guid.Empty, activity.EntityId);
    }

    [Fact]
    public async Task ListAsync_RejectsPartialProximityFilter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 47.4979,
            longitude: null,
            radiusMeters: 1000,
            cancellationToken);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Contains("Proximity", problem.Errors.Keys);
    }

    [Fact]
    public async Task ListAsync_PaginatesOrderedTasks_WhenPageIsProvided()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var categoryId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
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

        for (var i = 0; i < 5; i++)
        {
            db.Tasks.Add(new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = $"Task {i}",
                Description = "Need help with a task.",
                CompensationType = CompensationType.Voluntary,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(i),
                UpdatedAt = now.AddMinutes(i)
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: "Open",
            categoryId: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken,
            page: 2,
            pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();

        Assert.Equal(["Task 2", "Task 1"], tasks.Select(task => task.Title));
    }

    [Fact]
    public async Task ListAsync_RejectsInvalidPagination()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken,
            page: 0,
            pageSize: 101);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Contains("page", problem.Errors.Keys);
        Assert.Contains("pageSize", problem.Errors.Keys);
    }

    [Fact]
    public async Task ListAsync_RejectsPaginationOffsetOverflow()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken,
            page: int.MaxValue,
            pageSize: 100);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Contains("page", problem.Errors.Keys);
    }

    [Fact]
    public void ProximityQuery_TranslatesToPostgisDWithinAndDistanceOrdering()
    {
        using var db = CreateNpgsqlDbContext();
        var origin = new Point(19.0402, 47.4979) { SRID = 4326 };

        var sql = db.Tasks
            .Where(task => task.Location != null && task.Location.IsWithinDistance(origin, 1000))
            .OrderBy(task => task.Location == null ? double.MaxValue : task.Location.Distance(origin))
            .ToQueryString();

        Assert.Contains("ST_DWithin", sql);
        Assert.Contains("ST_Distance", sql);
        Assert.Contains("ORDER BY", sql);
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), options => options.EnableNullChecks(false))
            .Options;

        return new AppDbContext(options);
    }

    private static AppDbContext CreateNpgsqlDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=proximity_sql_test", npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }

    private static TasksController CreateTasksController(AppDbContext db, Guid userId)
    {
        return new TasksController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim("email_verified", "true")
                    ], "TestAuth"))
                }
            }
        };
    }
}
