using System.Security.Claims;
using Backend.Common;
using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Tasks.Applications;

public sealed partial class TaskApplicationsController
{
    private static bool TryParseAction(string value, out bool isAccept)
    {
        var normalized = StringUtilities.Normalize(value);
        if (normalized is null)
        {
            isAccept = false;
            return false;
        }

        if (string.Equals(normalized, "accept", StringComparison.OrdinalIgnoreCase))
        {
            isAccept = true;
            return true;
        }

        if (string.Equals(normalized, "reject", StringComparison.OrdinalIgnoreCase))
        {
            isAccept = false;
            return true;
        }

        isAccept = false;
        return false;
    }

    private async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return null;
        }

        return await db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId.Value, cancellationToken);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
