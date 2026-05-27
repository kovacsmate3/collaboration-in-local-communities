using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Backend.Features.Terms;

[PublicAPI]
public sealed record ActiveTermsResponse(
    Guid Id,
    string Version,
    string Title,
    string? Content,
    string? ContentUrl,
    DateTimeOffset EffectiveFrom);

[PublicAPI]
public sealed record AcceptTermsRequest
{
    [Required]
    public Guid? TermsVersionId { get; init; }
}

[PublicAPI]
public sealed record TermsAcceptanceResponse(
    bool HasAccepted,
    DateTimeOffset? AcceptedAt);
