using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Terms;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace backend.Tests;

public sealed class TermsControllerTests
{
    [Fact]
    public async Task GetCurrentAsync_IgnoresFutureAndInactiveVersions()
    {
        await using var db = CreateDbContext();
        var now = new DateTimeOffset(2026, 05, 06, 12, 00, 00, TimeSpan.Zero);

        db.TermsVersions.AddRange(
            new TermsVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.0",
                Title = "Initial",
                IsActive = true,
                EffectiveFrom = now.AddDays(-30),
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-30)
            },
            new TermsVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.1",
                Title = "Current",
                IsActive = true,
                EffectiveFrom = now.AddDays(-1),
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            },
            new TermsVersion
            {
                Id = Guid.NewGuid(),
                Version = "1.2",
                Title = "Future",
                IsActive = true,
                EffectiveFrom = now.AddDays(7),
                CreatedAt = now,
                UpdatedAt = now
            },
            new TermsVersion
            {
                Id = Guid.NewGuid(),
                Version = "0.9",
                Title = "Inactive",
                IsActive = false,
                EffectiveFrom = now.AddDays(-90),
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-90)
            });

        await db.SaveChangesAsync();

        var current = await db.TermsVersions
            .AsNoTracking()
            .GetCurrentAsync(now, CancellationToken.None);

        Assert.NotNull(current);
        Assert.Equal("1.1", current.Version);
    }

    [Fact]
    public async Task AcceptTermsAsync_RejectsExistingNonCurrentVersion()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var previousTerms = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "1.0",
            Title = "Previous",
            IsActive = false,
            EffectiveFrom = now.AddDays(-30),
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-30)
        };
        var currentTerms = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "1.1",
            Title = "Current",
            IsActive = true,
            EffectiveFrom = now.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };

        db.TermsVersions.AddRange(previousTerms, currentTerms);
        await db.SaveChangesAsync();

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.AcceptTermsAsync(
            new AcceptTermsRequest { TermsVersionId = previousTerms.Id },
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only the current active terms version can be accepted.", badRequest.Value);
        Assert.Empty(db.UserTermsAcceptances);
    }

    [Fact]
    public async Task AcceptTermsAsync_AcceptsCurrentVersion()
    {
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var currentTerms = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "1.1",
            Title = "Current",
            IsActive = true,
            EffectiveFrom = now.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };

        db.TermsVersions.Add(currentTerms);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        var controller = CreateController(db, userId);

        var result = await controller.AcceptTermsAsync(
            new AcceptTermsRequest { TermsVersionId = currentTerms.Id },
            CancellationToken.None);

        Assert.IsType<OkResult>(result);

        var acceptance = await db.UserTermsAcceptances.SingleAsync();
        Assert.Equal(userId, acceptance.UserId);
        Assert.Equal(currentTerms.Id, acceptance.TermsVersionId);
    }

    [Fact]
    public void GetActiveTermsAsync_AllowsAnonymousAccess()
    {
        var method = typeof(TermsController).GetMethod(nameof(TermsController.GetActiveTermsAsync));

        Assert.NotNull(method);
        Assert.Contains(
            method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true),
            _ => true);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static TermsController CreateController(AppDbContext db, Guid userId)
    {
        var controller = new TermsController(db)
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
            }
        };

        return controller;
    }
}
