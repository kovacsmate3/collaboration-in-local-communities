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
        // Only reviews earned as a task's accepted helper feed the reputation
        // score; reviews received as a seeker (task-giver) are excluded here so
        // the trend matches the headline score on the profile responses.
        var reviewEventDaysQuery = db.Reviews
            .AsNoTracking()
            .Where(review =>
                review.RevieweeProfileId == profileId
                && review.Task.AcceptedHelperProfileId == profileId)
            .Select(review => new
            {
                review.CreatedAt.Year,
                review.CreatedAt.Month,
                review.CreatedAt.Day
            });

        // Only tasks completed as the helper count toward the score's
        // completed-task component, consistent with how CompletedTaskCount is
        // maintained on the profile and with excluding seeker-side reviews.
        var completedTaskEventDaysQuery = db.Tasks
            .AsNoTracking()
            .Where(task =>
                task.Status == DomainTaskStatus.Completed
                && task.AcceptedHelperProfileId == profileId)
            .Select(task => new
            {
                (task.CompletedAt ?? task.UpdatedAt).Year,
                (task.CompletedAt ?? task.UpdatedAt).Month,
                (task.CompletedAt ?? task.UpdatedAt).Day
            });

        var trendDaysDescending = await reviewEventDaysQuery
            .Concat(completedTaskEventDaysQuery)
            .Distinct()
            .OrderByDescending(day => day.Year)
            .ThenByDescending(day => day.Month)
            .ThenByDescending(day => day.Day)
            .Take(ReputationTrendPointLimit)
            .ToListAsync(cancellationToken);

        if (trendDaysDescending.Count == 0)
        {
            return Array.Empty<ProfileReputationTrendPointResponse>();
        }

        var trendDays = trendDaysDescending
            .Select(day => new DateOnly(day.Year, day.Month, day.Day))
            .OrderBy(day => day)
            .ToList();

        var earliestTrendDay = trendDays[0];
        var earliestTrendStartUtc = new DateTimeOffset(
            earliestTrendDay.Year,
            earliestTrendDay.Month,
            earliestTrendDay.Day,
            0,
            0,
            0,
            TimeSpan.Zero);

        var reviewBaseline = await db.Reviews
            .AsNoTracking()
            .Where(review =>
                review.RevieweeProfileId == profileId
                && review.Task.AcceptedHelperProfileId == profileId
                && review.CreatedAt < earliestTrendStartUtc)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                RatingSum = group.Sum(review => (decimal?)review.Rating) ?? 0m,
                ReviewCount = group.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var completedTaskBaseline = await db.Tasks
            .AsNoTracking()
            .Where(task =>
                task.Status == DomainTaskStatus.Completed
                && task.AcceptedHelperProfileId == profileId
                && (task.CompletedAt ?? task.UpdatedAt) < earliestTrendStartUtc)
            .CountAsync(cancellationToken);

        var reviewDailyAggregates = await db.Reviews
            .AsNoTracking()
            .Where(review =>
                review.RevieweeProfileId == profileId
                && review.Task.AcceptedHelperProfileId == profileId
                && review.CreatedAt >= earliestTrendStartUtc)
            .GroupBy(review => new
            {
                review.CreatedAt.Year,
                review.CreatedAt.Month,
                review.CreatedAt.Day
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                RatingSum = group.Sum(review => (decimal)review.Rating),
                ReviewCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var completedTaskDailyAggregates = await db.Tasks
            .AsNoTracking()
            .Where(task =>
                task.Status == DomainTaskStatus.Completed
                && task.AcceptedHelperProfileId == profileId
                && (task.CompletedAt ?? task.UpdatedAt) >= earliestTrendStartUtc)
            .GroupBy(task => new
            {
                (task.CompletedAt ?? task.UpdatedAt).Year,
                (task.CompletedAt ?? task.UpdatedAt).Month,
                (task.CompletedAt ?? task.UpdatedAt).Day
            })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.Day,
                CompletedTaskCount = group.Count()
            })
            .ToListAsync(cancellationToken);

        var reviewDailyAggregateMap = reviewDailyAggregates.ToDictionary(
            aggregate => new DateOnly(aggregate.Year, aggregate.Month, aggregate.Day),
            aggregate => new { aggregate.RatingSum, aggregate.ReviewCount });

        var completedTaskDailyAggregateMap = completedTaskDailyAggregates.ToDictionary(
            aggregate => new DateOnly(aggregate.Year, aggregate.Month, aggregate.Day),
            aggregate => aggregate.CompletedTaskCount);

        var dailySnapshots = new List<ProfileReputationTrendPointResponse>(trendDays.Count);
        decimal ratingSum = reviewBaseline?.RatingSum ?? 0m;
        var reviewCount = reviewBaseline?.ReviewCount ?? 0;
        var completedTaskCount = completedTaskBaseline;

        foreach (var trendDay in trendDays)
        {
            if (reviewDailyAggregateMap.TryGetValue(trendDay, out var reviewAggregate))
            {
                ratingSum += reviewAggregate.RatingSum;
                reviewCount += reviewAggregate.ReviewCount;
            }

            if (completedTaskDailyAggregateMap.TryGetValue(trendDay, out var completedTasksOnDay))
            {
                completedTaskCount += completedTasksOnDay;
            }

            var averageRating = reviewCount > 0
                ? decimal.Round(ratingSum / reviewCount, 2, MidpointRounding.AwayFromZero)
                : 0m;

            dailySnapshots.Add(new ProfileReputationTrendPointResponse(
                trendDay,
                ReputationScoreCalculator.Compute(averageRating, reviewCount, completedTaskCount),
                averageRating,
                reviewCount,
                completedTaskCount));
        }

        return dailySnapshots;
    }
}
