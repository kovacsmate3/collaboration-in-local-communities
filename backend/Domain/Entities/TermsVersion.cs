namespace Backend.Domain.Entities;

public sealed class TermsVersion
{
    public Guid Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int PatchVersion { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? ContentUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<UserTermsAcceptance> Acceptances { get; } = new List<UserTermsAcceptance>();
}
