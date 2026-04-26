using NetTopologySuite.Geometries;

namespace Backend.Domain.Entities;

public sealed class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Workplace { get; set; }
    public string? Position { get; set; }
    public string? Availability { get; set; }
    public string? PhotoUrl { get; set; }
    public Point? Location { get; set; }
    public string? LocationText { get; set; }
    public bool IsProfileCompleted { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ProfilePrivacySettings? PrivacySettings { get; set; }
    public ICollection<ProfileSkill> ProfileSkills { get; } = new List<ProfileSkill>();
    public ICollection<CommunityTask> PostedTasks { get; } = new List<CommunityTask>();
    public ICollection<CommunityTask> AcceptedTasks { get; } = new List<CommunityTask>();
    public ICollection<TaskApplication> TaskApplications { get; } = new List<TaskApplication>();
    public ICollection<TaskCompletionConfirmation> CompletionConfirmations { get; } = new List<TaskCompletionConfirmation>();
    public ICollection<Review> ReviewsWritten { get; } = new List<Review>();
    public ICollection<Review> ReviewsReceived { get; } = new List<Review>();
    public ICollection<PointsLedgerEntry> PointsLedgerEntries { get; } = new List<PointsLedgerEntry>();
}
