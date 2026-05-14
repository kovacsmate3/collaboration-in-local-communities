namespace Backend.Domain.Entities;

public sealed class Category
{
    public const string DefaultIcon = "MoreHorizontalCircle01Icon";

    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = DefaultIcon;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CommunityTask> Tasks { get; } = new List<CommunityTask>();
}
