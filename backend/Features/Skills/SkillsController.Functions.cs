using System.Security.Claims;
using System.Text;
using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Features.Skills;

public sealed partial class SkillsController
{
    private static string GenerateCode(string name)
    {
        var builder = new StringBuilder();
        var previousUnderscore = false;

        foreach (var ch in name.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousUnderscore = false;
            }
            else if (!previousUnderscore && builder.Length > 0)
            {
                builder.Append('_');
                previousUnderscore = true;
            }
        }

        return builder.ToString().TrimEnd('_');
    }

    private async Task<UserProfile?> GetCurrentProfileAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            return null;
        }

        return await db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }
}
