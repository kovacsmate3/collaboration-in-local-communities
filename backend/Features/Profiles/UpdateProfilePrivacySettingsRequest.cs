using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Profiles;

/// <summary>
/// Request DTO for updating privacy settings. All fields are required (full-replace, no partial updates).
/// </summary>
public sealed record UpdateProfilePrivacySettingsRequest
{
    [Required]
    public bool? ShowWorkplace { get; init; }

    [Required]
    public bool? ShowPosition { get; init; }

    [Required]
    public bool? ShowLocation { get; init; }

    [Required]
    public bool? ShowAvailability { get; init; }
}
