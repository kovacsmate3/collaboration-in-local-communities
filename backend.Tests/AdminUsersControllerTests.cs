using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Features.Admin.Users;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace backend.Tests;

public sealed class AdminUsersControllerTests
{
    // ── List tests (no UserManager needed) ───────────────────────────────────

    [Fact]
    public async Task ListAsync_ReturnsPaginatedResults()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Users.AddRange(
            new ApplicationUser { Id = userId1, UserName = "alice@test.com", Email = "alice@test.com" },
            new ApplicationUser { Id = userId2, UserName = "bob@test.com", Email = "bob@test.com" });
        db.Profiles.AddRange(
            new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId1,
                DisplayName = "Alice",
                IsProfileCompleted = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId2,
                DisplayName = "Bob",
                IsProfileCompleted = false,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            });
        await db.SaveChangesAsync(ct);

        var controller = CreateListController(db);

        var result = await controller.ListAsync(page: 1, pageSize: 20, ct: ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<AdminUserPagedResponse>(ok.Value);
        Assert.Equal(2, paged.TotalCount);
        Assert.Equal(2, paged.Items.Count);
        Assert.Equal(1, paged.TotalPages);
    }

    // Search tests require PostgreSQL: EF.Functions.ILike is a Npgsql-specific translation
    // that the InMemory provider cannot evaluate. Run these as integration tests against a
    // real Postgres instance (same convention as SkillsControllersTests, which also skips
    // the ILike prefix-search path in unit tests).

    [Fact(Skip = "Requires PostgreSQL — ILike is not supported by the InMemory provider")]
    public async Task ListAsync_WithSearch_IsCaseInsensitiveAndFilters()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        db.Users.AddRange(
            new ApplicationUser { Id = userId1, UserName = "charlie@test.com", Email = "charlie@test.com" },
            new ApplicationUser { Id = userId2, UserName = "dave@test.com", Email = "dave@test.com" });
        db.Profiles.AddRange(
            new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId1,
                DisplayName = "Charlie Brown",
                IsProfileCompleted = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId2,
                DisplayName = "Dave Smith",
                IsProfileCompleted = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        await db.SaveChangesAsync(ct);

        var controller = CreateListController(db);

        // Upper-case term must still match lower-case email and title-case display name.
        var result = await controller.ListAsync(page: 1, pageSize: 20, search: "CHARLIE", ct: ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<AdminUserPagedResponse>(ok.Value);
        Assert.Equal(1, paged.TotalCount);
        Assert.Equal("charlie@test.com", paged.Items[0].Email);
    }

    [Fact(Skip = "Requires PostgreSQL — ILike is not supported by the InMemory provider")]
    public async Task ListAsync_SearchByEmail_IsCaseInsensitive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        var userId = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = userId, UserName = "findme@test.com", Email = "findme@test.com" });
        await db.SaveChangesAsync(ct);

        var controller = CreateListController(db);

        // Upper-case term must match lower-case email address.
        var result = await controller.ListAsync(page: 1, pageSize: 20, search: "FINDME", ct: ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var paged = Assert.IsType<AdminUserPagedResponse>(ok.Value);
        Assert.Equal(1, paged.TotalCount);
    }

    // ── Make-admin / revoke-admin tests (UserManager needed) ─────────────────

    [Fact]
    public async Task MakeAdminAsync_ReturnsNotFound_ForUnknownUser()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var controller = CreateRoleController(scope, actorId: Guid.NewGuid());

        var result = await controller.MakeAdminAsync(Guid.NewGuid(), ct);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MakeAdminAsync_IsIdempotent_WhenUserAlreadyAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await EnsureRolesAsync(roleManager);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin-already@test.com", Email = "admin-already@test.com" };
        Assert.True((await userManager.CreateAsync(user, "P@ssword123!")).Succeeded);
        Assert.True((await userManager.AddToRoleAsync(user, ApplicationRoleNames.Admin)).Succeeded);

        var controller = CreateRoleController(scope, actorId: Guid.NewGuid());

        var result = await controller.MakeAdminAsync(user.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminUserResponse>(ok.Value);
        Assert.Contains("Admin", response.Roles);
    }

    [Fact]
    public async Task MakeAdminAsync_PromotesUser_WhenNotAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await EnsureRolesAsync(roleManager);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "promote@test.com", Email = "promote@test.com" };
        Assert.True((await userManager.CreateAsync(user, "P@ssword123!")).Succeeded);

        var controller = CreateRoleController(scope, actorId: Guid.NewGuid());

        var result = await controller.MakeAdminAsync(user.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminUserResponse>(ok.Value);
        Assert.Contains("Admin", response.Roles);
    }

    [Fact]
    public async Task RevokeAdminAsync_ReturnsBadRequest_WhenDemotingSelf()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var actorId = Guid.NewGuid();
        var controller = CreateRoleController(scope, actorId: actorId);

        var result = await controller.RevokeAdminAsync(actorId, ct);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RevokeAdminAsync_ReturnsNotFound_ForUnknownUser()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var controller = CreateRoleController(scope, actorId: Guid.NewGuid());

        var result = await controller.RevokeAdminAsync(Guid.NewGuid(), ct);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RevokeAdminAsync_IsIdempotent_WhenUserNotAdmin()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await EnsureRolesAsync(roleManager);

        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "notadmin@test.com", Email = "notadmin@test.com" };
        Assert.True((await userManager.CreateAsync(user, "P@ssword123!")).Succeeded);

        var controller = CreateRoleController(scope, actorId: Guid.NewGuid());

        var result = await controller.RevokeAdminAsync(user.Id, ct);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AdminUserResponse>(ok.Value);
        Assert.DoesNotContain("Admin", response.Roles);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddApplicationIdentity();
        return services.BuildServiceProvider();
    }

    private static AdminUsersController CreateListController(AppDbContext db, Guid? actorId = null)
    {
        var actor = actorId ?? Guid.NewGuid();

        // The list endpoint never calls UserManager, so we build a minimal DI
        // container with a fresh in-memory DB just to resolve the dependency.
        var services = new ServiceCollection();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o =>
            o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddApplicationIdentity();
        using var sp = services.BuildServiceProvider();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        var controller = new AdminUsersController(db, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, actor.ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    ], "TestAuth"))
                }
            }
        };

        return controller;
    }

    private static AdminUsersController CreateRoleController(IServiceScope scope, Guid actorId)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        return new AdminUsersController(db, userManager)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
                        new Claim(ClaimTypes.Role, "Admin")
                    ], "TestAuth"))
                }
            }
        };
    }

    private static async Task EnsureRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync(ApplicationRoleNames.Admin))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = ApplicationRoleNames.Admin });
        }

        if (!await roleManager.RoleExistsAsync(ApplicationRoleNames.User))
        {
            await roleManager.CreateAsync(new ApplicationRole { Name = ApplicationRoleNames.User });
        }
    }

}
