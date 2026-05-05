namespace Backend.Features.Profiles;

/// <summary>
/// Response DTO for privacy settings, representing which profile fields are visible to others.
/// </summary>
public sealed record ProfilePrivacySettingsResponse
{
    public bool ShowWorkplace { get; init; }
    public bool ShowPosition { get; init; }
    public bool ShowLocation { get; init; }
    public bool ShowAvailability { get; init; }
}
