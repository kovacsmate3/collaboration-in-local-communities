using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public sealed class ActivityEvent
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ProfileId { get; set; }
    public ActivityEventType EventType { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? Metadata { get; set; }

    public UserProfile? Profile { get; set; }
}
