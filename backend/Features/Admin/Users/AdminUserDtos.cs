using JetBrains.Annotations;

namespace Backend.Features.Admin.Users;

[PublicAPI]
public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    string? DisplayName,
    string? PhotoUrl,
    bool IsProfileCompleted,
    IReadOnlyList<string> Roles,
    DateTimeOffset? JoinedAt);

[PublicAPI]
public sealed record AdminUserPagedResponse(
    IReadOnlyList<AdminUserResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
