namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class ProfilePointsBalance
{
    public Guid ProfileId { get; set; }
    public long PointsBalance { get; set; }
}
