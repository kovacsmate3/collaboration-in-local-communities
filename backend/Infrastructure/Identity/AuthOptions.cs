namespace Backend.Infrastructure.Identity;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = "collaboration-in-local-communities";
    public string Audience { get; set; } = "collaboration-in-local-communities-api";
    public string? SigningKey { get; set; }
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
