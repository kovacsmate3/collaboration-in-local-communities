using System.ComponentModel.DataAnnotations;

using Backend.Domain.Entities;

namespace Backend.Features.Skills;

public sealed record SkillResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Status)
{
    public static SkillResponse FromEntity(Skill skill) =>
        new(skill.Id, skill.Code, skill.Name, skill.Description, skill.Status.ToString());
}

public sealed record CreateSkillRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }
}
