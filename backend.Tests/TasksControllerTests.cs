using Backend.Domain.Entities;
using Backend.Features.Tasks;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Xunit;

namespace backend.Tests;

public sealed class TasksControllerTests
{
    [Fact]
    public async Task ListAsync_RejectsPartialProximityFilter()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateInMemoryDbContext();
        var controller = new TasksController(db);

        var result = await controller.ListAsync(
            status: null,
            categoryId: null,
            latitude: 47.4979,
            longitude: null,
            radiusMeters: 1000,
            cancellationToken);

        var badRequest = Assert.IsType<ObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(badRequest.Value);

        Assert.Contains("Proximity", problem.Errors.Keys);
    }

    [Fact]
    public void ProximityQuery_TranslatesToPostgisDWithinAndDistanceOrdering()
    {
        using var db = CreateNpgsqlDbContext();
        var origin = new Point(19.0402, 47.4979) { SRID = 4326 };

        var sql = db.Tasks
            .Where(task => task.Location != null && task.Location.IsWithinDistance(origin, 1000))
            .OrderBy(task => task.Location == null ? double.MaxValue : task.Location.Distance(origin))
            .ToQueryString();

        Assert.Contains("ST_DWithin", sql);
        Assert.Contains("ST_Distance", sql);
        Assert.Contains("ORDER BY", sql);
    }

    private static AppDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AppDbContext CreateNpgsqlDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=proximity_sql_test", npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }
}
