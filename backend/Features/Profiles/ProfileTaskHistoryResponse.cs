using JetBrains.Annotations;

namespace Backend.Features.Profiles;

[PublicAPI]
public sealed record ProfileTaskHistoryResponse(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryCode,
    string CategoryName,
    string CategoryIcon,
    string Status,
    DateTimeOffset CreatedAt);
