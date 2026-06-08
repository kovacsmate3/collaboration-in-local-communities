using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Persistence.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace backend.Tests;

public sealed class ProfileReputationRefreshTests
{
    [Fact]
    public void RefreshCommand_RefreshesMaterializedViewConcurrently()
    {
        // CONCURRENTLY keeps profile_reputation_v readable while it rebuilds and
        // depends on the unique index created in the
        // MaterializeProfileReputationView migration. The schema must stay fully
        // qualified so the command does not rely on search_path.
        Assert.Equal(
            "REFRESH MATERIALIZED VIEW CONCURRENTLY analytics.profile_reputation_v",
            ProfileReputationViewRefresher.RefreshCommand);
    }

    [Fact]
    public void Options_HaveExpectedDefaults()
    {
        var options = new ProfileReputationRefreshOptions();

        Assert.Equal("ProfileReputationRefresh", ProfileReputationRefreshOptions.SectionName);
        Assert.True(options.Enabled);
        Assert.True(options.RunOnStartup);
        Assert.Equal(15, options.IntervalMinutes);
    }

    [Fact]
    public void ProfileReputation_MapsToView_WithDenormalisedColumns()
    {
        // The materialized view created in MaterializeProfileReputationView must
        // expose exactly these columns for the ProfileReputation read model to
        // bind; this guards the C# model and the migration SQL from drifting.
        using var db = CreateDbContext();
        var entityType = db.Model.FindEntityType(typeof(ProfileReputation));
        Assert.NotNull(entityType);

        var storeObject = StoreObjectIdentifier.View("profile_reputation_v", "analytics");

        Assert.Equal(
            "profile_id",
            entityType.FindProperty(nameof(ProfileReputation.ProfileId))?.GetColumnName(storeObject));
        Assert.Equal(
            "average_rating",
            entityType.FindProperty(nameof(ProfileReputation.AverageRating))?.GetColumnName(storeObject));
        Assert.Equal(
            "review_count",
            entityType.FindProperty(nameof(ProfileReputation.ReviewCount))?.GetColumnName(storeObject));
        Assert.Equal(
            "completed_task_count",
            entityType.FindProperty(nameof(ProfileReputation.CompletedTaskCount))?.GetColumnName(storeObject));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
