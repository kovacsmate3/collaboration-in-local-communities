namespace Backend.Features.Profiles;

/// <summary>
/// Public profile response that respects privacy settings.
/// Fields that are hidden by the user will be null.
/// </summary>
public sealed record PublicProfileResponse
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Bio { get; init; }
    public string? Workplace { get; init; }
    public string? Position { get; init; }
    public string? Availability { get; init; }
    public string? PhotoUrl { get; init; }
    public string? LocationText { get; init; }
    public required decimal AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required int CompletedTaskCount { get; init; }
}
