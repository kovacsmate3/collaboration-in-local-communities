using JetBrains.Annotations;

namespace Backend.Features.Profiles;

/// <summary>
/// Public-facing review payload. The internal <c>TaskId</c> linkage is
/// deliberately omitted: reviews are tied to a completed task for integrity
/// and duplicate prevention, but that task should not be exposed by default
/// on public review surfaces (see issue #115).
/// </summary>
[PublicAPI]
public sealed record ProfileReviewResponse(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    Guid TargetUserId,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAt);

/// <summary>
/// Paginated envelope used by the profile reviews list (issue #115).
/// </summary>
[PublicAPI]
public sealed record ProfileReviewPagedResponse(
    IReadOnlyList<ProfileReviewResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
