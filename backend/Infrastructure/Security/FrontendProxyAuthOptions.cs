using System.ComponentModel.DataAnnotations;

namespace Backend.Infrastructure.Security;

public sealed class FrontendProxyAuthOptions
{
    public const string SectionName = "FrontendProxyAuth";

    public bool Enabled { get; set; } = true;

    [MinLength(32)]
    public string? SigningKey { get; set; }

    [Range(1, 600)]
    public int MaxTokenLifetimeSeconds { get; set; } = 60;

    [Range(0, 60)]
    public int ClockSkewSeconds { get; set; } = 5;
}
