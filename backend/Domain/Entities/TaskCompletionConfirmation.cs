namespace Backend.Domain.Entities;

public sealed class TaskCompletionConfirmation
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid ProfileId { get; set; }
    public DateTimeOffset ConfirmedAt { get; set; }

    public CommunityTask Task { get; set; } = null!;
    public UserProfile Profile { get; set; } = null!;
}
