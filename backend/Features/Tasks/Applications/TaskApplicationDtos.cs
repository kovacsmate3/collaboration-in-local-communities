using Backend.Domain.Entities;

namespace Backend.Features.Tasks.Applications;

public sealed record ApplyToTaskRequest(string? Message);

public sealed record PatchApplicationRequest(string Action);

public sealed record TaskApplicationResponse(
    Guid Id,
    Guid TaskId,
    Guid HelperProfileId,
    string HelperDisplayName,
    string? Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static TaskApplicationResponse FromApplication(TaskApplication application, string helperDisplayName)
    {
        return new TaskApplicationResponse(
            application.Id,
            application.TaskId,
            application.HelperProfileId,
            helperDisplayName,
            application.Message,
            application.Status.ToString(),
            application.CreatedAt,
            application.UpdatedAt);
    }
}
