namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// A unit of runtime data seeding. Implementations are resolved from DI and
/// run sequentially in ascending <see cref="Order"/> after migrations on
/// application startup.
/// </summary>
/// <remarks>
/// <para>
/// Static reference data (categories, skills, terms versions, role rows) is
/// seeded declaratively via EF Core's <c>HasData</c> in entity configurations
/// and applied through migrations. Use this interface only for seed data that
/// requires application services at runtime — e.g. password hashing through
/// <c>UserManager</c>, role membership, or anything that varies by environment.
/// </para>
/// <para>
/// Each implementation must be idempotent: running it repeatedly against an
/// already-seeded database must not produce duplicates or errors.
/// </para>
/// </remarks>
public interface IDataSeeder
{
    /// <summary>
    /// Gets the relative ordering hint. Lower values run first. Use this to
    /// express dependencies between seeders without making the runner aware of
    /// them.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Apply this seeder. Implementations should be idempotent.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the seed operation.</param>
    /// <returns>A task representing the asynchronous seed operation.</returns>
    Task SeedAsync(CancellationToken cancellationToken);
}
