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
    DateTimeOffset? PublishedAt,
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
    DateTimeOffset? PublishedAt,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int AcceptanceCount);

[PublicAPI]
public sealed record CreateTermsVersionRequest(
    [Required] string Version,
    [Required] string Title,
    string? Content,
    string? ContentUrl,
    [Required] DateTimeOffset? EffectiveFrom);

[PublicAPI]
public sealed record UpdateTermsVersionRequest(
    [Required] string Version,
    [Required] string Title,
    string? Content,
    string? ContentUrl,
    [Required] DateTimeOffset? EffectiveFrom);
