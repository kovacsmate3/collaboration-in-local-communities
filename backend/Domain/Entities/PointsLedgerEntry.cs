using Backend.Domain.Enums;

namespace Backend.Domain.Entities;

public sealed class PointsLedgerEntry
{
    public Guid Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid? TaskId { get; set; }
    public int Amount { get; set; }
    public PointEntryType EntryType { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public UserProfile Profile { get; set; } = null!;
    public CommunityTask? Task { get; set; }
}
