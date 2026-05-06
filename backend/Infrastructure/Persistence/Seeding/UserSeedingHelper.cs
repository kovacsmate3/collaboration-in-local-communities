using Backend.Domain.Entities;
using Backend.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Reusable "ensure user exists with roles + profile" primitive. Encapsulates
/// the multi-step idempotent dance of:
/// <list type="bullet">
///   <item>creating the <see cref="ApplicationUser"/> if missing,</item>
///   <item>confirming the email,</item>
///   <item>granting any roles that aren't already attached,</item>
///   <item>creating the <see cref="UserProfile"/> + <see cref="ProfilePrivacySettings"/> if missing,</item>
///   <item>completing an existing-but-stub profile if needed.</item>
/// </list>
/// All operations are idempotent and safe to run on every startup. Extracted
/// from the seeders so adding a new seeded account is one helper call rather
/// than ~80 lines of duplicated code (DRY).
/// </summary>
public sealed class UserSeedingHelper(
    UserManager<ApplicationUser> userManager,
    AppDbContext db)
{
    /// <summary>
    /// Ensure the supplied development account exists and has the supplied
    /// roles, creating the user, profile, and privacy settings as needed.
    /// </summary>
    public async Task EnsureUserAsync(
        DevSeedAccount account,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var user = await EnsureIdentityUserAsync(account, cancellationToken);
        await EnsureRolesAsync(user, roles);
        await EnsureProfileAsync(user, account, cancellationToken);
    }

    private async Task<ApplicationUser> EnsureIdentityUserAsync(
        DevSeedAccount account,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = account.Email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true
            };

            (await userManager.CreateAsync(user, account.Password))
                .ThrowIfFailed($"create development user {normalizedEmail}");

            return user;
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            (await userManager.UpdateAsync(user))
                .ThrowIfFailed($"confirm development user {normalizedEmail}");
        }

        // The cancellationToken parameter is here for symmetry with the
        // surrounding seed pipeline; UserManager doesn't accept one directly.
        cancellationToken.ThrowIfCancellationRequested();
        return user;
    }

    private async Task EnsureRolesAsync(ApplicationUser user, IReadOnlyCollection<string> roles)
    {
        foreach (var role in roles)
        {
            if (await userManager.IsInRoleAsync(user, role))
            {
                continue;
            }

            (await userManager.AddToRoleAsync(user, role))
                .ThrowIfFailed($"add {role} role to {user.Email}");
        }
    }

    private async Task EnsureProfileAsync(
        ApplicationUser user,
        DevSeedAccount account,
        CancellationToken cancellationToken)
    {
        var profile = await db.Profiles
            .Include(p => p.PrivacySettings)
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (profile is null)
        {
            db.Profiles.Add(new UserProfile
            {
                UserId = user.Id,
                DisplayName = account.DisplayName,
                Workplace = account.Workplace,
                Position = account.Position,
                LocationText = account.LocationText,
                Bio = account.Bio,
                IsProfileCompleted = true,
                CreatedAt = now,
                UpdatedAt = now,
                PrivacySettings = new ProfilePrivacySettings
                {
                    CreatedAt = now,
                    UpdatedAt = now
                }
            });

            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var changed = false;

        if (!profile.IsProfileCompleted)
        {
            profile.IsProfileCompleted = true;
            profile.UpdatedAt = now;
            changed = true;
        }

        if (profile.PrivacySettings is null)
        {
            profile.PrivacySettings = new ProfilePrivacySettings
            {
                CreatedAt = now,
                UpdatedAt = now
            };
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
