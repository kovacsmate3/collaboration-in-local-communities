using System.Security.Claims;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Profiles;

[ApiController]
[Route("api/profiles")]
[Authorize]
public sealed class ProfilesController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Get a public profile by ID, respecting privacy settings.
    /// </summary>
    /// <param name="id">The profile ID to retrieve.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with the public profile. Fields hidden by privacy settings are omitted from the response.
    /// 404 Not Found if the profile does not exist.
    /// </returns>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublicProfileAsync(Guid id, CancellationToken cancellationToken)
    {
        var profile = await db.Profiles
            .AsNoTracking()
            .Include(p => p.PrivacySettings)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        var privacy = profile.PrivacySettings;
        var response = new PublicProfileResponse
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            Workplace = privacy?.ShowWorkplace == true ? profile.Workplace : null,
            Position = privacy?.ShowPosition == true ? profile.Position : null,
            Availability = privacy?.ShowAvailability == true ? profile.Availability : null,
            PhotoUrl = profile.PhotoUrl,
            LocationText = privacy?.ShowLocation == true ? profile.LocationText : null,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            CompletedTaskCount = profile.CompletedTaskCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Get the current authenticated user's privacy settings.
    /// </summary>
    /// <returns>
    /// 200 OK with privacy settings (showWorkplace, showPosition, showLocation, showAvailability).
    /// 404 Not Found if the user has no profile or privacy settings.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpGet("me/privacy")]
    public async Task<IActionResult> GetPrivacySettingsAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var privacySettings = await db.ProfilePrivacySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Profile.UserId == userIdGuid, cancellationToken);

        if (privacySettings is null)
        {
            return NotFound();
        }

        var response = new ProfilePrivacySettingsResponse
        {
            ShowWorkplace = privacySettings.ShowWorkplace,
            ShowPosition = privacySettings.ShowPosition,
            ShowLocation = privacySettings.ShowLocation,
            ShowAvailability = privacySettings.ShowAvailability
        };

        return Ok(response);
    }

    /// <summary>
    /// Get the current authenticated user's own profile with all fields (private + public).
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with the user's complete profile including all fields.
    /// 404 Not Found if the user has no profile or no privacy settings
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetOwnProfileAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var profile = await db.Profiles
            .AsNoTracking()
            .Include(p => p.PrivacySettings)
            .Include(p => p.ProfileSkills)
            .FirstOrDefaultAsync(p => p.UserId == userIdGuid, cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        var response = new OwnProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            Workplace = profile.Workplace,
            Position = profile.Position,
            Availability = profile.Availability,
            PhotoUrl = profile.PhotoUrl,
            LocationText = profile.LocationText,
            IsProfileCompleted = profile.IsProfileCompleted,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            CompletedTaskCount = profile.CompletedTaskCount,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            SkillIds = profile.ProfileSkills.Select(ps => ps.SkillId).ToList(),
            PrivacySettings = new ProfilePrivacySettingsResponse
            {
                ShowWorkplace = profile.PrivacySettings?.ShowWorkplace ?? true,
                ShowPosition = profile.PrivacySettings?.ShowPosition ?? true,
                ShowLocation = profile.PrivacySettings?.ShowLocation ?? true,
                ShowAvailability = profile.PrivacySettings?.ShowAvailability ?? true
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Update the current authenticated user's profile.
    /// </summary>
    /// <param name="request">Profile fields to update.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with the updated profile.
    /// 404 Not Found if the user has no profile.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateOwnProfileAsync(
        UpdateOwnProfileRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var profile = await db.Profiles
            .Include(p => p.PrivacySettings)
            .Include(p => p.ProfileSkills)
            .FirstOrDefaultAsync(p => p.UserId == userIdGuid, cancellationToken);

        if (profile is null)
        {
            return NotFound();
        }

        profile.DisplayName = request.DisplayName;
        profile.Bio = request.Bio;
        profile.Workplace = request.Workplace;
        profile.Position = request.Position;
        profile.Availability = request.Availability;
        profile.PhotoUrl = request.PhotoUrl;
        profile.LocationText = request.LocationText;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var response = new OwnProfileResponse
        {
            Id = profile.Id,
            UserId = profile.UserId,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            Workplace = profile.Workplace,
            Position = profile.Position,
            Availability = profile.Availability,
            PhotoUrl = profile.PhotoUrl,
            LocationText = profile.LocationText,
            IsProfileCompleted = profile.IsProfileCompleted,
            AverageRating = profile.AverageRating,
            ReviewCount = profile.ReviewCount,
            CompletedTaskCount = profile.CompletedTaskCount,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            SkillIds = profile.ProfileSkills.Select(ps => ps.SkillId).ToList(),
            PrivacySettings = new ProfilePrivacySettingsResponse
            {
                ShowWorkplace = profile.PrivacySettings?.ShowWorkplace ?? true,
                ShowPosition = profile.PrivacySettings?.ShowPosition ?? true,
                ShowLocation = profile.PrivacySettings?.ShowLocation ?? true,
                ShowAvailability = profile.PrivacySettings?.ShowAvailability ?? true
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Update the current authenticated user's privacy settings (full-replace).
    /// </summary>
    /// <param name="request">All privacy flags must be provided (full-replace, no partial updates).</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>
    /// 200 OK with updated privacy settings.
    /// 404 Not Found if the user has no profile or privacy settings.
    /// 401 Unauthorized if not authenticated.
    /// </returns>
    [HttpPut("me/privacy")]
    public async Task<IActionResult> UpdatePrivacySettingsAsync(
        UpdateProfilePrivacySettingsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userIdGuid))
        {
            return Unauthorized();
        }

        var privacySettings = await db.ProfilePrivacySettings
            .Include(p => p.Profile)
            .FirstOrDefaultAsync(p => p.Profile.UserId == userIdGuid, cancellationToken);

        if (privacySettings is null)
        {
            return NotFound();
        }

        privacySettings.ShowWorkplace = request.ShowWorkplace!.Value;
        privacySettings.ShowPosition = request.ShowPosition!.Value;
        privacySettings.ShowLocation = request.ShowLocation!.Value;
        privacySettings.ShowAvailability = request.ShowAvailability!.Value;
        privacySettings.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var response = new ProfilePrivacySettingsResponse
        {
            ShowWorkplace = privacySettings.ShowWorkplace,
            ShowPosition = privacySettings.ShowPosition,
            ShowLocation = privacySettings.ShowLocation,
            ShowAvailability = privacySettings.ShowAvailability
        };

        return Ok(response);
    }
}
