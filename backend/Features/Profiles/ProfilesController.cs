using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

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
            Latitude = profile.Location?.Y,
            Longitude = profile.Location?.X,
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

        if (!TryBuildLocation(request.Latitude, request.Longitude, out var location))
        {
            return ValidationProblem(ModelState);
        }

        profile.DisplayName = request.DisplayName;
        profile.Bio = request.Bio;
        profile.Workplace = request.Workplace;
        profile.Position = request.Position;
        profile.Availability = request.Availability;
        profile.PhotoUrl = request.PhotoUrl;
        profile.LocationText = request.LocationText;
        profile.Location = location;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.SkillIds is not null)
        {
            var requested = request.SkillIds.ToHashSet();
            var currentIds = profile.ProfileSkills.Select(ps => ps.SkillId).ToHashSet();

            foreach (var ps in profile.ProfileSkills.Where(ps => !requested.Contains(ps.SkillId)).ToList())
            {
                profile.ProfileSkills.Remove(ps);
            }

            var toAdd = requested.Except(currentIds).ToList();
            if (toAdd.Count > 0)
            {
                var validIds = await db.Skills
                    .Where(s => toAdd.Contains(s.Id) && s.IsActive && s.Status == SkillStatus.Approved)
                    .Select(s => s.Id)
                    .ToListAsync(cancellationToken);

                foreach (var skillId in validIds)
                {
                    profile.ProfileSkills.Add(new ProfileSkill { SkillId = skillId });
                }
            }
        }

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
            Latitude = profile.Location?.Y,
            Longitude = profile.Location?.X,
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

    private bool TryBuildLocation(double? latitude, double? longitude, out Point? location)
    {
        location = null;

        if (latitude.HasValue != longitude.HasValue)
        {
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Latitude), "Both Latitude and Longitude must be provided together.");
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Longitude), "Both Latitude and Longitude must be provided together.");
            return false;
        }

        if (!latitude.HasValue || !longitude.HasValue)
        {
            return true;
        }

        if (!double.IsFinite(latitude.Value) || latitude.Value is < -90 or > 90)
        {
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Latitude), "Latitude must be between -90 and 90.");
            return false;
        }

        if (!double.IsFinite(longitude.Value) || longitude.Value is < -180 or > 180)
        {
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Longitude), "Longitude must be between -180 and 180.");
            return false;
        }

        location = new Point(longitude.Value, latitude.Value) { SRID = 4326 };
        return true;
    }
}
