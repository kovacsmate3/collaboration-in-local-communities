using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence;
using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();

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

        dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
        {
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]), ct);
            return token.Token;
        }, TimeSpan.FromMinutes(50), TimeSpan.FromSeconds(10));
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
        logger.LogInformation("Connected to database at {Host}: {Version}",
            db.Database.GetDbConnection().DataSource, version);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database connection check failed");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) || !inContainer)
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health");

app.Run();

static int GetOptionalPort()
{
    var value = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_PORT");
    if (string.IsNullOrWhiteSpace(value))
        return 5432;
    if (!int.TryParse(value, out var port))
        throw new InvalidOperationException($"Environment variable 'AZURE_POSTGRESQL_PORT' has an invalid value '{value}'. Expected a valid integer port number.");
    return port;
}

static string GetRequiredEnvironmentVariable(string name)
{
    return Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Missing required environment variable: {name}.");
}
