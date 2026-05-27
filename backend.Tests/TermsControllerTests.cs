using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Terms;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace backend.Tests;

public sealed class TermsControllerTests
{
    [Fact]
    public async Task GetCurrentAsync_IgnoresFutureAndInactiveVersions()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
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

        await db.SaveChangesAsync(cancellationToken);

        var current = await db.TermsVersions
            .AsNoTracking()
            .GetCurrentAsync(now, cancellationToken);

        Assert.NotNull(current);
        Assert.Equal("1.1", current.Version);
    }

    [Fact]
    public async Task AcceptTermsAsync_RejectsExistingNonCurrentVersion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
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
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.AcceptTermsAsync(
            new AcceptTermsRequest { TermsVersionId = previousTerms.Id },
            cancellationToken);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Only the current active terms version can be accepted.", badRequest.Value);
        Assert.Empty(db.UserTermsAcceptances);
    }

    [Fact]
    public async Task AcceptTermsAsync_AcceptsCurrentVersion()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
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
        await db.SaveChangesAsync(cancellationToken);

        var userId = Guid.NewGuid();
        var controller = CreateController(db, userId);

        var result = await controller.AcceptTermsAsync(
            new AcceptTermsRequest { TermsVersionId = currentTerms.Id },
            cancellationToken);

        Assert.IsType<OkResult>(result);

        var acceptance = await db.UserTermsAcceptances.SingleAsync(cancellationToken);
        Assert.NotEqual(Guid.Empty, acceptance.Id);
        Assert.Equal(userId, acceptance.UserId);
        Assert.Equal(currentTerms.Id, acceptance.TermsVersionId);
    }

    [Fact]
    public async Task AcceptTermsAsync_RejectsMissingTermsVersionId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var controller = CreateController(db, Guid.NewGuid());

        var result = await controller.AcceptTermsAsync(
            new AcceptTermsRequest { TermsVersionId = null },
            cancellationToken);

        var validationProblem = Assert.IsAssignableFrom<ObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(validationProblem.Value);
        var error = Assert.Single(problemDetails.Errors[nameof(AcceptTermsRequest.TermsVersionId)]);
        Assert.Equal("Terms version ID is required.", error);
        Assert.Empty(db.UserTermsAcceptances);
    }

    [Fact]
    public async Task AcceptTermsAsync_IsIdempotentForExistingAcceptance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
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
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, Guid.NewGuid());
        var request = new AcceptTermsRequest { TermsVersionId = currentTerms.Id };

        var firstResult = await controller.AcceptTermsAsync(request, cancellationToken);
        var secondResult = await controller.AcceptTermsAsync(request, cancellationToken);

        Assert.IsType<OkResult>(firstResult);
        Assert.IsType<OkResult>(secondResult);
        Assert.Equal(1, await db.UserTermsAcceptances.CountAsync(cancellationToken));
    }

    [Fact]
    public void IsDuplicateUserTermsAcceptance_MatchesUniqueConstraint()
    {
        var postgresException = new PostgresException(
            "duplicate key value violates unique constraint",
            "ERROR",
            "ERROR",
            PostgresErrorCodes.UniqueViolation,
            detail: null,
            hint: null,
            position: 0,
            internalPosition: 0,
            internalQuery: null,
            where: null,
            schemaName: null,
            tableName: "user_terms_acceptances",
            columnName: null,
            dataTypeName: null,
            constraintName: "ux_user_terms_acceptances_user_terms",
            file: null,
            line: null,
            routine: null);
        var exception = new DbUpdateException("Duplicate acceptance.", postgresException);

        Assert.True(PostgresExceptionHelpers.IsDuplicateUserTermsAcceptance(exception));
    }

    [Fact]
    public async Task GetAcceptanceAsync_PatchBump_RemainsAccepted()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var accepted010 = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "0.1.0",
            MajorVersion = 0,
            MinorVersion = 1,
            PatchVersion = 0,
            Title = "Original",
            IsActive = false,
            EffectiveFrom = now.AddDays(-10),
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-10)
        };
        var active011 = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "0.1.1",
            MajorVersion = 0,
            MinorVersion = 1,
            PatchVersion = 1,
            Title = "Patch bump",
            IsActive = true,
            EffectiveFrom = now.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };

        db.TermsVersions.AddRange(accepted010, active011);
        db.UserTermsAcceptances.Add(new UserTermsAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TermsVersionId = accepted010.Id,
            AcceptedAt = now.AddDays(-5),
            IpAddress = "127.0.0.1"
        });
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, userId);
        var result = await controller.GetAcceptanceAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TermsAcceptanceResponse>(ok.Value);

        // Same major.minor (0.1) — patch bump must not require re-acceptance.
        Assert.True(response.HasAccepted);
    }

    [Fact]
    public async Task GetAcceptanceAsync_MinorBump_RequiresReAcceptance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        var accepted010 = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "0.1.0",
            MajorVersion = 0,
            MinorVersion = 1,
            PatchVersion = 0,
            Title = "Original",
            IsActive = false,
            EffectiveFrom = now.AddDays(-10),
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-10)
        };
        var active020 = new TermsVersion
        {
            Id = Guid.NewGuid(),
            Version = "0.2.0",
            MajorVersion = 0,
            MinorVersion = 2,
            PatchVersion = 0,
            Title = "Minor bump",
            IsActive = true,
            EffectiveFrom = now.AddDays(-1),
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now.AddDays(-1)
        };

        db.TermsVersions.AddRange(accepted010, active020);
        db.UserTermsAcceptances.Add(new UserTermsAcceptance
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TermsVersionId = accepted010.Id,
            AcceptedAt = now.AddDays(-5),
            IpAddress = "127.0.0.1"
        });
        await db.SaveChangesAsync(cancellationToken);

        var controller = CreateController(db, userId);
        var result = await controller.GetAcceptanceAsync(cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TermsAcceptanceResponse>(ok.Value);

        // Different minor version (0.1 → 0.2) — must require re-acceptance.
        Assert.False(response.HasAccepted);
    }

    [Fact]
    public void GetActiveTermsAsync_AllowsAnonymousAccess()
    {
        var method = typeof(TermsController).GetMethod(nameof(TermsController.GetActiveTermsAsync));

        Assert.NotNull(method);
        Assert.True(method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true));
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
        var controller = new TermsController(db, new StubClientIpAccessor())
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

    private sealed class StubClientIpAccessor : IClientIpAccessor
    {
        public string GetClientIp() => "127.0.0.1";
    }
}
