namespace Backend.Domain.Entities;

public sealed class Category
{
    public const string DefaultIcon = "MoreHorizontalCircle01Icon";

    public const decimal DefaultPointsWeight = 1.0m;

    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = DefaultIcon;
    public int SortOrder { get; set; }

    // Multiplier applied to the base completion reward so admins can make harder
    // categories (e.g. moving) worth more points than lighter ones. Server-owned;
    // never set from client input.
    public decimal PointsWeight { get; set; } = DefaultPointsWeight;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<CommunityTask> Tasks { get; } = new List<CommunityTask>();
}
