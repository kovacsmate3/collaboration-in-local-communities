using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using DomainTaskStatus = Backend.Domain.Enums.TaskStatus;

namespace Backend.Features.Tasks;

public sealed partial class TasksController
{
    private static bool TryParseCompensationType(string value, out CompensationType result)
    {
        result = default;
        return !int.TryParse(value, out _) && Enum.TryParse(value, ignoreCase: true, out result);
    }

    private static bool TryParseTaskStatus(string value, out DomainTaskStatus result)
    {
        result = default;
        return !int.TryParse(value, out _) && Enum.TryParse(value, ignoreCase: true, out result);
    }

    private static Point? BuildLocation(double? latitude, double? longitude)
    {
        if (latitude is null || longitude is null)
        {
            return null;
        }

        return new Point(longitude.Value, latitude.Value) { SRID = 4326 };
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
