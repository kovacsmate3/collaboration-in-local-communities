using Azure.Core;
using Azure.Identity;
using Backend.Application.Categories;
using Backend.Features.Auth;
using Backend.Infrastructure.Azure;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.OpenApi;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Persistence.Queries;
using Backend.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Bind application-owned Azure settings from the "Azure" configuration section.
// Values flow through any ASP.NET Core configuration provider (appsettings,
// environment variables like Azure__CosmosEndpoint / Azure__Postgres__Host, etc.).
var azureOptions = builder.Configuration
    .GetSection(AzureOptions.SectionName)
    .Get<AzureOptions>() ?? new AzureOptions();

var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
{
    // Only add Application Insights if the connection string is present in configuration
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsConnectionString;
    });
}

builder.Services.AddOpenApiWithJwt();

builder.Services.AddSingleton(_ =>
{
    if (!string.IsNullOrEmpty(azureOptions.CosmosEndpoint))
    {
        // Azure: use managed identity, no key needed
        return new CosmosClient(azureOptions.CosmosEndpoint, new DefaultAzureCredential());
    }

    // Key-based: Development uses emulator config; other envs must supply explicit config
    var endpoint = builder.Configuration["CosmosDb:AccountEndpoint"]
        ?? throw new InvalidOperationException(
            "No CosmosDB config found. Set Azure:CosmosEndpoint (Azure) or CosmosDb:AccountEndpoint (local).");
    var key = builder.Configuration["CosmosDb:AccountKey"]
        ?? throw new InvalidOperationException("CosmosDb:AccountKey required when not using managed identity.");

    var host = new Uri(endpoint).Host;
    var isLocalEmulator = host is "localhost" or "127.0.0.1" or "cosmos";

    if (isLocalEmulator || builder.Environment.IsDevelopment())
    {
        return new CosmosClient(endpoint, key, new CosmosClientOptions
        {
            HttpClientFactory = () => new HttpClient(new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }),
            ConnectionMode = ConnectionMode.Gateway
        });
    }

    return new CosmosClient(endpoint, key);
});

builder.Services.AddSingleton(_ =>
{
    NpgsqlDataSourceBuilder dataSourceBuilder;

    if (!string.IsNullOrEmpty(azureOptions.Postgres.Host))
    {
        // Azure: Service Connector vars + managed identity token
        var azureConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = azureOptions.Postgres.Host,
            Port = azureOptions.Postgres.Port,
            Database = azureOptions.Postgres.Database
                ?? throw new InvalidOperationException("Missing required configuration value: 'Azure:Postgres:Database'."),
            Username = azureOptions.Postgres.Username
                ?? throw new InvalidOperationException("Missing required configuration value: 'Azure:Postgres:Username'."),
            SslMode = SslMode.Require
        }.ToString();

        dataSourceBuilder = new NpgsqlDataSourceBuilder(azureConnectionString);
        var credential = new DefaultAzureCredential();

        dataSourceBuilder.UsePeriodicPasswordProvider(
            async (_, ct) =>
            {
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]),
                    ct);
                return token.Token;
            },
            TimeSpan.FromMinutes(50),
            TimeSpan.FromSeconds(10));
    }
    else
    {
        // Local / fallback: plain connection string from config
        var localConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No database configuration found. Set Azure:Postgres:Host (for Azure) or ConnectionStrings:DefaultConnection (for local).");
        dataSourceBuilder = new NpgsqlDataSourceBuilder(localConnectionString);
    }

    dataSourceBuilder.UseNetTopologySuite();
    return dataSourceBuilder.Build();
});

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
    options.UseNpgsql(dataSource, npgsql => npgsql.UseNetTopologySuite());
});
builder.Services.AddScoped<IListCategoriesQuery, EfListCategoriesQuery>();
builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
builder.Services.AddScoped<RefreshTokenPruner>();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("Nominatim", client =>
{
    var baseUrl = builder.Configuration["Nominatim:BaseUrl"]?.Trim()
        ?? "https://nominatim.openstreetmap.org";
    var userAgent = builder.Configuration["Nominatim:UserAgent"]?.Trim()
        ?? "2gather-local-community-platform/1.0";
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});
builder.Services.AddOptions<RefreshTokenPruningOptions>()
    .Bind(builder.Configuration.GetSection(RefreshTokenPruningOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<RefreshTokenPruningBackgroundService>();

builder.Services.AddApplicationIdentity();

builder.Services.AddControllers();
builder.Services.AddOutputCache();
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddDevelopmentDataSeeders(builder.Configuration, builder.Environment);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await db.Database.OpenConnectionAsync();
        await using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT version()";
        var version = await cmd.ExecuteScalarAsync();
        logger.LogInformation(
            "Connected to database at {Host}: {Version}",
            db.Database.GetDbConnection().DataSource,
            version);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connection check failed");
        throw;
    }
}

{
    var cosmos = app.Services.GetRequiredService<CosmosClient>();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        var props = await cosmos.ReadAccountAsync();
        logger.LogInformation("Connected to CosmosDB account: {AccountId}", props.Id);
    }
    catch (Exception ex)
    {
        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning(ex, "CosmosDB connection check failed (non-fatal in Development)");
        }
        else
        {
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarWithJwt();

    using var seedScope = app.Services.CreateScope();
    var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await seedScope.ServiceProvider.RunDataSeedersAsync();
}

// Skip HTTPS redirect inside Docker containers (HTTP-only on port 8080).
// The DOTNET_RUNNING_IN_CONTAINER variable is set by the official .NET base images;
// reading it via configuration keeps the lookup consistent with the rest of the
// composition root.
if (!builder.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

app.MapControllers();

app.Run();
