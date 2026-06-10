using JetBrains.Annotations;

namespace Backend.Features.Profiles;

/// <summary>
/// A single immutable points ledger entry. <c>EntryType</c> is the string form
/// of <see cref="Backend.Domain.Enums.PointEntryType"/>; <c>TaskId</c> is set for
/// task-linked entries (e.g. completion rewards) and null otherwise.
/// </summary>
[PublicAPI]
public sealed record PointsLedgerEntryResponse(
    Guid Id,
    int Amount,
    string EntryType,
    string? Description,
    Guid? TaskId,
    DateTimeOffset CreatedAt);

/// <summary>
/// Paginated envelope for a profile's points ledger history, ordered newest first.
/// </summary>
[PublicAPI]
public sealed record PointsLedgerPagedResponse(
    IReadOnlyList<PointsLedgerEntryResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);
