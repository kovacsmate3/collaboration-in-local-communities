using JetBrains.Annotations;

namespace Backend.Features.Admin.AuditLog;

[PublicAPI]
public sealed record AuditLogEntryResponse(
    Guid Id,
    Guid? ActorUserId,
    string? ActorEmail,
    string? ActorDisplayName,
    string EventType,
    string? EntityType,
    Guid? EntityId,
    string? Payload,
    DateTimeOffset CreatedAt);

[PublicAPI]
public sealed record AuditLogPagedResponse(
    IReadOnlyList<AuditLogEntryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
