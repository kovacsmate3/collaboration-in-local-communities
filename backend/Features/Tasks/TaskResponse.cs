using Backend.Domain.Entities;

namespace Backend.Features.Tasks;

public sealed record TaskResponse(
    Guid Id,
    string PublicCode,
    Guid SeekerProfileId,
    string SeekerDisplayName,
    Guid? AcceptedHelperProfileId,
    string? AcceptedHelperDisplayName,
    Guid CategoryId,
    string CategoryName,
    string Title,
    string Description,
    string? LocationText,
    double? Latitude,
    double? Longitude,
    string CompensationType,
    decimal? CompensationAmount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TaskResponse FromTask(CommunityTask task)
    {
        return new TaskResponse(
            task.Id,
            task.PublicCode ?? string.Empty,
            task.SeekerProfileId,
            task.SeekerProfile.DisplayName,
            task.AcceptedHelperProfileId,
            task.AcceptedHelperProfile?.DisplayName,
            task.CategoryId,
            task.Category.Name,
            task.Title,
            task.Description,
            task.LocationText,
            task.Location?.Y,
            task.Location?.X,
            task.CompensationType.ToString(),
            task.CompensationAmount,
            task.Status.ToString(),
            task.CreatedAt,
            task.UpdatedAt);
    }
}
