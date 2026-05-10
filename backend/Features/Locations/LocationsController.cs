using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Features.Locations;

[ApiController]
[Route("api/locations")]
[AllowAnonymous]
public sealed class LocationsController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache) : ControllerBase
{
    private const string NominatimClientName = "Nominatim";
    private const string CoordinateFormat = "0.######";
    private static readonly SemaphoreSlim _nominatimThrottle = new(1, 1);
    private static DateTimeOffset _nextNominatimRequestAt = DateTimeOffset.MinValue;

    [HttpGet("search")]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var query = q?.Trim();
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            return Ok(new LocationSearchResponse([]));
        }

        var cacheKey = $"locations:search:{query.ToUpperInvariant()}";
        if (cache.TryGetValue<LocationSearchResponse>(cacheKey, out var cachedResponse))
        {
            return Ok(cachedResponse);
        }

        var parameters = new Dictionary<string, string?>
        {
            ["q"] = query,
            ["format"] = "jsonv2",
            ["addressdetails"] = "1",
            ["limit"] = "5"
        };
        AddConfiguredEmail(parameters);

        using var response = await SendNominatimRequestAsync(
            BuildUri("search", parameters),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Problem(
                title: "Address lookup is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var results = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<NominatimSearchResult>>(cancellationToken);

        var locationResponse = new LocationSearchResponse(
            results?
                .Select(ToSuggestion)
                .OfType<LocationSuggestionResponse>()
                .ToList()
            ?? []);
        cache.Set(cacheKey, locationResponse, TimeSpan.FromDays(1));

        return Ok(locationResponse);
    }

    [HttpGet("reverse")]
    public async Task<IActionResult> ReverseAsync(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken cancellationToken)
    {
        if (!IsValidLatitude(lat))
        {
            ModelState.AddModelError(nameof(lat), "Latitude must be between -90 and 90.");
        }

        if (!IsValidLongitude(lon))
        {
            ModelState.AddModelError(nameof(lon), "Longitude must be between -180 and 180.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var cacheKey = $"locations:reverse:{FormatCoordinate(lat)},{FormatCoordinate(lon)}";
        if (cache.TryGetValue<LocationReverseResponse>(cacheKey, out var cachedResponse))
        {
            return Ok(cachedResponse);
        }

        var parameters = new Dictionary<string, string?>
        {
            ["lat"] = FormatCoordinate(lat),
            ["lon"] = FormatCoordinate(lon),
            ["format"] = "jsonv2",
            ["zoom"] = "18"
        };
        AddConfiguredEmail(parameters);

        using var response = await SendNominatimRequestAsync(
            BuildUri("reverse", parameters),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Problem(
                title: "Reverse geocoding is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await response.Content
            .ReadFromJsonAsync<NominatimReverseResult>(cancellationToken);
        var location = ToSuggestion(result, lat, lon);
        var locationResponse = new LocationReverseResponse(location);
        cache.Set(cacheKey, locationResponse, TimeSpan.FromDays(1));

        return Ok(locationResponse);
    }

    private static string BuildUri(string path, IReadOnlyDictionary<string, string?> parameters)
    {
        var query = string.Join(
            "&",
            parameters
                .Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                .Select(parameter =>
                    $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value!)}"));

        return $"{path}?{query}";
    }

    private static LocationSuggestionResponse? ToSuggestion(NominatimSearchResult result)
    {
        if (!TryParseCoordinate(result.Lat, out var latitude)
            || !TryParseCoordinate(result.Lon, out var longitude)
            || string.IsNullOrWhiteSpace(result.DisplayName))
        {
            return null;
        }

        return new LocationSuggestionResponse(
            result.PlaceId.ToString(CultureInfo.InvariantCulture),
            result.DisplayName,
            latitude,
            longitude);
    }

    private static LocationSuggestionResponse ToSuggestion(
        NominatimReverseResult? result,
        double fallbackLatitude,
        double fallbackLongitude)
    {
        var latitude = TryParseCoordinate(result?.Lat, out var parsedLatitude)
            ? parsedLatitude
            : fallbackLatitude;
        var longitude = TryParseCoordinate(result?.Lon, out var parsedLongitude)
            ? parsedLongitude
            : fallbackLongitude;

        var fallbackCoordinates = $"{FormatCoordinate(fallbackLatitude)},{FormatCoordinate(fallbackLongitude)}";
        var fallbackDisplayName = $"{FormatCoordinate(fallbackLatitude)}, {FormatCoordinate(fallbackLongitude)}";
        var id = result?.PlaceId?.ToString(CultureInfo.InvariantCulture)
            ?? fallbackCoordinates;
        var displayName = string.IsNullOrWhiteSpace(result?.DisplayName)
            ? fallbackDisplayName
            : result.DisplayName;

        return new LocationSuggestionResponse(id, displayName, latitude, longitude);
    }

    private static bool TryParseCoordinate(string? value, out double coordinate)
    {
        return double.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out coordinate)
            && double.IsFinite(coordinate);
    }

    private static bool IsValidLatitude(double value)
    {
        return double.IsFinite(value) && value is >= -90 and <= 90;
    }

    private static bool IsValidLongitude(double value)
    {
        return double.IsFinite(value) && value is >= -180 and <= 180;
    }

    private static string FormatCoordinate(double value)
    {
        return value.ToString(CoordinateFormat, CultureInfo.InvariantCulture);
    }

    private async Task<HttpResponseMessage> SendNominatimRequestAsync(
        string requestUri,
        CancellationToken cancellationToken)
    {
        await _nominatimThrottle.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (_nextNominatimRequestAt > now)
            {
                await Task.Delay(_nextNominatimRequestAt - now, cancellationToken);
            }

            _nextNominatimRequestAt = DateTimeOffset.UtcNow.AddSeconds(1);

            return await httpClientFactory
                .CreateClient(NominatimClientName)
                .GetAsync(requestUri, cancellationToken);
        }
        finally
        {
            _nominatimThrottle.Release();
        }
    }

    private void AddConfiguredEmail(Dictionary<string, string?> parameters)
    {
        var email = configuration["Nominatim:Email"]?.Trim();
        if (!string.IsNullOrEmpty(email))
        {
            parameters["email"] = email;
        }
    }

    private sealed record NominatimSearchResult(
        [property: JsonPropertyName("place_id")] long PlaceId,
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon);

    private sealed record NominatimReverseResult(
        [property: JsonPropertyName("place_id")] long? PlaceId,
        [property: JsonPropertyName("display_name")] string? DisplayName,
        [property: JsonPropertyName("lat")] string? Lat,
        [property: JsonPropertyName("lon")] string? Lon);
}
