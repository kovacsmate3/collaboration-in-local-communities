using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddApplicationIdentity();
        services.AddSingleton(new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("refresh-token-role-test-signing-key-32")));
        services.AddSingleton<IOptions<AuthOptions>>(Options.Create(new AuthOptions
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
