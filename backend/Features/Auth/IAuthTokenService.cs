using Backend.Infrastructure.Identity;

namespace Backend.Features.Auth;

public interface IAuthTokenService
{
    Task<AuthTokenResult> CreateTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken);

    string HashRefreshToken(string refreshToken);
}
