using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Features.Auth;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace backend.Tests;

public sealed class AuthTokenServiceTests
{
    [Fact]
    public async Task CreateTokenPairAsync_PreservesAdminRoleForRefreshLoadedUser()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await roleManager.CreateAsync(new ApplicationRole
        {
            Name = ApplicationRoleNames.Admin
        });
        await roleManager.CreateAsync(new ApplicationRole
        {
            Name = ApplicationRoleNames.User
        });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin@example.test",
            Email = "admin@example.test"
        };

        Assert.True((await userManager.CreateAsync(user, "P@ssword123")).Succeeded);
        Assert.True((await userManager.AddToRolesAsync(
            user,
            [ApplicationRoleNames.Admin, ApplicationRoleNames.User])).Succeeded);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = "HASH",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        var refreshLoadedUser = await db.RefreshTokens
            .Include(token => token.User)
            .Where(token => token.UserId == user.Id)
            .Select(token => token.User)
            .SingleAsync(cancellationToken);

        var tokens = await tokenService.CreateTokenPairAsync(refreshLoadedUser, cancellationToken);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);

        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == ClaimTypes.Role && claim.Value == ApplicationRoleNames.Admin);
    }

    [Fact]
    public async Task CreateTokenPairAsync_EmitsExpectedClaimsAndExpiry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "claims@example.test",
            Email = "claims@example.test"
        };

        var before = DateTimeOffset.UtcNow;
        var pair = await tokenService.CreateTokenPairAsync(user, cancellationToken);
        var after = DateTimeOffset.UtcNow;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(pair.AccessToken);
        Assert.Equal(options.Issuer, jwt.Issuer);
        Assert.Contains(options.Audience, jwt.Audiences);
        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.NotEmpty(jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value);

        // Expiry should land within the configured window of when the call started.
        var minExpected = before.AddMinutes(options.AccessTokenMinutes);
        var maxExpected = after.AddMinutes(options.AccessTokenMinutes);
        Assert.InRange(pair.AccessTokenExpiresAt, minExpected, maxExpected);

        // Refresh token is a non-empty Base64 string with the documented byte length.
        var refreshBytes = Convert.FromBase64String(pair.RefreshToken);
        Assert.Equal(64, refreshBytes.Length);
    }

    [Fact]
    public async Task CreateTokenPairAsync_GeneratesUniqueRefreshTokensAndJtis()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "unique@example.test",
            Email = "unique@example.test"
        };

        var first = await tokenService.CreateTokenPairAsync(user, cancellationToken);
        var second = await tokenService.CreateTokenPairAsync(user, cancellationToken);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        var firstJti = new JwtSecurityTokenHandler()
            .ReadJwtToken(first.AccessToken)
            .Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var secondJti = new JwtSecurityTokenHandler()
            .ReadJwtToken(second.AccessToken)
            .Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        Assert.NotEqual(firstJti, secondJti);
    }

    [Fact]
    public async Task HashRefreshToken_IsDeterministicForSameInput()
    {
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();

        // A real refresh token is base64; just feed the service a known base64 string.
        var token = Convert.ToBase64String("the quick brown fox jumps over the lazy dog"u8.ToArray());

        var first = tokenService.HashRefreshToken(token);
        var second = tokenService.HashRefreshToken(token);

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length); // SHA-256 hex
        await Task.CompletedTask;
    }

    [Fact]
    public async Task HashRefreshToken_ThrowsFormatException_WhenNotBase64()
    {
        await using var services = CreateServices();
        using var scope = services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IAuthTokenService>();

        Assert.Throws<FormatException>(() => tokenService.HashRefreshToken("not-base64-***"));
        await Task.CompletedTask;
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddApplicationIdentity();
        services.AddSingleton(new SymmetricSecurityKey(
            "refresh-token-role-test-signing-key-32"u8.ToArray()));
        services.AddSingleton(Options.Create(new AuthOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            AccessTokenMinutes = 15,
            RefreshTokenDays = 30
        }));
        services.AddScoped<IAuthTokenService, AuthTokenService>();

        return services.BuildServiceProvider();
    }
}
