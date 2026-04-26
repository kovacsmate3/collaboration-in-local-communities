using Microsoft.AspNetCore.Identity;

namespace Backend.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<RefreshToken> RefreshTokens { get; } = new List<RefreshToken>();
}
