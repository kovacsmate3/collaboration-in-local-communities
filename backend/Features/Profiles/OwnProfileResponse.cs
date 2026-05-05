namespace Backend.Features.Profiles;

/// <summary>
/// Response DTO for the authenticated user's own profile.
/// Returns all fields (private and public) since the user is viewing their own data.
/// Skill details are accessed separately via the skills API.
/// </summary>
public sealed record OwnProfileResponse
{
    public required Guid Id { get; init; }
    public required Guid UserId { get; init; }
    public required string DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? Workplace { get; init; }
    public string? Position { get; init; }
    public string? Availability { get; init; }
    public string? PhotoUrl { get; init; }
    public string? LocationText { get; init; }
    public bool IsProfileCompleted { get; init; }
    public required decimal AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required int CompletedTaskCount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public ICollection<Guid> SkillIds { get; init; } = [];
    public required ProfilePrivacySettingsResponse PrivacySettings { get; init; }
}
