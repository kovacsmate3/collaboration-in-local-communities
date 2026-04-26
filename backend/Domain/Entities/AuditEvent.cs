namespace Backend.Domain.Entities;

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
