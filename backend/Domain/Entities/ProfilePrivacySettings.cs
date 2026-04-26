namespace Backend.Domain.Entities;

public sealed class ProfilePrivacySettings
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public bool ShowWorkplace { get; set; } = true;
    public bool ShowPosition { get; set; } = true;
    public bool ShowLocation { get; set; } = true;
    public bool ShowAvailability { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public UserProfile Profile { get; set; } = null!;
}
