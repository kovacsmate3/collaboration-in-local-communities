using JetBrains.Annotations;

namespace Backend.Features.Profiles;

[PublicAPI]
public sealed record ProfileReviewResponse(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    Guid TargetUserId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);
