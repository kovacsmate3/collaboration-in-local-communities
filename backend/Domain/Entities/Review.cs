namespace Backend.Domain.Entities;

public sealed class Review
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ReviewerProfileId { get; set; }
    public Guid RevieweeProfileId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public CommunityTask Task { get; set; } = null!;
    public UserProfile ReviewerProfile { get; set; } = null!;
    public UserProfile RevieweeProfile { get; set; } = null!;
}
