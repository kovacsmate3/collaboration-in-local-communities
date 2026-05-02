using Azure.Core;
using Azure.Identity;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var applicationInsightsConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(applicationInsightsConnectionString))
{
    // Only add Application Insights if the connection string is present in configuration
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsConnectionString;
    });
}

builder.Services.AddOpenApi();

builder.Services.AddSingleton(_ =>
{
    var azureEndpoint = Environment.GetEnvironmentVariable("AZURE_COSMOS_ENDPOINT");

    if (!string.IsNullOrEmpty(azureEndpoint))
    {
        // Azure: use managed identity, no key needed
        return new CosmosClient(azureEndpoint, new DefaultAzureCredential());
    }

    // Key-based: Development uses emulator config; other envs must supply explicit config
    var endpoint = builder.Configuration["CosmosDb:AccountEndpoint"]
        ?? throw new InvalidOperationException(
            "No CosmosDB config found. Set AZURE_COSMOS_ENDPOINT (Azure) or CosmosDb:AccountEndpoint (local).");
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
    var azureHost = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_HOST");
    NpgsqlDataSourceBuilder dataSourceBuilder;

    if (!string.IsNullOrEmpty(azureHost))
    {
        // Azure: Service Connector vars + managed identity token
        var azureConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = azureHost,
            Port = GetOptionalPort(),
            Database = GetRequiredEnvironmentVariable("AZURE_POSTGRESQL_DATABASE"),
            Username = GetRequiredEnvironmentVariable("AZURE_POSTGRESQL_USERNAME"),
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
                "No database configuration found. Set AZURE_POSTGRESQL_HOST (for Azure) or ConnectionStrings:DefaultConnection (for local).");
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

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddControllers();
builder.Services.AddOutputCache();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

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
        logger.LogError(ex, "CosmosDB connection check failed");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Skip HTTPS redirect inside Docker containers (HTTP-only on port 8080)
if (!bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) || !inContainer)
{
    app.UseHttpsRedirection();
}

app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

app.MapControllers();

app.Run();

static int GetOptionalPort()
{
    var value = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_PORT");
    if (string.IsNullOrWhiteSpace(value))
    {
        return 5432;
    }

    if (!int.TryParse(value, out var port))
    {
        throw new InvalidOperationException(
            $"Environment variable 'AZURE_POSTGRESQL_PORT' has an invalid value '{value}'. Expected a valid integer port number.");
    }

    return port;
}

static string GetRequiredEnvironmentVariable(string name)
{
    return Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Missing required environment variable: {name}.");
}
