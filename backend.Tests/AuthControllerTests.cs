using Backend.Features.Auth;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace backend.Tests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task RegisterAsync_Returns400_WhenNoActiveTermsExist()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var db = CreateDbContext();

        // No TermsVersions seeded — the "no active terms" path should trigger.
        var controller = CreateController(db);

        var request = new RegisterRequest(
            Email: "user@test.com",
            Password: "P@ssword123!",
            DisplayName: "Test User",
            Workplace: null,
            Position: null,
            LocationText: null,
            Latitude: null,
            Longitude: null,
            Bio: null,
            AcceptTerms: true,
            SkillIds: null);

        var result = await controller.RegisterAsync(request, ct);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(badRequest.Value);
        Assert.Equal("Registration unavailable", problem.Title);

        // FakeUserStore never writes to db.Users, so the user was not persisted.
        Assert.Empty(db.Users);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static AuthController CreateController(AppDbContext db)
    {
        var userManager = new FakeUserManager();

        // Services used only after a successful registration; passing null! is safe
        // because the "no active terms" path returns 400 before reaching them.
        var controller = new AuthController(
            db,
            userManager,
            tokenService: null!,
            clientIpAccessor: null!,
            emailSender: null!,
            emailOptions: null!,
            logger: NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private sealed class FakeUserManager()
        : UserManager<ApplicationUser>(
            new FakeUserStore(),
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            userValidators: [],
            passwordValidators: [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<ApplicationUser>>.Instance)
    {
        public override Task<IdentityResult> AddToRoleAsync(ApplicationUser user, string role) =>
            Task.FromResult(IdentityResult.Success);
    }

    private sealed class FakeUserStore : IUserStore<ApplicationUser>
    {
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) =>
            Task.FromResult(user.Id.ToString());

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) =>
            Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct) =>
            Task.FromResult<ApplicationUser?>(null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
            Task.FromResult<ApplicationUser?>(null);

        public void Dispose() { }
    }
}
