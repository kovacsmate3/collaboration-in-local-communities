using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Persistence.Seeding;

/// <summary>
/// Service-collection helpers for registering and running <see cref="IDataSeeder"/>
/// implementations. Keeps <c>Program.cs</c> declarative and lets new seeders
/// plug in with a single <c>AddDataSeeder&lt;T&gt;()</c> call.
/// </summary>
public static partial class DataSeederExtensions
{
    /// <summary>
    /// Bind development seeder options and register the built-in development
    /// seeders when the app is running in Development and seeding is enabled.
    /// </summary>
    public static IServiceCollection AddDevelopmentDataSeeders(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var section = configuration.GetSection(DevSeederOptions.SectionName);
        services.Configure<DevSeederOptions>(section);

        var options = new DevSeederOptions();
        section.Bind(options);

        if (!environment.IsDevelopment() || !options.Enabled)
        {
            return services;
        }

        services.AddScoped<UserSeedingHelper>();
        services
            .AddDataSeeder<DevelopmentAdminSeeder>()
            .AddDataSeeder<DevelopmentUserSeeder>();

        return services;
    }

    /// <summary>
    /// Register an <see cref="IDataSeeder"/> implementation with the DI container.
    /// </summary>
    public static IServiceCollection AddDataSeeder<TSeeder>(this IServiceCollection services)
        where TSeeder : class, IDataSeeder
    {
        services.AddScoped<IDataSeeder, TSeeder>();
        return services;
    }

    /// <summary>
    /// Resolve every registered <see cref="IDataSeeder"/> from the supplied scope
    /// and run them sequentially in <see cref="IDataSeeder.Order"/> order.
    /// </summary>
    public static async Task RunDataSeedersAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var logger = services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Backend.Infrastructure.Persistence.Seeding");

        var seeders = services
            .GetServices<IDataSeeder>()
            .OrderBy(seeder => seeder.Order)
            .ThenBy(seeder => seeder.GetType().FullName, StringComparer.Ordinal)
            .ToList();

        if (seeders.Count == 0)
        {
            NoDataSeedersRegistered(logger);
            return;
        }

        RunningDataSeeders(logger, seeders.Count);

        foreach (var seeder in seeders)
        {
            var name = seeder.GetType().Name;
            try
            {
                await seeder.SeedAsync(cancellationToken);
                SeederFinished(logger, name);
            }
            catch (Exception ex)
            {
                SeederFailed(logger, ex, name);
                throw;
            }
        }
    }

    [LoggerMessage(EventId = 1200, Level = LogLevel.Debug, Message = "No data seeders registered; skipping.")]
    private static partial void NoDataSeedersRegistered(ILogger logger);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Information, Message = "Running {Count} data seeder(s).")]
    private static partial void RunningDataSeeders(ILogger logger, int count);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Debug, Message = "Seeder {Seeder} finished.")]
    private static partial void SeederFinished(ILogger logger, string seeder);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Error, Message = "Seeder {Seeder} failed.")]
    private static partial void SeederFailed(ILogger logger, Exception exception, string seeder);
}
