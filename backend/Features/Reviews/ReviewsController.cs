using System.Data;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Features.Profiles;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Security;
using Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Reviews;

[ApiController]
[Route("api/tasks/{taskId:guid}/reviews")]
[Authorize]
public sealed partial class ReviewsController(
    AppDbContext db,
    IBlobStorageService blobStorage) : ControllerBase
{
    /// <summary>
    /// Submit a review for the other participant in a completed task.
    /// The seeker reviews the helper; the helper reviews the seeker.
    /// </summary>
    /// <param name="taskId">The ID of the completed task being reviewed.</param>
    /// <param name="request">Rating (1-5) and optional comment.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 201 Created with the new review.
    /// 404 Not Found if the task does not exist.
    /// 409 Conflict if the task is not completed, has no helper, or a review was already submitted.
    /// 403 Forbidden if the caller is not a participant of the task.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.ReviewsPolicy)]
    public async Task<IActionResult> PostReviewAsync(
        Guid taskId,
        PostReviewRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await GetCurrentProfileAsync(cancellationToken);
        if (profile is null)
        {
            return Unauthorized();
        }

        var task = await db.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task is null)
        {
            return NotFound();
        }

        if (task.Status != DomainTaskStatus.Completed)
        {
            return Problem(
                title: "Task is not completed",
                detail: "Reviews can only be submitted for completed tasks.",
                statusCode: StatusCodes.Status409Conflict);
        }

        Guid revieweeProfileId;
        if (task.SeekerProfileId == profile.Id)
        {
            if (task.AcceptedHelperProfileId is null)
            {
                return Problem(
                    title: "No helper to review",
                    detail: "This task has no accepted helper.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            revieweeProfileId = task.AcceptedHelperProfileId.Value;
        }
        else if (task.AcceptedHelperProfileId == profile.Id)
        {
            revieweeProfileId = task.SeekerProfileId;
        }
        else
        {
            return Forbid();
        }

        var now = DateTimeOffset.UtcNow;

        // Serializable isolation prevents two concurrent reviewers from both
        // reading the same baseline aggregate and writing back with stale +1.
        // Combined with the post-insert recompute below, this guarantees the
        // denormalised stats always match the actual review rows.
        //
        // The trade-off is that Postgres can raise SQLSTATE 40001
        // (serialization_failure) when two transactions collide; the
        // application is expected to retry. We try a handful of times with a
        // small linear backoff before letting the failure bubble up.
        const int maxAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            var review = new Review
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ReviewerProfileId = profile.Id,
                RevieweeProfileId = revieweeProfileId,
                Rating = request.Rating,
                Comment = request.Comment?.Trim(),
                CreatedAt = now,
                UpdatedAt = now,
            };

            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

                db.Reviews.Add(review);

                db.AddActivityEvent(
                    profile.UserId,
                    profile.Id,
                    ActivityEventType.ReviewSubmitted,
                    nameof(Review),
                    review.Id,
                    new { TaskId = taskId, RevieweeProfileId = revieweeProfileId, request.Rating });

                db.AddAuditEvent(
                    profile.UserId,
                    "review.submitted",
                    nameof(Review),
                    review.Id,
                    new
                    {
                        TaskId = taskId,
                        ReviewerProfileId = profile.Id,
                        RevieweeProfileId = revieweeProfileId,
                        request.Rating,
                    });

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (PostgresExceptionHelpers.IsDuplicateReview(ex))
                {
                    return Problem(
                        title: "Already reviewed",
                        detail: "You have already submitted a review for this task.",
                        statusCode: StatusCodes.Status409Conflict);
                }

                // Recompute the reputation aggregate from the persisted review rows
                // (which now includes the row we just inserted) rather than mutating
                // the denormalised stats in memory. This eliminates the lost-update
                // race the in-memory read-modify-write was vulnerable to.
                var stats = await db.Reviews
                    .Where(r => r.RevieweeProfileId == revieweeProfileId)
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        Count = g.Count(),
                        Sum = g.Sum(r => (decimal)r.Rating),
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                var revieweeProfile = await db.Profiles
                    .FirstOrDefaultAsync(p => p.Id == revieweeProfileId, cancellationToken);

                if (revieweeProfile is not null && stats is not null && stats.Count > 0)
                {
                    revieweeProfile.ReviewCount = stats.Count;
                    revieweeProfile.AverageRating = decimal.Round(
                        stats.Sum / stats.Count,
                        2,
                        MidpointRounding.AwayFromZero);
                    revieweeProfile.UpdatedAt = now;
                    await db.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                var response = new ProfileReviewResponse(
                    review.Id,
                    profile.Id,
                    profile.DisplayName,
                    blobStorage.RewriteToPublicUrl(profile.PhotoUrl),
                    revieweeProfileId,
                    review.Rating,
                    review.Comment ?? string.Empty,
                    review.CreatedAt);

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (DbUpdateException ex) when (
                attempt < maxAttempts &&
                ex.InnerException is PostgresException pg &&
                pg.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                // Serialization conflict on a concurrent reviewer: discard
                // tracked state from this failed attempt so the next one can
                // re-add the review entity cleanly, then back off briefly.
                db.ChangeTracker.Clear();
                await Task.Delay(TimeSpan.FromMilliseconds(20 * attempt), cancellationToken);
            }
        }
    }
}
