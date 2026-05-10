using Backend.Infrastructure.Identity;
using Xunit;

namespace backend.Tests;

public sealed class RefreshTokenPrunerTests
{
    [Fact]
    public void CreatePrunablePredicate_MatchesOnlyTokensExpiredOrRevokedAtLeastOneWeekAgo()
    {
        var now = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
        var cutoff = now.AddDays(-7);
        var isExpired = RefreshTokenPruner.CreateExpiredPredicate(cutoff).Compile();
        var isRevoked = RefreshTokenPruner.CreateRevokedPredicate(cutoff).Compile();

        Assert.True(isExpired(CreateRefreshToken(expiresAt: cutoff)));
        Assert.True(isRevoked(CreateRefreshToken(expiresAt: cutoff.AddDays(1), revokedAt: cutoff)));
        Assert.False(isExpired(CreateRefreshToken(expiresAt: cutoff.AddSeconds(1))));
        Assert.False(isRevoked(CreateRefreshToken(expiresAt: now.AddDays(1), revokedAt: cutoff.AddSeconds(1))));
        Assert.False(isExpired(CreateRefreshToken(expiresAt: now.AddDays(1))));
        Assert.False(isRevoked(CreateRefreshToken(expiresAt: now.AddDays(1))));
    }

    private static RefreshToken CreateRefreshToken(DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = Guid.NewGuid().ToString("N"),
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
            CreatedAt = expiresAt.AddDays(-1)
        };
    }
}
