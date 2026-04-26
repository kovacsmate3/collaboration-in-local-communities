namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class ProfileReputation
{
    public Guid ProfileId { get; set; }
    public decimal AverageRating { get; set; }
    public long ReviewCount { get; set; }
    public long CompletedTaskCount { get; set; }
}
