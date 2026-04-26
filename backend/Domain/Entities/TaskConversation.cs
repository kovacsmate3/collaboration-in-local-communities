namespace Backend.Domain.Entities;

public sealed class TaskConversation
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid SeekerProfileId { get; set; }
    public Guid HelperProfileId { get; set; }
    public string CosmosConversationId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public CommunityTask Task { get; set; } = null!;
    public UserProfile SeekerProfile { get; set; } = null!;
    public UserProfile HelperProfile { get; set; } = null!;
}
