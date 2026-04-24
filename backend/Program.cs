using Azure.Core;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var azureHost = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_HOST");

    NpgsqlDataSource dataSource;

    if (!string.IsNullOrEmpty(azureHost))
    {
        // Azure: Service Connector vars + managed identity token
        var azureConnectionString = new NpgsqlConnectionStringBuilder
        {
            Host = azureHost,
            Port = int.Parse(Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_PORT") ?? "5432"),
            Database = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_DATABASE"),
            Username = Environment.GetEnvironmentVariable("AZURE_POSTGRESQL_USERNAME"),
            SslMode = SslMode.Require
        }.ToString();

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(azureConnectionString);
        dataSourceBuilder.UsePeriodicPasswordProvider(async (_, ct) =>
        {
            var credential = new DefaultAzureCredential();
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(["https://ossrdbms-aad.database.windows.net/.default"]), ct);
            return token.Token;
        }, TimeSpan.FromMinutes(50), TimeSpan.FromSeconds(10));

        dataSource = dataSourceBuilder.Build();
    }
    else
    {
        // Local / fallback: plain connection string from config
        var localConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No database configuration found. Set AZURE_POSTGRESQL_HOST (for Azure) or ConnectionStrings:DefaultConnection (for local).");
        dataSource = new NpgsqlDataSourceBuilder(localConnectionString).Build();
    }

    options.UseNpgsql(dataSource);
});

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

string[] summaries = [
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
