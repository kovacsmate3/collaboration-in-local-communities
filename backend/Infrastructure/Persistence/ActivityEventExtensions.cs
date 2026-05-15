using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Domain.Enums;

namespace Backend.Infrastructure.Persistence;

internal static class ActivityEventExtensions
{
    public static void AddActivityEvent(
        this AppDbContext db,
        Guid? userId,
        Guid? profileId,
        ActivityEventType eventType,
        string? entityType,
        Guid? entityId,
        object? metadata = null)
    {
        db.ActivityEvents.Add(new ActivityEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProfileId = profileId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTimeOffset.UtcNow
        });
    }
}
