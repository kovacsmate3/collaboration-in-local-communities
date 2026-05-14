using Backend.Infrastructure.Identity;

namespace Backend.Features.Auth;

/// <summary>
/// Creates and hashes authentication tokens for application users.
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Creates an access token and refresh token pair for the specified user.
    /// </summary>
    /// <param name="user">The user to create tokens for.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The generated token pair.</returns>
    Task<AuthTokenResult> CreateTokenPairAsync(ApplicationUser user, CancellationToken cancellationToken);

    /// <summary>
    /// Hashes a refresh token before storing it.
    /// </summary>
    /// <param name="refreshToken">The refresh token to hash.</param>
    /// <returns>The hashed refresh token.</returns>
    string HashRefreshToken(string refreshToken);
}
