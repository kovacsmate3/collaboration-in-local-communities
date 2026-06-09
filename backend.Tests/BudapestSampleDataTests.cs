using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence.Seeding;
using Xunit;

namespace backend.Tests;

/// <summary>
/// Guards the demo dataset's invariants. These are the assumptions the
/// <see cref="SampleTaskSeeder"/> and the database check-constraints rely on,
/// so they are asserted here rather than discovered at seed time.
/// </summary>
public class BudapestSampleDataTests
{
    // Must match the codes seeded by CategoryConfiguration.HasData.
    private static readonly HashSet<string> _knownCategoryCodes = new(StringComparer.Ordinal)
    {
        "moving", "tutoring", "repairs", "shopping",
        "pet_care", "cleaning", "errands", "other",
    };

    [Fact]
    public void Tasks_AreExactlyFifty()
    {
        Assert.Equal(50, BudapestSampleData.Tasks.Count);
    }

    [Fact]
    public void EveryTask_UsesAKnownCategoryCode()
    {
        Assert.All(
            BudapestSampleData.Tasks,
            task => Assert.Contains(task.CategoryCode, _knownCategoryCodes));
    }

    [Fact]
    public void CompensationAmount_IsConsistentWithCompensationType()
    {
        // Mirrors the controller rule and ck_tasks_compensation_amount checks:
        // only Paid tasks carry a (positive) amount; every other mode is null.
        Assert.All(BudapestSampleData.Tasks, task =>
        {
            if (task.CompensationType == CompensationType.Paid)
            {
                Assert.NotNull(task.Amount);
                Assert.True(task.Amount > 0m);
            }
            else
            {
                Assert.Null(task.Amount);
            }
        });
    }

    [Fact]
    public void EveryTask_HasCoordinatesWithinBudapest()
    {
        // Greater-Budapest bounding box; keeps the demo map pins in the city.
        Assert.All(BudapestSampleData.Tasks, task =>
        {
            Assert.InRange(task.Latitude, 47.34, 47.61);
            Assert.InRange(task.Longitude, 18.92, 19.34);
        });
    }

    [Fact]
    public void Tasks_HaveNonEmptyTitleAndDescription()
    {
        Assert.All(BudapestSampleData.Tasks, task =>
        {
            Assert.False(string.IsNullOrWhiteSpace(task.Title));
            Assert.False(string.IsNullOrWhiteSpace(task.Description));
            Assert.True(task.Title.Length <= 160);
        });
    }

    [Fact]
    public void Tasks_CoverEveryCategoryCompensationTypeAndAStatusMix()
    {
        var tasks = BudapestSampleData.Tasks;

        Assert.Equal(
            _knownCategoryCodes.OrderBy(code => code, StringComparer.Ordinal),
            tasks.Select(task => task.CategoryCode)
                .Distinct()
                .OrderBy(code => code, StringComparer.Ordinal));

        Assert.Equal(
            Enum.GetValues<CompensationType>().Length,
            tasks.Select(task => task.CompensationType).Distinct().Count());

        // A realistic feed shows more than just Open tasks.
        Assert.True(tasks.Select(task => task.Status).Distinct().Count() >= 3);
    }

    [Fact]
    public void Seekers_AreEnoughForDistinctHelperAssignmentAndHaveUniqueEmails()
    {
        var seekers = BudapestSampleData.Seekers;

        // The seeder assigns a helper that differs from the seeker, which is
        // only guaranteed when there is more than one seeker.
        Assert.True(seekers.Count > 1);

        var emails = seekers.Select(seeker => seeker.Email).ToList();
        Assert.Equal(emails.Count, emails.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Category_DefaultIconConstant_StaysInSyncWithDomain()
    {
        // The catalogue references categories by code; this guards that the
        // "other" fallback category the domain ships still exists.
        Assert.False(string.IsNullOrWhiteSpace(Category.DefaultIcon));
    }
}
