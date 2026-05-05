using System.Text.Json.Serialization;

namespace Backend.Features.Profiles;

/// <summary>
/// Public profile response that respects privacy settings.
/// Fields that are hidden by privacy settings are omitted from the JSON response.
/// </summary>
public sealed record PublicProfileResponse
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Bio { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Workplace { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Position { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Availability { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PhotoUrl { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LocationText { get; init; }
    public required decimal AverageRating { get; init; }
    public required int ReviewCount { get; init; }
    public required int CompletedTaskCount { get; init; }
}
