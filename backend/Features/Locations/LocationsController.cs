using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Features.Locations;

[ApiController]
[Route("api/locations")]
[AllowAnonymous]
public sealed class LocationsController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ControllerBase
{
    private const string NominatimClientName = "Nominatim";

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

        var parameters = new Dictionary<string, string?>
        {
            ["q"] = query,
            ["format"] = "jsonv2",
            ["addressdetails"] = "1",
            ["limit"] = "5"
        };
        AddConfiguredEmail(parameters);

        using var response = await httpClientFactory
            .CreateClient(NominatimClientName)
            .GetAsync(BuildUri("search", parameters), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Problem(
                title: "Address lookup is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var results = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<NominatimSearchResult>>(cancellationToken);

        return Ok(new LocationSearchResponse(
            results?
                .Select(ToSuggestion)
                .OfType<LocationSuggestionResponse>()
                .ToList()
            ?? []));
    }

    [HttpGet("reverse")]
    public async Task<IActionResult> ReverseAsync(
        [FromQuery] double lat,
        [FromQuery] double lon,
        CancellationToken cancellationToken)
    {
        if (!IsValidLatitude(lat) || !IsValidLongitude(lon))
        {
            ModelState.AddModelError("Location", "Valid latitude and longitude are required.");
            return ValidationProblem(ModelState);
        }

        var parameters = new Dictionary<string, string?>
        {
            ["lat"] = lat.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["lon"] = lon.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["format"] = "jsonv2",
            ["zoom"] = "18"
        };
        AddConfiguredEmail(parameters);

        using var response = await httpClientFactory
            .CreateClient(NominatimClientName)
            .GetAsync(BuildUri("reverse", parameters), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return Problem(
                title: "Reverse geocoding is unavailable.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var result = await response.Content
            .ReadFromJsonAsync<NominatimReverseResult>(cancellationToken);
        var location = ToSuggestion(result, lat, lon);

        return Ok(new LocationReverseResponse(location));
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

        var id = result?.PlaceId?.ToString(CultureInfo.InvariantCulture)
            ?? $"{fallbackLatitude},{fallbackLongitude}";
        var displayName = string.IsNullOrWhiteSpace(result?.DisplayName)
            ? $"{fallbackLatitude}, {fallbackLongitude}"
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
