namespace Backend.Domain.Entities;

public sealed class ProfileSkill
{
    public Guid ProfileId { get; set; }
    public Guid SkillId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserProfile Profile { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
