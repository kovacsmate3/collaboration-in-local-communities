using System.Security.Claims;
using System.Text.RegularExpressions;
using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    private static bool TryBuildProximityFilter(
        double? latitude,
        double? longitude,
        double? radiusMeters,
        ModelStateDictionary modelState,
        out Point? origin)
    {
        origin = null;

        if (latitude.HasValue != longitude.HasValue || latitude.HasValue != radiusMeters.HasValue)
        {
            modelState.AddModelError("Proximity", "Latitude, Longitude, and RadiusMeters must be provided together.");
            return false;
        }

        if (!latitude.HasValue || !longitude.HasValue || !radiusMeters.HasValue)
        {
            return true;
        }

        if (!double.IsFinite(latitude.Value) || latitude.Value is < -90 or > 90)
        {
            modelState.AddModelError(nameof(latitude), "Latitude must be between -90 and 90.");
            return false;
        }

        if (!double.IsFinite(longitude.Value) || longitude.Value is < -180 or > 180)
        {
            modelState.AddModelError(nameof(longitude), "Longitude must be between -180 and 180.");
            return false;
        }

        if (!double.IsFinite(radiusMeters.Value) || radiusMeters.Value <= 0)
        {
            modelState.AddModelError(nameof(radiusMeters), "RadiusMeters must be greater than 0.");
            return false;
        }

        origin = BuildLocation(latitude, longitude);
        return true;
    }

    private static string SanitizeDescription(string html) =>
        new HtmlSanitizer().Sanitize(html);

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

    private bool ValidateDescriptionPlainText(string sanitizedHtml, string fieldName)
    {
        var plain = Regex.Replace(sanitizedHtml, "<[^>]+>", string.Empty).Trim();
        if (plain.Length < 10)
        {
            ModelState.AddModelError(fieldName, "Description must contain at least 10 characters of text.");
            return false;
        }

        return true;
    }
}
