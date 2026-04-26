using Azure.Identity;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

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
