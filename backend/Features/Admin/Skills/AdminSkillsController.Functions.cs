using System.Security.Claims;
using System.Text.Json;
using Backend.Domain.Entities;

namespace Backend.Features.Admin.Skills;

public sealed partial class AdminSkillsController
{
    private Guid? GetActorUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private void AddAuditEvent(Guid? actorUserId, string eventType, string? entityType, Guid? entityId, object? payload)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = actorUserId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Payload = payload is null ? null : JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }
}
