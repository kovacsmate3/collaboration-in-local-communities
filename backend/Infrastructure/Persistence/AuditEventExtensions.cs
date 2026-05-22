using System.Text.Json;
using Backend.Domain.Entities;

namespace Backend.Infrastructure.Persistence;

internal static class AuditEventExtensions
{
    public static void AddAuditEvent(
        this AppDbContext db,
        Guid? actorUserId,
        string eventType,
        string? entityType,
        Guid? entityId,
        object? payload)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = actorUserId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
