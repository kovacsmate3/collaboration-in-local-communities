namespace Backend.Infrastructure.Security;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimit";

    public WindowPolicyOptions Auth { get; init; } = new();
    public WindowPolicyOptions Conversations { get; init; } = new();
    public WindowPolicyOptions Reviews { get; init; } = new();
    public WindowPolicyOptions PhotoUpload { get; init; } = new() { PermitLimit = 10, WindowSeconds = 600 };
}

public sealed class WindowPolicyOptions
{
    public int PermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 60;
}
