using Backend.Features.Admin.Analytics;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class AdminAnalyticsControllerTests
{
    [Fact]
    public async Task GetKpiAsync_ReturnsNotFound_WhenReadModelIsEmpty()
    {
        // Note: KpiCurrent maps to a PostgreSQL view (kpi_current_v) that always returns
        // a single aggregate row in production. An empty result can only happen when the
        // view is unavailable or the database is in an unexpected state.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = new AdminAnalyticsController(db);

        var result = await controller.GetKpiAsync(cancellationToken);

        Assert.IsType<NotFoundResult>(result);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
