using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the demo marketplace dataset — sample seeker accounts plus the curated
/// <see cref="BudapestSampleData.Tasks"/> Budapest community tasks — so a fresh
/// development database has realistic data for demos and manual testing.
/// </summary>
/// <remarks>
/// <para>
/// Idempotent: each task carries a stable <c>PublicCode</c> derived from its
/// position in <see cref="BudapestSampleData.Tasks"/>, and only codes not
/// already present are inserted. Re-running on every startup is therefore safe.
/// </para>
/// <para>
/// Categories themselves are static reference data seeded via EF Core
/// <c>HasData</c> (see <c>CategoryConfiguration</c>); this seeder only links
/// tasks to those existing categories by their stable <c>Code</c>.
/// </para>
/// </remarks>
public sealed class SampleTaskSeeder(
    UserSeedingHelper userSeedingHelper,
    AppDbContext db,
    ILogger<SampleTaskSeeder> logger) : IDataSeeder
{
    private const int Wgs84Srid = 4326;

    /// <summary>
    /// Gets the relative ordering hint. Runs after the account seeders so any
    /// shared infrastructure (roles, active terms) is already in place.
    /// </summary>
    public int Order => 200;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seekerProfileIds = await EnsureSeekersAsync(cancellationToken);
        var categoryIdsByCode = await LoadCategoryIdsByCodeAsync(cancellationToken);

        var existingCodes = await db.Tasks
            .Where(task => task.PublicCode != null
                && task.PublicCode.StartsWith(BudapestSampleData.TaskCodePrefix))
            .Select(task => task.PublicCode!)
            .ToListAsync(cancellationToken);
        var existing = existingCodes.ToHashSet(StringComparer.Ordinal);

        var now = DateTimeOffset.UtcNow;
        var inserted = 0;

        for (var index = 0; index < BudapestSampleData.Tasks.Count; index++)
        {
            var publicCode = $"{BudapestSampleData.TaskCodePrefix}{index + 1:D4}";
            if (existing.Contains(publicCode))
            {
                continue;
            }

            var template = BudapestSampleData.Tasks[index];
            if (!categoryIdsByCode.TryGetValue(template.CategoryCode, out var categoryId))
            {
                logger.LogWarning(
                    "Skipping sample task {PublicCode}: unknown category code '{Code}'.",
                    publicCode,
                    template.CategoryCode);
                continue;
            }

            db.Tasks.Add(BuildTask(template, publicCode, categoryId, seekerProfileIds, index, now));
            inserted++;
        }

        if (inserted > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Sample tasks ready ({Inserted} inserted, {Total} total in catalogue).",
                inserted,
                BudapestSampleData.Tasks.Count);
        }
    }

    private static CommunityTask BuildTask(
        SampleTask template,
        string publicCode,
        Guid categoryId,
        IReadOnlyList<Guid> seekerProfileIds,
        int index,
        DateTimeOffset now)
    {
        var seekerCount = seekerProfileIds.Count;
        var seekerProfileId = seekerProfileIds[index % seekerCount];

        // Stagger creation timestamps so the demo feed has a natural ordering.
        var createdAt = now.AddHours(-index * 6);

        var task = new CommunityTask
        {
            Id = Guid.NewGuid(),
            PublicCode = publicCode,
            SeekerProfileId = seekerProfileId,
            CategoryId = categoryId,
            Title = template.Title,
            Description = template.Description,
            Location = new Point(template.Longitude, template.Latitude) { SRID = Wgs84Srid },
            LocationText = template.LocationText,
            CompensationType = template.CompensationType,
            CompensationAmount = template.CompensationType == CompensationType.Paid ? template.Amount : null,
            Status = template.Status,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        ApplyLifecycle(task, template.Status, seekerProfileIds, index, createdAt);
        return task;
    }

    /// <summary>
    /// Fill in the status-dependent fields (accepted helper, lifecycle
    /// timestamps, cancellation reason) so every non-open task is internally
    /// consistent with the domain's check constraints.
    /// </summary>
    private static void ApplyLifecycle(
        CommunityTask task,
        DomainTaskStatus status,
        IReadOnlyList<Guid> seekerProfileIds,
        int index,
        DateTimeOffset createdAt)
    {
        if (status == DomainTaskStatus.Open)
        {
            return;
        }

        if (status == DomainTaskStatus.Cancelled)
        {
            task.CancelledAt = createdAt.AddHours(3);
            task.CancellationReason = "A feladatra már nincs szükség.";
            task.UpdatedAt = task.CancelledAt.Value;
            return;
        }

        // InProgress / PendingApproval / Completed all have an accepted helper —
        // a different sample profile than the seeker (ck_tasks_distinct_profiles).
        var seekerCount = seekerProfileIds.Count;
        task.AcceptedHelperProfileId = seekerProfileIds[(index + 1) % seekerCount];
        task.AcceptedAt = createdAt.AddHours(2);
        task.UpdatedAt = task.AcceptedAt.Value;

        if (status == DomainTaskStatus.Completed)
        {
            task.CompletedAt = createdAt.AddHours(26);
            task.UpdatedAt = task.CompletedAt.Value;
        }
    }

    private async Task<IReadOnlyList<Guid>> EnsureSeekersAsync(CancellationToken cancellationToken)
    {
        var profileIds = new List<Guid>(BudapestSampleData.Seekers.Count);

        foreach (var seeker in BudapestSampleData.Seekers)
        {
            var account = new DevSeedAccount
            {
                Email = seeker.Email,
                Password = BudapestSampleData.SeekerPassword,
                DisplayName = seeker.DisplayName,
                LocationText = $"Budapest, {seeker.District}",
                Bio = seeker.Bio,
            };

            var user = await userSeedingHelper.EnsureUserAsync(
                account,
                [ApplicationRoleNames.User],
                cancellationToken);

            var profileId = await db.Profiles
                .Where(profile => profile.UserId == user.Id)
                .Select(profile => profile.Id)
                .FirstAsync(cancellationToken);

            profileIds.Add(profileId);
        }

        return profileIds;
    }

    private async Task<Dictionary<string, Guid>> LoadCategoryIdsByCodeAsync(
        CancellationToken cancellationToken)
    {
        return await db.Set<Category>()
            .ToDictionaryAsync(
                category => category.Code,
                category => category.Id,
                StringComparer.Ordinal,
                cancellationToken);
    }
}
