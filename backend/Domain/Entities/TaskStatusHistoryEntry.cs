using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Domain.Entities;

public sealed class TaskStatusHistoryEntry
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public DomainTaskStatus? OldStatus { get; set; }
    public DomainTaskStatus NewStatus { get; set; }
    public Guid? ChangedByProfileId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset ChangedAt { get; set; }

    public CommunityTask Task { get; set; } = null!;
    public UserProfile? ChangedByProfile { get; set; }
}
