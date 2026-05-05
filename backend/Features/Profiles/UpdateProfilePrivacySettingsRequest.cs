namespace Backend.Features.Profiles;

/// <summary>
/// Request DTO for updating privacy settings. All fields are required (full-replace, no partial updates).
/// </summary>
public sealed record UpdateProfilePrivacySettingsRequest
{
    public required bool ShowWorkplace { get; init; }
    public required bool ShowPosition { get; init; }
    public required bool ShowLocation { get; init; }
    public required bool ShowAvailability { get; init; }
}
