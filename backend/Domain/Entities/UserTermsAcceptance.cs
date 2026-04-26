namespace Backend.Domain.Entities;

public sealed class UserTermsAcceptance
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TermsVersionId { get; set; }
    public DateTimeOffset AcceptedAt { get; set; }
    public string? IpAddress { get; set; }

    public TermsVersion TermsVersion { get; set; } = null!;
}
