using System.Security.Claims;
using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Reviews;

public sealed partial class ReviewsController
{
    /// <summary>
    /// Resolves the authenticated user's profile by reading the NameIdentifier
    /// claim. Returns null if the principal does not have a parseable user id
    /// or the user has no profile.
    /// </summary>
    private async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            return null;
        }

        return await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
