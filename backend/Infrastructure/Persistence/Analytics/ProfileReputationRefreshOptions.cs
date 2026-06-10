using System.ComponentModel.DataAnnotations;

namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class ProfileReputationRefreshOptions
{
    public const string SectionName = "ProfileReputationRefresh";

    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; } = true;

    [Range(1, 24 * 60)]
    public int IntervalMinutes { get; set; } = 15;
}
