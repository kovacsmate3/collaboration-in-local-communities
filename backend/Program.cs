using System.Net;
using Azure.Core;
using Azure.Identity;
using Backend.Application.Categories;
using Backend.Application.TaskCompletion;
using Backend.Features.Auth;
using Backend.Features.Conversations;
using Backend.Infrastructure.Azure;
using Backend.Infrastructure.Email;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.OpenApi;
using Backend.Infrastructure.Persistence;
using Backend.Infrastructure.Persistence.Queries;
using Backend.Infrastructure.Persistence.Seeding;
using Backend.Infrastructure.Security;
using Backend.Infrastructure.Storage;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Bind application-owned Azure settings from the "Azure" configuration section.
// Values flow through any ASP.NET Core configuration provider (appsettings,
// environment variables like Azure__CosmosEndpoint / Azure__Postgres__Host, etc.).
//
// Back-compat: the deployed Azure Container App still injects the legacy AZURE_*
// Service Connector variables. Alias them onto the structured Azure:* keys (only when
// the structured key isn't already supplied) so both forms work until the Container App
// settings are migrated. This reads through builder.Configuration, so no direct
// Environment.GetEnvironmentVariable calls are needed.
var legacyAzureAliases = new (string LegacyKey, string StructuredKey)[]
{
    ("AZURE_COSMOS_ENDPOINT", "Azure:CosmosEndpoint"),
    ("AZURE_POSTGRESQL_HOST", "Azure:Postgres:Host"),
    ("AZURE_POSTGRESQL_PORT", "Azure:Postgres:Port"),
    ("AZURE_POSTGRESQL_DATABASE", "Azure:Postgres:Database"),
    ("AZURE_POSTGRESQL_USERNAME", "Azure:Postgres:Username"),
};

var aliasedAzureSettings = new Dictionary<string, string?>();
foreach (var (legacyKey, structuredKey) in legacyAzureAliases)
{
    var legacyValue = builder.Configuration[legacyKey];
    if (!string.IsNullOrEmpty(legacyValue)
        && string.IsNullOrEmpty(builder.Configuration[structuredKey]))
    {
        aliasedAzureSettings[structuredKey] = legacyValue;
    }
}

if (aliasedAzureSettings.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(aliasedAzureSettings);
}

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
builder.Services.AddSingleton<ITaskCompletionPointsCalculator, FlatTaskCompletionPointsCalculator>();
builder.Services.AddScoped<ITaskCompletionRewardService, TaskCompletionRewardService>();
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
builder.Services.AddEmailSender(builder.Configuration);
builder.Services.AddBlobStorage(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:3000";
        policy
            .WithOrigins(frontendUrl)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddSingleton<CosmosMessageService>();
builder.Services.AddControllers();
builder.Services.AddOutputCache();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Only trust X-Forwarded-Proto from upstream proxies so Request.IsHttps reflects the
    // original scheme (used for Secure cookies). X-Forwarded-For is intentionally not
    // processed: the real client IP comes from the signed proxy token (FrontendProxyAuth).
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;

    // ACA's Envoy ingress reaches the app pod from a private RFC1918 address; trusting these
    // ranges lets the framework accept the X-Forwarded-Proto header it sets.
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
    options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
});
builder.Services.AddFrontendProxyAuth(builder.Configuration);
builder.Services.AddAppRateLimiting(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddDevelopmentDataSeeders(builder.Configuration, builder.Environment);

var app = builder.Build();

var proxyAuth = app.Services.GetRequiredService<IOptions<FrontendProxyAuthOptions>>().Value;
if (proxyAuth.Enabled
    && !app.Environment.IsDevelopment()
    && string.IsNullOrWhiteSpace(proxyAuth.SigningKey))
{
    throw new InvalidOperationException(
        "FrontendProxyAuth:SigningKey is required when FrontendProxyAuth is enabled outside Development. "
        + "Set FrontendProxyAuth__SigningKey or disable it via FrontendProxyAuth__Enabled=false.");
}

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

{
    var cosmosMessages = app.Services.GetRequiredService<CosmosMessageService>();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        await cosmosMessages.InitializeAsync();
        logger.LogInformation("CosmosDB messages container ready.");
    }
    catch (Exception ex)
    {
        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning(ex, "CosmosDB messages container initialization failed (non-fatal in Development)");
        }
        else
        {
            throw;
        }
    }
}

{
    using var blobScope = app.Services.CreateScope();
    var blobStorage = blobScope.ServiceProvider.GetRequiredService<IBlobStorageService>();
    var logger = blobScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await blobStorage.EnsureContainerExistsAsync(CancellationToken.None);
        logger.LogInformation("Blob storage container ready.");
    }
    catch (Exception ex)
    {
        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning(ex, "Blob storage container initialization failed (non-fatal in Development)");
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

app.UseForwardedHeaders();
app.UseFrontendProxyAuth();

// Skip HTTPS redirect inside Docker containers (HTTP-only on port 8080).
// DOTNET_RUNNING_IN_CONTAINER is set by the official .NET base images; read it through
// configuration (consistent with the rest of the composition root) with a tolerant
// parse so a non-boolean value can't crash startup — unknown/missing means "not in a
// container", keeping HTTPS redirection on.
var runningInContainer =
    bool.TryParse(builder.Configuration["DOTNET_RUNNING_IN_CONTAINER"], out var inContainer) && inContainer;
if (!runningInContainer)
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
