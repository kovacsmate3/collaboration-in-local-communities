using Backend.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the local-development admin account. Idempotent — safe on every
/// startup. The account specification (email, password, profile fields) is
/// taken from <see cref="DevSeederOptions.Admin"/> so it can be overridden
/// per-environment via configuration.
/// </summary>
/// <remarks>
/// Single responsibility: only the admin account. The regular user lives in
/// <see cref="DevelopmentUserSeeder"/>. Both share the heavy lifting through
/// <see cref="UserSeedingHelper"/> so this class stays a few lines long.
/// </remarks>
public sealed class DevelopmentAdminSeeder(
    IOptions<DevSeederOptions> options,
    UserSeedingHelper userSeedingHelper,
    ILogger<DevelopmentAdminSeeder> logger) : IDataSeeder
{
    /// <summary>
    /// Gets the relative ordering hint. Runs before
    /// <see cref="DevelopmentUserSeeder"/> purely for deterministic log output;
    /// there is no functional dependency.
    /// </summary>
    public int Order => 100;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var admin = options.Value.Admin;

        await userSeedingHelper.EnsureUserAsync(
            admin,
            [ApplicationRoleNames.Admin, ApplicationRoleNames.User],
            cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Development admin account ready ({Email}).",
                admin.Email);
        }
    }
}
