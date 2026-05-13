namespace Backend.Features.Locations;

public sealed record LocationSuggestionResponse(
    string Id,
    string LocationText,
    double Latitude,
    double Longitude);

public sealed record LocationSearchResponse(
    IReadOnlyList<LocationSuggestionResponse> Suggestions);

public sealed record LocationReverseResponse(
    LocationSuggestionResponse Location);
