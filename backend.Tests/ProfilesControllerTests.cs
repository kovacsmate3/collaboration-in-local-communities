using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Profiles;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class ProfilesControllerTests
{
    [Theory]
    [InlineData(47.0, null)]
    [InlineData(null, 19.0)]
    public async Task UpdateOwnProfileAsync_ReturnsBadRequest_WhenOnlyOneCoordinateProvided(
        double? latitude, double? longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("Latitude"));
        Assert.True(problem.Errors.ContainsKey("Longitude"));
    }

    [Theory]
    [InlineData(91.0, 0.0)]
    [InlineData(-91.0, 0.0)]
    [InlineData(double.NaN, 0.0)]
    [InlineData(double.PositiveInfinity, 0.0)]
    public async Task UpdateOwnProfileAsync_ReturnsBadRequest_WhenLatitudeIsInvalid(
        double latitude, double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("Latitude"));
    }

    [Theory]
    [InlineData(0.0, 181.0)]
    [InlineData(0.0, -181.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(0.0, double.NegativeInfinity)]
    public async Task UpdateOwnProfileAsync_ReturnsBadRequest_WhenLongitudeIsInvalid(
        double latitude, double longitude)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, _) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ValidationProblemDetails>(bad.Value);
        Assert.True(problem.Errors.ContainsKey("Longitude"));
    }

    [Fact]
    public async Task UpdateOwnProfileAsync_PersistsLocationWithSrid4326_WhenValidCoordinatesProvided()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var (userId, profileId) = await SeedProfileAsync(db, cancellationToken);
        var controller = CreateProfilesController(db, userId);

        const double latitude = 47.4979;
        const double longitude = 19.0402;

        var result = await controller.UpdateOwnProfileAsync(
            new UpdateOwnProfileRequest { DisplayName = "Test", Latitude = latitude, Longitude = longitude },
            cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OwnProfileResponse>(ok.Value);
        Assert.Equal(latitude, response.Latitude);
        Assert.Equal(longitude, response.Longitude);

        var storedProfile = await db.Profiles.FindAsync([profileId], cancellationToken);
        Assert.NotNull(storedProfile?.Location);
        Assert.Equal(4326, storedProfile.Location.SRID);
        Assert.Equal(longitude, storedProfile.Location.X);
        Assert.Equal(latitude, storedProfile.Location.Y);
    }

    private static async Task<(Guid userId, Guid profileId)> SeedProfileAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = "Existing User",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return (userId, profile.Id);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static ProfilesController CreateProfilesController(AppDbContext db, Guid userId)
    {
        var controller = new ProfilesController(db)
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
            },
            ProblemDetailsFactory = new FakeProblemDetailsFactory()
        };

        return controller;
    }

    private sealed class FakeProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new() { Status = statusCode, Title = title, Detail = detail };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new(modelStateDictionary) { Status = statusCode ?? 400 };
    }
}
