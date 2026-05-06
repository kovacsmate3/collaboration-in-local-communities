using System.ComponentModel.DataAnnotations;

namespace Backend.Features.Terms;

public sealed record ActiveTermsResponse(
    Guid Id,
    string Version,
    string Title,
    string? ContentUrl,
    DateTimeOffset EffectiveFrom);

public sealed record AcceptTermsRequest
{
    [Required]
    public Guid? TermsVersionId { get; init; }
}

public sealed record TermsAcceptanceResponse(
    bool HasAccepted,
    DateTimeOffset? AcceptedAt);
