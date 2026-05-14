using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Features.Locations;

[ApiController]
[Route("api/locations")]
[AllowAnonymous]
public sealed partial class LocationsController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IMemoryCache cache) : ControllerBase
{
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
}
