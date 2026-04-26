using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public sealed class TaskApplication
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid HelperProfileId { get; set; }
    public string? Message { get; set; }
    public TaskApplicationStatus Status { get; set; } = TaskApplicationStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public CommunityTask Task { get; set; } = null!;
    public UserProfile HelperProfile { get; set; } = null!;
}
