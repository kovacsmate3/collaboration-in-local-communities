using JetBrains.Annotations;

namespace Backend.Features.Auth;

[PublicAPI]
public sealed record AuthTokenResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
