using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Backend.Features.Admin.Terms;

[PublicAPI]
public sealed record AdminTermsVersionListItem(
    Guid Id,
    string Version,
    int MajorVersion,
    int MinorVersion,
    int PatchVersion,
    string Title,
    bool IsActive,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset CreatedAt,
    int AcceptanceCount);

[PublicAPI]
public sealed record AdminTermsVersionDetail(
    Guid Id,
    string Version,
    int MajorVersion,
    int MinorVersion,
    int PatchVersion,
    string Title,
    string? Content,
    string? ContentUrl,
    bool IsActive,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int AcceptanceCount);

[PublicAPI]
public sealed record CreateTermsVersionRequest
{
    [Required]
    public string Version { get; init; } = string.Empty;

    [Required]
    public string Title { get; init; } = string.Empty;

    public string? Content { get; init; }

    public string? ContentUrl { get; init; }

    [Required]
    public DateTimeOffset? EffectiveFrom { get; init; }
}

[PublicAPI]
public sealed record UpdateTermsVersionRequest
{
    [Required]
    public string Version { get; init; } = string.Empty;

    [Required]
    public string Title { get; init; } = string.Empty;

    public string? Content { get; init; }

    public string? ContentUrl { get; init; }

    [Required]
    public DateTimeOffset? EffectiveFrom { get; init; }
}
