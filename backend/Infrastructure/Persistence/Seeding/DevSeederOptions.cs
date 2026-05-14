namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Strongly-typed configuration for the development seeders. Bound from the
/// <c>DevSeed</c> section of configuration in <c>Program.cs</c> via
/// <c>services.Configure&lt;DevSeederOptions&gt;(...)</c>.
/// </summary>
public sealed class DevSeederOptions
{
    /// <summary>
    /// Configuration section name used by <c>Configure&lt;DevSeederOptions&gt;</c>.
    /// </summary>
    public const string SectionName = "DevSeed";

    /// <summary>
    /// Gets or sets a value indicating whether development seeders are
    /// registered.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the dev admin account specification.</summary>
    public DevSeedAccount Admin { get; set; } = new()
    {
        Email = "admin@local.test",
        Password = "Admin123!",
        DisplayName = "Local Admin",
        Workplace = "Operations",
        Position = "Platform administrator",
    };

    /// <summary>Gets or sets the dev regular-user account specification.</summary>
    public DevSeedAccount User { get; set; } = new()
    {
        Email = "user@local.test",
        Password = "User123!",
        DisplayName = "Local User",
        Workplace = "Neighbourhood",
        Position = "Community member",
    };
}

/// <summary>
/// One seeded development account. Profile fields default to non-null
/// strings so the seeder always produces a complete <c>UserProfile</c>.
/// </summary>
public sealed class DevSeedAccount
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Workplace { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string LocationText { get; set; } = "Local development";
    public string Bio { get; set; } = "Seeded account for local authentication development.";
}
