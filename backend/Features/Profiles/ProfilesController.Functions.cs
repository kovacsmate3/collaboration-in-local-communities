using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Backend.Features.Profiles;

public sealed partial class ProfilesController
{
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private const int DefaultReviewsPageSize = 10;
    private const int MaxReviewsPageSize = 50;

    private static readonly Dictionary<string, string> _allowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    private bool TryBuildLocation(double? latitude, double? longitude, out Point? location)
    {
        location = null;

        if (latitude.HasValue != longitude.HasValue)
        {
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Latitude), "Both Latitude and Longitude must be provided together.");
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Longitude), "Both Latitude and Longitude must be provided together.");
            return false;
        }

        if (!latitude.HasValue || !longitude.HasValue)
        {
            return true;
        }

        if (!double.IsFinite(latitude.Value) || latitude.Value is < -90 or > 90)
        {
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Latitude), "Latitude must be between -90 and 90.");
            return false;
        }

        if (!double.IsFinite(longitude.Value) || longitude.Value is < -180 or > 180)
        {
            ModelState.AddModelError(nameof(UpdateOwnProfileRequest.Longitude), "Longitude must be between -180 and 180.");
            return false;
        }

        location = new Point(longitude.Value, latitude.Value) { SRID = 4326 };
        return true;
    }

    private Task<bool> ProfileExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Profiles
            .AsNoTracking()
            .AnyAsync(profile => profile.Id == id, cancellationToken);
    }
}
