using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Profiles;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class ProfilesControllerTests
{
    [Theory]
    [InlineData(47.0, null)]
    [InlineData(null, 19.0)]
    public async Task UpdateOwnProfileAsync_ReturnsBadRequest_WhenOnlyOneCoordinateProvided(
        double? latitude, double? longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("Latitude"));
        Assert.True(problem.Errors.ContainsKey("Longitude"));
    }

    [Theory]
    [InlineData(91.0, 0.0)]
    [InlineData(-91.0, 0.0)]
    [InlineData(double.NaN, 0.0)]
    [InlineData(double.PositiveInfinity, 0.0)]
    public async Task UpdateOwnProfileAsync_ReturnsBadRequest_WhenLatitudeIsInvalid(
        double latitude, double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("Latitude"));
    }

    [Theory]
    [InlineData(0.0, 181.0)]
    [InlineData(0.0, -181.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(0.0, double.NegativeInfinity)]
    public async Task UpdateOwnProfileAsync_ReturnsBadRequest_WhenLongitudeIsInvalid(
        double latitude, double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("Longitude"));
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_PersistsLocationWithSrid4326_WhenValidCoordinatesProvided()
    {
        // EF Core InMemory stores .NET objects as-is, so SRID is preserved
        // without needing a real PostGIS-enabled database for this assertion.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        const double latitude = 47.4979;
        const double longitude = 19.0402;

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OwnProfileResponse>(ok.Value);
        Assert.Equal(latitude, response.Latitude);
        Assert.Equal(longitude, response.Longitude);

        var storedProfile = await db.Profiles.FindAsync([profileId], cancellationToken);
        Assert.NotNull(storedProfile?.Location);
        Assert.Equal(4326, storedProfile.Location.SRID);
        Assert.Equal(longitude, storedProfile.Location.X);
        Assert.Equal(latitude, storedProfile.Location.Y);
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_LogsProfileUpdatedActivity()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Updated User" },
            cancellationToken);

        Assert.IsType<OkObjectResult>(result);

        var activity = Assert.Single(db.ActivityEvents);
        Assert.Equal(userId, activity.UserId);
        Assert.Equal(profileId, activity.ProfileId);
        Assert.Equal(ActivityEventType.ProfileUpdated, activity.EventType);
        Assert.Equal(nameof(UserProfile), activity.EntityType);
        Assert.Equal(profileId, activity.EntityId);
    }

    [Fact]
    public async Task GetProfileReviewsAsync_ReturnsReviewsReceivedByProfileNewestFirst()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Profile");
        var reviewer = CreateProfile("Reviewer");
        var otherProfile = CreateProfile("Other");
        var older = DateTimeOffset.UtcNow.AddDays(-2);
        var newer = DateTimeOffset.UtcNow.AddDays(-1);

        db.Profiles.AddRange(profile, reviewer, otherProfile);
        db.Reviews.AddRange(
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                ReviewerProfileId = reviewer.Id,
                ReviewerProfile = reviewer,
                RevieweeProfileId = profile.Id,
                RevieweeProfile = profile,
                Rating = 4,
                Comment = "Older review",
                CreatedAt = older,
                UpdatedAt = older
            },
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                ReviewerProfileId = reviewer.Id,
                ReviewerProfile = reviewer,
                RevieweeProfileId = profile.Id,
                RevieweeProfile = profile,
                Rating = 5,
                Comment = "Newer review",
                CreatedAt = newer,
                UpdatedAt = newer
            },
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                ReviewerProfileId = reviewer.Id,
                ReviewerProfile = reviewer,
                RevieweeProfileId = otherProfile.Id,
                RevieweeProfile = otherProfile,
                Rating = 5,
                Comment = "Other profile review",
                CreatedAt = newer,
                UpdatedAt = newer
            });

        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);
        var result = await controller.GetProfileReviewsAsync(profile.Id, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var reviews = Assert.IsAssignableFrom<IEnumerable<ProfileReviewResponse>>(ok.Value).ToList();

        Assert.Equal(["Newer review", "Older review"], reviews.Select(review => review.Comment));
        Assert.All(reviews, review => Assert.Equal(profile.Id, review.TargetUserId));
        Assert.All(reviews, review => Assert.Equal(reviewer.DisplayName, review.AuthorName));
    }

    [Fact]
    public async Task GetProfileTaskHistoryAsync_ReturnsPostedAndAcceptedTasksNewestFirst()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Profile");
        var helper = CreateProfile("Helper");
        var otherProfile = CreateProfile("Other");
        var category = CreateCategory("repairs", "Repairs", "Wrench01Icon");
        var older = DateTimeOffset.UtcNow.AddDays(-3);
        var newer = DateTimeOffset.UtcNow.AddDays(-1);

        db.Profiles.AddRange(profile, helper, otherProfile);
        db.Categories.Add(category);
        db.Tasks.AddRange(
            CreateTask(
                "Posted task",
                profile,
                acceptedHelper: null,
                category,
                DomainTaskStatus.Open,
                older),
            CreateTask(
                "Accepted task",
                otherProfile,
                profile,
                category,
                DomainTaskStatus.InProgress,
                newer),
            CreateTask(
                "Unrelated task",
                otherProfile,
                helper,
                category,
                DomainTaskStatus.Completed,
                newer));

        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);
        var result = await controller.GetProfileTaskHistoryAsync(profile.Id, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var tasks = Assert.IsAssignableFrom<IEnumerable<ProfileTaskHistoryResponse>>(ok.Value).ToList();

        Assert.Equal(["Accepted task", "Posted task"], tasks.Select(task => task.Title));
        Assert.All(tasks, task => Assert.Equal(category.Code, task.CategoryCode));
        Assert.All(tasks, task => Assert.Equal(category.Icon, task.CategoryIcon));
        Assert.Contains(tasks, task => task.Status == nameof(DomainTaskStatus.InProgress));
    }

    [Fact]
    public async Task GetProfileTaskHistoryAsync_ReturnsNotFoundForMissingProfile()
    {
        await using var db = CreateDbContext();
        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);

        var result = await controller.GetProfileTaskHistoryAsync(
            Guid.NewGuid(),
            TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    private static async Task<(Guid userId, Guid profileId)> SeedProfileAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Existing User",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return (userId, profile.Id);
    }

    private static UserProfile CreateProfile(string displayName)
    {
        return new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            DisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static Category CreateCategory(string code, string name, string icon)
    {
        return new Category
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Icon = icon,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static CommunityTask CreateTask(
        string title,
        UserProfile seeker,
        UserProfile? acceptedHelper,
        Category category,
        DomainTaskStatus status,
        DateTimeOffset createdAt)
    {
        return new CommunityTask
        {
            Id = Guid.NewGuid(),
            PublicCode = $"TASK-{Guid.NewGuid():N}",
            SeekerProfileId = seeker.Id,
            SeekerProfile = seeker,
            AcceptedHelperProfileId = acceptedHelper?.Id,
            AcceptedHelperProfile = acceptedHelper,
            CategoryId = category.Id,
            Category = category,
            Title = title,
            Description = "A test task description long enough for validation.",
            CompensationType = CompensationType.Voluntary,
            Status = status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ProfilesController CreateProfilesController(AppDbContext db, Guid userId)
    {
        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance)
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
            },
            ProblemDetailsFactory = new FakeProblemDetailsFactory()
        };

        return controller;
    }

    private sealed class NullBlobStorageService : IBlobStorageService
    {
        public Task<Uri> UploadProfilePhotoAsync(Guid userId, Stream content, string contentType, string fileExtension, CancellationToken cancellationToken)
            => Task.FromResult(new Uri("https://example.com/photo"));

        public Task DeleteBlobByUrlAsync(string blobUrl, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new() { Status = statusCode, Title = title, Detail = detail };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new(modelStateDictionary) { Status = statusCode ?? 400 };
    }
}
