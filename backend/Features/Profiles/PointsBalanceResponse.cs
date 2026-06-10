using JetBrains.Annotations;

namespace Backend.Features.Profiles;

/// <summary>
/// Current points balance for a profile: the live sum of every ledger entry's
/// amount (earned rewards, manual adjustments, redemptions). Returned as a
/// signed total so future debits (e.g. redemptions) reduce it correctly.
/// </summary>
[PublicAPI]
public sealed record PointsBalanceResponse(
    Guid ProfileId,
    long Balance);
