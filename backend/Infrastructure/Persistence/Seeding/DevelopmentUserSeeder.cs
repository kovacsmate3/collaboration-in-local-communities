using Backend.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the local-development regular-user account. Idempotent — safe on
/// every startup. The account specification (email, password, profile
/// fields) is taken from <see cref="DevSeederOptions.User"/> so it can be
/// overridden per-environment via configuration.
/// </summary>
/// <remarks>
/// Single responsibility: only the regular user. The admin lives in
/// <see cref="DevelopmentAdminSeeder"/>. Both share the heavy lifting
/// through <see cref="UserSeedingHelper"/> so this class stays a few lines
/// long.
/// </remarks>
public sealed class DevelopmentUserSeeder(
    IOptions<DevSeederOptions> options,
    UserSeedingHelper userSeedingHelper,
    ILogger<DevelopmentUserSeeder> logger) : IDataSeeder
{
    /// <summary>
    /// Gets the relative ordering hint. Runs after the admin seeder; see notes
    /// on that class.
    /// </summary>
    public int Order => 110;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var user = options.Value.User;

        await userSeedingHelper.EnsureUserAsync(
            user,
            [ApplicationRoleNames.User],
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Development user account ready ({Email}).",
                user.Email);
        }
    }
}
