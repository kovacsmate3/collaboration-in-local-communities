using Backend.Domain.Enums;
using NetTopologySuite.Geometries;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Domain.Entities;

public sealed class CommunityTask
{
    public Guid Id { get; set; }
    public string? PublicCode { get; set; }
    public Guid SeekerProfileId { get; set; }
    public Guid? AcceptedHelperProfileId { get; set; }
    public Guid CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Point? Location { get; set; }
    public string? LocationText { get; set; }
    public CompensationType CompensationType { get; set; }
    public decimal? CompensationAmount { get; set; }
    public DomainTaskStatus Status { get; set; } = DomainTaskStatus.Open;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    public UserProfile SeekerProfile { get; set; } = null!;
    public UserProfile? AcceptedHelperProfile { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<TaskApplication> Applications { get; } = new List<TaskApplication>();
    public ICollection<TaskCompletionConfirmation> CompletionConfirmations { get; } = new List<TaskCompletionConfirmation>();
    public TaskConversation? Conversation { get; set; }
    public ICollection<TaskStatusHistoryEntry> StatusHistory { get; } = new List<TaskStatusHistoryEntry>();
    public ICollection<Review> Reviews { get; } = new List<Review>();
    public ICollection<PointsLedgerEntry> PointsLedgerEntries { get; } = new List<PointsLedgerEntry>();
}
