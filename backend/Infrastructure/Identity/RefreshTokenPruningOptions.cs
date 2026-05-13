using System.ComponentModel.DataAnnotations;

namespace Backend.Infrastructure.Identity;

public sealed class RefreshTokenPruningOptions
{
    public const string SectionName = "RefreshTokenPruning";

    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; } = true;

    [Range(1, 24 * 30)]
    public int IntervalHours { get; set; } = 24;

    [Range(1, 365)]
    public int RetentionDays { get; set; } = 7;
}
