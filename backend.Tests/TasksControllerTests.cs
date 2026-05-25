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
                "Points",
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
    public async Task CreateAsync_RejectsCompensationAmountForNonPaidTasks()
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
                "Points",
                5000m,
                null,
                null,
                null),
            cancellationToken);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Contains(nameof(CreateTaskRequest.CompensationAmount), problem.Errors.Keys);
        Assert.Empty(db.Tasks);
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
            cancellationToken: cancellationToken);

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
    public async Task ListAsync_FiltersByCompensationType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedSinglePosterAsync(db, cancellationToken);

        // Two paid tasks (one of which is also the most recent) and two non-paid
        // tasks. The compensationType filter must isolate the paid ones.
        db.Tasks.AddRange(
            new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = "Paid task A",
                Description = "Needs a paid helper.",
                CompensationType = CompensationType.Paid,
                CompensationAmount = 5000m,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(1),
                UpdatedAt = now.AddMinutes(1)
            },
            new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = "Voluntary task",
                Description = "Free help appreciated.",
                CompensationType = CompensationType.Voluntary,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(2),
                UpdatedAt = now.AddMinutes(2)
            },
            new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = "Paid task B",
                Description = "Also paid.",
                CompensationType = CompensationType.Paid,
                CompensationAmount = 8000m,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(3),
                UpdatedAt = now.AddMinutes(3)
            },
            new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = "Barter task",
                Description = "Trade for help.",
                CompensationType = CompensationType.Barter,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(4),
                UpdatedAt = now.AddMinutes(4)
            });
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            compensationType: "Paid",
            createdAfter: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();

        Assert.Equal(2, tasks.Length);
        Assert.All(tasks, task => Assert.Equal(nameof(CompensationType.Paid), task.CompensationType));
    }

    [Fact]
    public async Task ListAsync_TreatsPointsAndVoluntaryAsPointsOnlyFilter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedSinglePosterAsync(db, cancellationToken);

        // Helpers always earn points. The UI sends "points" for the points-only
        // bucket, and older Voluntary records should remain discoverable there.
        db.Tasks.AddRange(
            new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = "Points task",
                Description = "Earn platform points.",
                CompensationType = CompensationType.Points,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now,
                UpdatedAt = now
            },
            new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = "Legacy voluntary task",
                Description = "Older points-only task.",
                CompensationType = CompensationType.Voluntary,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(1),
                UpdatedAt = now.AddMinutes(1)
            });
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            compensationType: "points",
            createdAfter: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();

        Assert.Equal(2, tasks.Length);
        Assert.Contains(tasks, task => task.CompensationType == nameof(CompensationType.Points));
        Assert.Contains(tasks, task => task.CompensationType == nameof(CompensationType.Voluntary));
    }

    [Fact]
    public async Task ListAsync_RejectsInvalidCompensationType()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            compensationType: "not-a-real-type",
            createdAfter: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Contains("compensationType", problem.Errors.Keys);
    }

    [Fact]
    public async Task ListAsync_AppliesCompensationFilterBeforePagination()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedSinglePosterAsync(db, cancellationToken);

        // Three paid + three voluntary, interleaved by creation time. With the
        // compensation filter applied server-side first, pageSize=2 on page 1
        // returns the two most-recent Paid tasks; if filtering happened after
        // pagination, the first page would only contain voluntary tasks.
        for (var i = 0; i < 6; i++)
        {
            db.Tasks.Add(new CommunityTask
            {
                Id = Guid.NewGuid(),
                SeekerProfileId = profileId,
                CategoryId = categoryId,
                Title = $"Task {i}",
                Description = "Needs help.",
                CompensationType = i % 2 == 0 ? CompensationType.Paid : CompensationType.Voluntary,
                CompensationAmount = i % 2 == 0 ? 1000m : null,
                Status = Backend.Domain.Enums.TaskStatus.Open,
                CreatedAt = now.AddMinutes(i),
                UpdatedAt = now.AddMinutes(i)
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            compensationType: "Paid",
            createdAfter: null,
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken,
            page: 1,
            pageSize: 2);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();

        Assert.Equal(2, tasks.Length);
        Assert.All(tasks, task => Assert.Equal(nameof(CompensationType.Paid), task.CompensationType));
        Assert.Equal(["Task 4", "Task 2"], tasks.Select(task => task.Title));
    }

    [Fact]
    public async Task ListAsync_FiltersOutTasksOlderThanCreatedAfter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var (profileId, categoryId, now) = await SeedSinglePosterAsync(db, cancellationToken);

        var oldTask = new CommunityTask
        {
            Id = Guid.NewGuid(),
            SeekerProfileId = profileId,
            CategoryId = categoryId,
            Title = "Old task",
            Description = "Posted weeks ago.",
            CompensationType = CompensationType.Voluntary,
            Status = Backend.Domain.Enums.TaskStatus.Open,
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-30)
        };
        var freshTask = new CommunityTask
        {
            Id = Guid.NewGuid(),
            SeekerProfileId = profileId,
            CategoryId = categoryId,
            Title = "Fresh task",
            Description = "Just posted.",
            CompensationType = CompensationType.Voluntary,
            Status = Backend.Domain.Enums.TaskStatus.Open,
            CreatedAt = now.AddHours(-1),
            UpdatedAt = now.AddHours(-1)
        };
        db.Tasks.AddRange(oldTask, freshTask);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            compensationType: null,
            createdAfter: now.AddDays(-7),
            latitude: null,
            longitude: null,
            radiusMeters: null,
            cancellationToken: cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<TaskResponse>>(ok.Value).ToArray();

        var task = Assert.Single(tasks);
        Assert.Equal("Fresh task", task.Title);
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

    private static async Task<(Guid ProfileId, Guid CategoryId, DateTimeOffset Now)> SeedSinglePosterAsync(
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
