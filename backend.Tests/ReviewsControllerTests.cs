using System.Security.Claims;
using Backend.Application.Reviews;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Profiles;
using Backend.Features.Reviews;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xunit;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace backend.Tests;

public sealed class ReviewsControllerTests
{
    [Fact]
    public async Task PostReviewAsync_SeekerReviewsHelper_CreatesReviewAndUpdatesHelperReputation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 4, Comment = "Great help!" },
            cancellationToken);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var response = Assert.IsType<ProfileReviewResponse>(created.Value);
        Assert.Equal(scenario.SeekerProfileId, response.AuthorId);
        Assert.Equal(scenario.HelperProfileId, response.TargetUserId);
        Assert.Equal(4, response.Rating);
        Assert.Equal("Great help!", response.Comment);

        var review = Assert.Single(db.Reviews);
        Assert.Equal(scenario.SeekerProfileId, review.ReviewerProfileId);
        Assert.Equal(scenario.HelperProfileId, review.RevieweeProfileId);
        Assert.Equal(4, review.Rating);

        // Helper's reputation stats should be updated.
        var helperProfile = await db.Profiles.AsNoTracking()
            .FirstAsync(p => p.Id == scenario.HelperProfileId, cancellationToken);
        Assert.Equal(1, helperProfile.ReviewCount);
        Assert.Equal(4.00m, helperProfile.AverageRating);

        Assert.Contains(db.ActivityEvents, a => a.EventType == ActivityEventType.ReviewSubmitted);
        Assert.Contains(db.AuditEvents, a => a.EventType == "review.submitted");
    }

    [Fact]
    public async Task PostReviewAsync_SeekerReviewsHelper_AwardsReviewQualityBonus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 5 },
            cancellationToken);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);

        var bonus = Assert.Single(db.PointsLedger);
        Assert.Equal(PointEntryType.ReviewQualityBonus, bonus.EntryType);
        Assert.Equal(scenario.HelperProfileId, bonus.ProfileId);
        Assert.Equal(scenario.TaskId, bonus.TaskId);
        Assert.Equal(2 * ReviewQualityBonusCalculator.PointsPerStar, bonus.Amount);
    }

    [Fact]
    public async Task PostReviewAsync_HelperReviewsSeeker_AwardsNoBonus()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 5 },
            cancellationToken);

        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(result).StatusCode);

        // A helper reviewing the seeker does not earn the helper a quality bonus.
        Assert.Empty(db.PointsLedger);
    }

    [Fact]
    public async Task PostReviewAsync_HelperReviewsSeeker_CreatesReviewAndUpdatesSeekerReputation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var controller = CreateController(db, scenario.HelperUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 5, Comment = "Pleasure to work with." },
            cancellationToken);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var review = Assert.Single(db.Reviews);
        Assert.Equal(scenario.HelperProfileId, review.ReviewerProfileId);
        Assert.Equal(scenario.SeekerProfileId, review.RevieweeProfileId);
        Assert.Equal(5, review.Rating);

        var seekerProfile = await db.Profiles.AsNoTracking()
            .FirstAsync(p => p.Id == scenario.SeekerProfileId, cancellationToken);
        Assert.Equal(1, seekerProfile.ReviewCount);
        Assert.Equal(5.00m, seekerProfile.AverageRating);
    }

    [Fact]
    public async Task PostReviewAsync_AverageRatingIsRecalculated_AcrossMultipleReviews()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);

        // Pre-seed the helper with two actual review rows from different
        // tasks, ratings 2 and 4 (sum=6, avg=3.00). The controller now
        // recomputes the aggregate from persisted rows, so the denormalised
        // stats must be backed by real Review rows for the test to be valid.
        var now = DateTimeOffset.UtcNow;
        var otherReviewer1 = Guid.NewGuid();
        var otherReviewer2 = Guid.NewGuid();
        var otherTask1 = Guid.NewGuid();
        var otherTask2 = Guid.NewGuid();
        db.Profiles.Add(new UserProfile
        {
            Id = otherReviewer1,
            UserId = Guid.NewGuid(),
            DisplayName = "Other 1",
            CreatedAt = now,
            UpdatedAt = now,
        });
        db.Profiles.Add(new UserProfile
        {
            Id = otherReviewer2,
            UserId = Guid.NewGuid(),
            DisplayName = "Other 2",
            CreatedAt = now,
            UpdatedAt = now,
        });
        db.Reviews.AddRange(
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = otherTask1,
                ReviewerProfileId = otherReviewer1,
                RevieweeProfileId = scenario.HelperProfileId,
                Rating = 2,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new Review
            {
                Id = Guid.NewGuid(),
                TaskId = otherTask2,
                ReviewerProfileId = otherReviewer2,
                RevieweeProfileId = scenario.HelperProfileId,
                Rating = 4,
                CreatedAt = now,
                UpdatedAt = now,
            });
        var helperProfile = await db.Profiles.FirstAsync(p => p.Id == scenario.HelperProfileId, cancellationToken);
        helperProfile.ReviewCount = 2;
        helperProfile.AverageRating = 3.00m;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.SeekerUserId);

        await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 5 },
            cancellationToken);

        // After recompute: ratings [2, 4, 5] → count 3, avg (2+4+5)/3 = 3.67.
        var updated = await db.Profiles.AsNoTracking()
            .FirstAsync(p => p.Id == scenario.HelperProfileId, cancellationToken);
        Assert.Equal(3, updated.ReviewCount);
        Assert.Equal(3.67m, updated.AverageRating);
    }

    [Fact]
    public async Task PostReviewAsync_TaskNotFound_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PostReviewAsync(
            Guid.NewGuid(),
            new PostReviewRequest { Rating = 3 },
            cancellationToken);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(db.Reviews);
    }

    [Fact]
    public async Task PostReviewAsync_TaskNotCompleted_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = DomainTaskStatus.InProgress;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 3 },
            cancellationToken);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
        Assert.Empty(db.Reviews);
    }

    [Theory]
    [InlineData(DomainTaskStatus.Open)]
    [InlineData(DomainTaskStatus.InProgress)]
    [InlineData(DomainTaskStatus.PendingApproval)]
    [InlineData(DomainTaskStatus.Cancelled)]
    public async Task PostReviewAsync_AllNonCompletedStatuses_ReturnConflict(DomainTaskStatus status)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);
        var task = await db.Tasks.FirstAsync(t => t.Id == scenario.TaskId, cancellationToken);
        task.Status = status;
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, scenario.SeekerUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 3 },
            cancellationToken);

        var problem = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, problem.StatusCode);
    }

    [Fact]
    public async Task PostReviewAsync_CallerNotParticipant_ReturnsForbid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var scenario = await SeedCompletedTaskAsync(db, cancellationToken);

        // A third user who is not the seeker or helper.
        var outsiderUserId = Guid.NewGuid();
        var outsiderProfileId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Profiles.Add(new UserProfile
        {
            Id = outsiderProfileId,
            UserId = outsiderUserId,
            DisplayName = "Outsider",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, outsiderUserId);

        var result = await controller.PostReviewAsync(
            scenario.TaskId,
            new PostReviewRequest { Rating = 1 },
            cancellationToken);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(db.Reviews);
    }

    // ── PostgresExceptionHelpers.IsDuplicateReview ────────────────────────────

    [Fact]
    public void IsDuplicateReview_MatchesUniqueReviewConstraint()
    {
        // Mirrors the shape Npgsql actually emits when the unique index
        // ux_reviews_task_reviewer rejects a second review from the same
        // reviewer on the same task. This is the only path that turns the
        // duplicate insert into a 409 in PostReviewAsync — without coverage,
        // a constraint rename or SqlState change would silently regress
        // duplicate posts to a 500.
        var postgresException = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: null,
            tableName: "reviews",
            columnName: null,
            dataTypeName: null,
            constraintName: "ux_reviews_task_reviewer",
            file: null,
            line: null,
            routine: null);
        var exception = new DbUpdateException("Duplicate review.", postgresException);

        Assert.True(PostgresExceptionHelpers.IsDuplicateReview(exception));
    }

    [Fact]
    public void IsDuplicateReview_DoesNotMatchUnrelatedConstraint()
    {
        // A different unique constraint must not be mistaken for the review
        // dedupe — otherwise unrelated 23505s would surface as "Already
        // reviewed" 409s on the review endpoint.
        var postgresException = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: null,
            tableName: "user_terms_acceptances",
            columnName: null,
            dataTypeName: null,
            constraintName: "ux_user_terms_acceptances_user_terms",
            file: null,
            line: null,
            routine: null);
        var exception = new DbUpdateException("Duplicate.", postgresException);

        Assert.False(PostgresExceptionHelpers.IsDuplicateReview(exception));
    }

    // ── PostReviewRequest validation (DataAnnotations) ────────────────────────

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(6, false)]
    public void PostReviewRequest_RatingBounds_Validated(int rating, bool expectedValid)
    {
        var request = new PostReviewRequest { Rating = rating };
        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(request);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = System.ComponentModel.DataAnnotations.Validator
            .TryValidateObject(request, ctx, results, validateAllProperties: true);

        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.Contains(
                results,
                r => r.MemberNames.Contains(nameof(PostReviewRequest.Rating)));
        }
    }

    [Fact]
    public void PostReviewRequest_CommentMaxLength_2000_Enforced()
    {
        var request = new PostReviewRequest
        {
            Rating = 5,
            Comment = new string('x', 2001),
        };
        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(request);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        var isValid = System.ComponentModel.DataAnnotations.Validator
            .TryValidateObject(request, ctx, results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(
            results,
            r => r.MemberNames.Contains(nameof(PostReviewRequest.Comment)));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<ReviewScenario> SeedCompletedTaskAsync(
        AppDbContext db,
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
            PublicCode = "TASK-REV-000001",
            SeekerProfileId = seekerProfileId,
            AcceptedHelperProfileId = helperProfileId,
            CategoryId = categoryId,
            Title = "Help needed",
            Description = "Some task.",
            CompensationType = CompensationType.Voluntary,
            Status = DomainTaskStatus.Completed,
            CreatedAt = now,
            UpdatedAt = now,
            AcceptedAt = now,
            CompletedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        return new ReviewScenario(taskId, seekerUserId, seekerProfileId, helperUserId, helperProfileId);
    }

    private static AppDbContext CreateDbContext()
    {
        // The InMemory provider does not support transactions and raises
        // TransactionIgnoredWarning, which is configured as an error in the
        // app. ReviewsController wraps the review insert in a Serializable
        // transaction for race-safety against real Postgres, so we have to
        // suppress that warning under InMemory tests.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static ReviewsController CreateController(AppDbContext db, Guid userId)
    {
        return new ReviewsController(
            db,
            new PassthroughBlobStorageService(),
            new TaskReviewBonusService(db, new ReviewQualityBonusCalculator()))
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

    /// <summary>
    /// Stub that returns URLs unchanged so tests don't need a real blob service.
    /// </summary>
    private sealed class PassthroughBlobStorageService : IBlobStorageService
    {
        public string? RewriteToPublicUrl(string? url) => url;

        public Task<Uri> UploadProfilePhotoAsync(
            Guid userId,
            Stream stream,
            string contentType,
            string fileExtension,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not used in these tests.");

        public Task DeleteBlobByUrlAsync(string url, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not used in these tests.");

        public Task EnsureContainerExistsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not used in these tests.");
    }

    private sealed record ReviewScenario(
        Guid TaskId,
        Guid SeekerUserId,
        Guid SeekerProfileId,
        Guid HelperUserId,
        Guid HelperProfileId);
}
