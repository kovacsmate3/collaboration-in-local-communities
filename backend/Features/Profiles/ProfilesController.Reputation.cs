using Backend.Domain.Reputation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Profiles;

public sealed partial class ProfilesController
{
    private const int ReputationTrendPointLimit = 12;

    /// <summary>
    /// Get cumulative reputation snapshots for a profile.
    /// </summary>
    /// <param name="id">The profile ID whose reputation trend should be returned.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with daily reputation snapshots ordered oldest to newest.
    /// 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpGet("{id:guid}/reputation-trend")]
    public async Task<IActionResult> GetProfileReputationTrendAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await ProfileExistsAsync(id, cancellationToken))
        {
            return NotFound();
        }

        return Ok(await BuildReputationTrendAsync(id, cancellationToken));
    }

    private async Task<IReadOnlyList<ProfileReputationTrendPointResponse>> BuildReputationTrendAsync(
        Guid profileId,
        CancellationToken cancellationToken)
    {
        var reviewEvents = await db.Reviews
            .AsNoTracking()
            .Where(review => review.RevieweeProfileId == profileId)
            .Select(review => new
            {
                OccurredAt = review.CreatedAt,
                Rating = (int?)review.Rating,
                CountsCompletedTask = false
            })
            .ToListAsync(cancellationToken);

        var completedTaskEvents = await db.Tasks
            .AsNoTracking()
            .Where(task =>
                task.Status == DomainTaskStatus.Completed
                && (task.SeekerProfileId == profileId || task.AcceptedHelperProfileId == profileId))
            .Select(task => new
            {
                OccurredAt = task.CompletedAt ?? task.UpdatedAt,
                Rating = (int?)null,
                CountsCompletedTask = true
            })
            .ToListAsync(cancellationToken);

        var events = reviewEvents
            .Concat(completedTaskEvents)
            .OrderBy(reputationEvent => reputationEvent.OccurredAt)
            .ThenBy(reputationEvent => reputationEvent.CountsCompletedTask)
            .ToList();

        var dailySnapshots = new List<ProfileReputationTrendPointResponse>();
        decimal ratingSum = 0;
        var reviewCount = 0;
        var completedTaskCount = 0;

        foreach (var reputationEvent in events)
        {
            if (reputationEvent.CountsCompletedTask)
            {
                completedTaskCount++;
            }

            if (reputationEvent.Rating is { } rating)
            {
                ratingSum += rating;
                reviewCount++;
            }

            var averageRating = reviewCount > 0
                ? decimal.Round(ratingSum / reviewCount, 2, MidpointRounding.AwayFromZero)
                : 0m;
            var snapshot = new ProfileReputationTrendPointResponse(
                DateOnly.FromDateTime(reputationEvent.OccurredAt.UtcDateTime),
                ReputationScoreCalculator.Compute(averageRating, reviewCount, completedTaskCount),
                averageRating,
                reviewCount,
                completedTaskCount);

            if (dailySnapshots.Count > 0 && dailySnapshots[^1].Date == snapshot.Date)
            {
                dailySnapshots[^1] = snapshot;
            }
            else
            {
                dailySnapshots.Add(snapshot);
            }
        }

        return dailySnapshots
            .TakeLast(ReputationTrendPointLimit)
            .ToList();
    }
}
