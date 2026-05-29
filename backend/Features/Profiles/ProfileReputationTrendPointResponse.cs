namespace Backend.Features.Profiles;

public sealed record ProfileReputationTrendPointResponse(
    DateOnly Date,
    int Score,
    decimal AverageRating,
    int ReviewCount,
    int CompletedTaskCount);
