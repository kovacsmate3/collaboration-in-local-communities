namespace Backend.Infrastructure.Security;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimit";

    public WindowPolicyOptions Auth { get; init; } = new();
    public WindowPolicyOptions Conversations { get; init; } = new();
    public WindowPolicyOptions Reviews { get; init; } = new();
    public WindowPolicyOptions PhotoUpload { get; init; } = new() { PermitLimit = 10, WindowSeconds = 600 };

    // Generous read budget for high-frequency browsing (tasks list/detail and
    // reference catalogues like categories, skills, terms, profile reads).
    public WindowPolicyOptions TasksRead { get; init; } = new() { PermitLimit = 120, WindowSeconds = 60 };

    // Tighter budget for state-changing endpoints (post / update / cancel a
    // task, apply / withdraw / accept an application, submit / approve a
    // completion). Abuse here costs real domain integrity.
    public WindowPolicyOptions TasksWrite { get; init; } = new() { PermitLimit = 20, WindowSeconds = 60 };

    // External geocoding (Nominatim) proxy — each call has a real upstream
    // cost and the provider's own usage policy, so cap independently.
    public WindowPolicyOptions Locations { get; init; } = new() { PermitLimit = 30, WindowSeconds = 60 };

    // Admin endpoints are already gated behind the Admin role, so the cap is
    // mainly a guard against runaway scripts and accidental misuse.
    public WindowPolicyOptions Admin { get; init; } = new() { PermitLimit = 60, WindowSeconds = 60 };
}

public sealed class WindowPolicyOptions
{
    public int PermitLimit { get; init; } = 10;
    public int WindowSeconds { get; init; } = 60;
}
