using Backend.Domain.Entities;

namespace Backend.Features.Admin.Skills;

public sealed record AdminSkillResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive,
    string Status,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static AdminSkillResponse FromEntity(Skill skill) => new(
        skill.Id,
        skill.Code,
        skill.Name,
        skill.Description,
        skill.IsActive,
        skill.Status.ToString(),
        skill.ApprovedAt,
        skill.CreatedAt,
        skill.UpdatedAt);
}

public sealed record AdminSkillPagedResponse(
    IReadOnlyList<AdminSkillResponse> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record PatchSkillRequest
{
    public string Action { get; init; } = string.Empty;
}
