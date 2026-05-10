using Backend.Features.Admin.Analytics;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class AdminAnalyticsControllerTests
{
    [Fact]
    public async Task GetKpiAsync_ReturnsZeroedKpi_WhenReadModelIsEmpty()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = new AdminAnalyticsController(db);

        var result = await controller.GetKpiAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<KpiCurrentResponse>(ok.Value);
        Assert.Equal(0, response.RegisteredUsers);
        Assert.Equal(0, response.ActiveUsers7d);
        Assert.Equal(0, response.TasksPosted7d);
        Assert.Equal(0, response.CompletedTasks7d);
        Assert.Equal(0, response.CompletionRate7d);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
