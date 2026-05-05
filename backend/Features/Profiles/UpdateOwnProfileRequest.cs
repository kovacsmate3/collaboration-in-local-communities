namespace Backend.Features.Profiles;

/// <summary>
/// Request DTO for updating the authenticated user's own profile.
/// </summary>
public sealed record UpdateOwnProfileRequest
{
    public required string DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? Workplace { get; init; }
    public string? Position { get; init; }
    public string? Availability { get; init; }
    public string? PhotoUrl { get; init; }
    public string? LocationText { get; init; }
}
