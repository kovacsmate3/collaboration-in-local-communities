using System.ComponentModel.DataAnnotations;
using Backend.Domain.Entities;
using JetBrains.Annotations;

namespace Backend.Features.Tasks.Applications;

[PublicAPI]
public sealed record ApplyToTaskRequest(
    [StringLength(1000)]
    string? Message);

[PublicAPI]
public sealed record PatchApplicationRequest(
    [Required]
    [StringLength(16)]
    string Action);

[PublicAPI]
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

[PublicAPI]
public sealed record MyTaskApplicationResponse(
    Guid Id,
    Guid TaskId,
    Guid HelperProfileId,
    string HelperDisplayName,
    string? Message,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TaskResponse Task)
{
    public static MyTaskApplicationResponse FromApplication(TaskApplication application)
    {
        return new MyTaskApplicationResponse(
            application.Id,
            application.TaskId,
            application.HelperProfileId,
            application.HelperProfile.DisplayName,
            application.Message,
            application.Status.ToString(),
            application.CreatedAt,
            application.UpdatedAt,
            TaskResponse.FromTask(application.Task));
    }
}
