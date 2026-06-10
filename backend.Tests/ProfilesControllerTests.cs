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
        var result = await controller.GetProfileReviewsAsync(profile.Id, page: 1, pageSize: 10, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<ProfileReviewPagedResponse>(ok.Value);

        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(1, paged.Page);
        Assert.Equal(10, paged.PageSize);
        Assert.Equal(1, paged.TotalPages);
        Assert.Equal(["Newer review", "Older review"], paged.Items.Select(review => review.Comment));
        Assert.All(paged.Items, review => Assert.Equal(profile.Id, review.TargetUserId));
        Assert.All(paged.Items, review => Assert.Equal(reviewer.DisplayName, review.AuthorName));
    }

    [Fact]
    public async Task GetProfileReviewsAsync_RespectsPaginationParameters()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Profile");
        var reviewer = CreateProfile("Reviewer");

        db.Profiles.AddRange(profile, reviewer);

        // Seed 3 reviews with increasing CreatedAt so ordering is deterministic.
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 3; i++)
        {
            db.Reviews.Add(new Review
            {
                Id = Guid.NewGuid(),
                TaskId = Guid.NewGuid(),
                ReviewerProfileId = reviewer.Id,
                ReviewerProfile = reviewer,
                RevieweeProfileId = profile.Id,
                RevieweeProfile = profile,
                Rating = 5,
                Comment = $"review {i}",
                CreatedAt = now.AddMinutes(i),
                UpdatedAt = now.AddMinutes(i),
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);

        // Page 1, size 2 → newest 2.
        var page1Result = await controller.GetProfileReviewsAsync(profile.Id, page: 1, pageSize: 2, cancellationToken);
        var page1 = Assert.IsType<ProfileReviewPagedResponse>(Assert.IsType<OkObjectResult>(page1Result).Value);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(["review 2", "review 1"], page1.Items.Select(r => r.Comment));

        // Page 2, size 2 → oldest 1.
        var page2Result = await controller.GetProfileReviewsAsync(profile.Id, page: 2, pageSize: 2, cancellationToken);
        var page2 = Assert.IsType<ProfileReviewPagedResponse>(Assert.IsType<OkObjectResult>(page2Result).Value);
        Assert.Single(page2.Items);
        Assert.Equal("review 0", page2.Items[0].Comment);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task GetProfileReviewsAsync_InvalidPagination_ReturnsValidationProblem(int page, int pageSize)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Profile");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);
        var result = await controller.GetProfileReviewsAsync(profile.Id, page, pageSize, cancellationToken);

        // ValidationProblem() returns an ObjectResult whose Value is a
        // ValidationProblemDetails. In unit tests without a full DI container
        // the wrapper's StatusCode is null (the 400 only lives on the inner
        // ProblemDetails.Status), so we assert on the payload type.
        var problem = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.IsAssignableFrom<ValidationProblemDetails>(problem.Value);
    }

    [Fact]
    public async Task GetProfileReviewsAsync_OffsetOverflow_ReturnsValidationProblem()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Profile");
        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);

        // (page-1)*pageSize must be evaluated as long to detect overflow.
        // page=int.MaxValue, pageSize=50 would wrap to a negative int.
        var result = await controller.GetProfileReviewsAsync(
            profile.Id, page: int.MaxValue, pageSize: 50, cancellationToken);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.IsAssignableFrom<ValidationProblemDetails>(problem.Value);
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

    [Fact]
    public async Task GetProfileReputationTrendAsync_ReturnsDailySnapshotsFromCompletedTasksAndReviews()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Profile");
        var helper = CreateProfile("Helper");
        var seeker = CreateProfile("Seeker");
        var reviewer = CreateProfile("Reviewer");
        var category = CreateCategory("repairs", "Repairs", "Wrench01Icon");
        var firstDay = new DateTimeOffset(2026, 1, 2, 9, 0, 0, TimeSpan.Zero);
        var secondDay = firstDay.AddDays(1);
        var thirdDay = firstDay.AddDays(2);
        var fourthDay = firstDay.AddDays(3);
        var seekerTask = CreateTask(
            "Seeker completion",
            profile,
            helper,
            category,
            DomainTaskStatus.Completed,
            firstDay.AddDays(-1),
            firstDay);
        var helperTask = CreateTask(
            "Helper completion",
            seeker,
            profile,
            category,
            DomainTaskStatus.Completed,
            thirdDay.AddDays(-1),
            thirdDay);

        db.Profiles.AddRange(profile, helper, seeker, reviewer);
        db.Categories.Add(category);
        db.Tasks.AddRange(seekerTask, helperTask);
        db.Reviews.AddRange(
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = seekerTask.Id,
                ReviewerProfileId = reviewer.Id,
                ReviewerProfile = reviewer,
                RevieweeProfileId = profile.Id,
                RevieweeProfile = profile,
                Rating = 5,
                Comment = "Great help",
                CreatedAt = secondDay,
                UpdatedAt = secondDay
            },
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = helperTask.Id,
                ReviewerProfileId = reviewer.Id,
                ReviewerProfile = reviewer,
                RevieweeProfileId = profile.Id,
                RevieweeProfile = profile,
                Rating = 3,
                Comment = "Solid help",
                CreatedAt = fourthDay,
                UpdatedAt = fourthDay
            });
        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);
        var result = await controller.GetProfileReputationTrendAsync(profile.Id, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var trend = Assert.IsAssignableFrom<IEnumerable<ProfileReputationTrendPointResponse>>(ok.Value).ToList();

        // Only helper-side contributions score: the helper-side completion on
        // thirdDay and the helper-side review on fourthDay. The seeker-side
        // completion (firstDay) and seeker-side review (secondDay) are excluded,
        // so they no longer appear as trend points.
        Assert.Equal(
            [10, 16],
            trend.Select(point => point.Score));
        Assert.Equal(
            new[] { thirdDay, fourthDay }
                .Select(day => DateOnly.FromDateTime(day.UtcDateTime)),
            trend.Select(point => point.Date));
        Assert.Equal([0m, 3m], trend.Select(point => point.AverageRating));
        Assert.Equal([0, 1], trend.Select(point => point.ReviewCount));
        Assert.Equal([1, 1], trend.Select(point => point.CompletedTaskCount));
    }

    [Fact]
    public async Task GetPublicProfileAsync_ReputationScore_CountsOnlyHelperSideReviews()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var profile = CreateProfile("Helper");
        // Display aggregates reflect ALL reviews received and are passed through
        // unchanged (a task-giver still visibly "gets" the rating left for them)…
        profile.AverageRating = 4.8m;
        profile.ReviewCount = 12;
        // …while the score's completed-task component uses the maintained counter.
        profile.CompletedTaskCount = 9;
        profile.PrivacySettings = new ProfilePrivacySettings
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            ShowWorkplace = true,
            ShowPosition = true,
            ShowLocation = true,
            ShowAvailability = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var seeker = CreateProfile("Seeker");
        var reviewer = CreateProfile("Reviewer");
        var category = CreateCategory("repairs", "Repairs", "Wrench01Icon");
        var now = DateTimeOffset.UtcNow;

        // A task the profile completed AS THE HELPER — its reviews score.
        var helperTask = CreateTask(
            "Helped", seeker, profile, category, DomainTaskStatus.Completed, now.AddDays(-2), now.AddDays(-1));
        // A task the profile posted AS THE SEEKER — its reviews are displayed but
        // must NOT contribute reputation points.
        var seekerTask = CreateTask(
            "Posted", profile, seeker, category, DomainTaskStatus.Completed, now.AddDays(-2), now.AddDays(-1));

        db.Profiles.AddRange(profile, seeker, reviewer);
        db.Categories.Add(category);
        db.Tasks.AddRange(helperTask, seekerTask);
        db.Reviews.AddRange(
            CreateReview(helperTask, reviewer, profile, 5, now),
            CreateReview(helperTask, seeker, profile, 5, now),
            // Seeker-side review — excluded from the score.
            CreateReview(seekerTask, seeker, profile, 5, now));
        await db.SaveChangesAsync(cancellationToken);

        var controller = new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance);
        var result = await controller.GetPublicProfileAsync(profile.Id, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PublicProfileResponse>(ok.Value);
        // 9 x 10 (completed) + 5.00 avg x 2 helper-side reviews x 2 = 110.
        // The seeker-side review is excluded, so it adds no points.
        Assert.Equal(110, response.ReputationScore);
        // Display aggregates pass through unchanged.
        Assert.Equal(4.8m, response.AverageRating);
        Assert.Equal(12, response.ReviewCount);
        Assert.Equal(9, response.CompletedTaskCount);
    }

    [Fact]
    public async Task GetOwnProfileAsync_PopulatesReputationScore_FromCalculator()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);
        var profile = await db.Profiles.FindAsync([profileId], cancellationToken);
        Assert.NotNull(profile);
        profile.AverageRating = 5m;
        profile.ReviewCount = 4;
        profile.CompletedTaskCount = 3;
        var privacySettings = new ProfilePrivacySettings
        {
            Id = Guid.NewGuid(),
            ProfileId = profile.Id,
            ShowWorkplace = true,
            ShowPosition = true,
            ShowLocation = true,
            ShowAvailability = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        profile.PrivacySettings = privacySettings;
        db.ProfilePrivacySettings.Add(privacySettings);

        // Four reviews earned AS THE HELPER feed the score's review component.
        var seeker = CreateProfile("Seeker");
        var reviewer = CreateProfile("Reviewer");
        var category = CreateCategory("repairs", "Repairs", "Wrench01Icon");
        var now = DateTimeOffset.UtcNow;
        var helperTask = CreateTask(
            "Helped", seeker, profile, category, DomainTaskStatus.Completed, now.AddDays(-2), now.AddDays(-1));
        db.Profiles.AddRange(seeker, reviewer);
        db.Categories.Add(category);
        db.Tasks.Add(helperTask);
        for (var i = 0; i < 4; i++)
        {
            db.Reviews.Add(CreateReview(helperTask, reviewer, profile, 5, now));
        }
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateProfilesController(db, userId);
        var result = await controller.GetOwnProfileAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OwnProfileResponse>(ok.Value);
        // 3 x 10 (completed) + 5.00 avg x 4 helper-side reviews x 2 = 70
        Assert.Equal(70, response.ReputationScore);
    }

    [Fact]
    public async Task GetOwnPointsBalanceAsync_ReturnsSignedSumOfOwnEntries_IgnoringOtherProfiles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);
        var other = CreateProfile("Other");

        db.Profiles.Add(other);
        var now = DateTimeOffset.UtcNow;
        db.PointsLedger.AddRange(
            CreateLedgerEntry(profileId, 10, now.AddMinutes(-3)),
            CreateLedgerEntry(profileId, 15, now.AddMinutes(-2)),
            // A redemption is a negative entry; the balance must net it out.
            CreateLedgerEntry(profileId, -5, now.AddMinutes(-1), PointEntryType.Redemption),
            // Another profile's entry must not leak into this user's balance.
            CreateLedgerEntry(other.Id, 100, now));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateProfilesController(db, userId);
        var result = await controller.GetOwnPointsBalanceAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PointsBalanceResponse>(ok.Value);
        Assert.Equal(profileId, response.ProfileId);
        Assert.Equal(20, response.Balance);
    }

    [Fact]
    public async Task GetOwnPointsBalanceAsync_ReturnsZero_WhenNoEntries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);

        var controller = CreateProfilesController(db, userId);
        var result = await controller.GetOwnPointsBalanceAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PointsBalanceResponse>(ok.Value);
        Assert.Equal(profileId, response.ProfileId);
        Assert.Equal(0, response.Balance);
    }

    [Fact]
    public async Task GetOwnPointsBalanceAsync_ReturnsNotFound_WhenUserHasNoProfile()
    {
        await using var db = CreateDbContext();
        var controller = CreateProfilesController(db, Guid.NewGuid());

        var result = await controller.GetOwnPointsBalanceAsync(TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetOwnPointsBalanceAsync_ReturnsUnauthorized_WhenNoClaim()
    {
        await using var db = CreateDbContext();
        var controller = CreateProfilesControllerWithoutAuth(db);

        var result = await controller.GetOwnPointsBalanceAsync(TestContext.Current.CancellationToken);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetOwnPointsLedgerAsync_ReturnsEntriesNewestFirst_ScopedToOwnProfile()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);
        var other = CreateProfile("Other");

        db.Profiles.Add(other);
        var now = DateTimeOffset.UtcNow;
        db.PointsLedger.AddRange(
            CreateLedgerEntry(profileId, 10, now.AddMinutes(-2), description: "older"),
            CreateLedgerEntry(profileId, 15, now.AddMinutes(-1), description: "newer"),
            CreateLedgerEntry(other.Id, 100, now, description: "other profile"));
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateProfilesController(db, userId);
        var result = await controller.GetOwnPointsLedgerAsync(page: 1, pageSize: 20, cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<PointsLedgerPagedResponse>(ok.Value);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(1, paged.TotalPages);
        Assert.Equal(["newer", "older"], paged.Items.Select(entry => entry.Description));
        Assert.Equal(
            nameof(PointEntryType.TaskCompletedReward),
            paged.Items[0].EntryType);
    }

    [Fact]
    public async Task GetOwnPointsLedgerAsync_RespectsPaginationParameters()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 3; i++)
        {
            db.PointsLedger.Add(CreateLedgerEntry(profileId, 10, now.AddMinutes(i), description: $"entry {i}"));
        }
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateProfilesController(db, userId);

        // Page 1, size 2 → newest 2.
        var page1Result = await controller.GetOwnPointsLedgerAsync(page: 1, pageSize: 2, cancellationToken);
        var page1 = Assert.IsType<PointsLedgerPagedResponse>(Assert.IsType<OkObjectResult>(page1Result).Value);
        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
        Assert.Equal(["entry 2", "entry 1"], page1.Items.Select(entry => entry.Description));

        // Page 2, size 2 → oldest 1.
        var page2Result = await controller.GetOwnPointsLedgerAsync(page: 2, pageSize: 2, cancellationToken);
        var page2 = Assert.IsType<PointsLedgerPagedResponse>(Assert.IsType<OkObjectResult>(page2Result).Value);
        Assert.Single(page2.Items);
        Assert.Equal("entry 0", page2.Items[0].Description);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task GetOwnPointsLedgerAsync_InvalidPagination_ReturnsValidationProblem(int page, int pageSize)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);

        var controller = CreateProfilesController(db, userId);
        var result = await controller.GetOwnPointsLedgerAsync(page, pageSize, cancellationToken);

        var problem = Assert.IsAssignableFrom<ObjectResult>(result);
        Assert.IsAssignableFrom<ValidationProblemDetails>(problem.Value);
    }

    [Fact]
    public async Task GetOwnPointsLedgerAsync_ReturnsNotFound_WhenUserHasNoProfile()
    {
        await using var db = CreateDbContext();
        var controller = CreateProfilesController(db, Guid.NewGuid());

        var result = await controller.GetOwnPointsLedgerAsync(
            page: 1, pageSize: 20, TestContext.Current.CancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    private static PointsLedgerEntry CreateLedgerEntry(
        Guid profileId,
        int amount,
        DateTimeOffset createdAt,
        PointEntryType entryType = PointEntryType.TaskCompletedReward,
        Guid? taskId = null,
        string? description = null)
    {
        return new PointsLedgerEntry
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            TaskId = taskId,
            Amount = amount,
            EntryType = entryType,
            Description = description,
            CreatedAt = createdAt
        };
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

    private static Review CreateReview(
        CommunityTask task,
        UserProfile reviewer,
        UserProfile reviewee,
        int rating,
        DateTimeOffset createdAt)
    {
        return new Review
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            Task = task,
            ReviewerProfileId = reviewer.Id,
            ReviewerProfile = reviewer,
            RevieweeProfileId = reviewee.Id,
            RevieweeProfile = reviewee,
            Rating = rating,
            Comment = "Review comment",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    private static CommunityTask CreateTask(
        string title,
        UserProfile seeker,
        UserProfile? acceptedHelper,
        Category category,
        DomainTaskStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt = null)
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
            UpdatedAt = completedAt ?? createdAt,
            CompletedAt = completedAt
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

    private static ProfilesController CreateProfilesControllerWithoutAuth(AppDbContext db)
    {
        return new ProfilesController(db, new NullBlobStorageService(), NullLogger<ProfilesController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };
    }

    private sealed class NullBlobStorageService : IBlobStorageService
    {
        public Task<Uri> UploadProfilePhotoAsync(Guid userId, Stream content, string contentType, string fileExtension, CancellationToken cancellationToken)
            => Task.FromResult(new Uri("https://example.com/photo"));

        public Task DeleteBlobByUrlAsync(string blobUrl, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
            => Task.CompletedTask;

        public string? RewriteToPublicUrl(string? url) => url;
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
