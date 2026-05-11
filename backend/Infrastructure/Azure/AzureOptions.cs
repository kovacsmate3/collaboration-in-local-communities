namespace Backend.Infrastructure.Azure;

/// <summary>
/// Strongly-typed bindings for application-owned Azure configuration.
/// Read via <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> from
/// the <c>Azure</c> section so values can be supplied through any provider
/// (appsettings, environment variables, Key Vault, etc.).
/// </summary>
internal sealed class AzureOptions
{
    public const string SectionName = "Azure";

    /// <summary>
    /// Gets the Cosmos DB account endpoint (e.g. <c>https://&lt;account&gt;.documents.azure.com:443/</c>).
    /// When set, the host uses managed-identity auth; when unset, the local-emulator
    /// fallback in <c>CosmosDb:AccountEndpoint</c>/<c>CosmosDb:AccountKey</c> applies.
    /// </summary>
    public string? CosmosEndpoint { get; init; }

    /// <summary>
    /// Gets the Azure Database for PostgreSQL connection settings used in production.
    /// </summary>
    public AzurePostgresOptions Postgres { get; init; } = new();
}

/// <summary>
/// Azure Database for PostgreSQL connection settings. When <see cref="Host"/> is set,
/// the host wires up an Npgsql data source authenticated with a managed-identity AAD
/// token; otherwise it falls back to the local <c>ConnectionStrings:DefaultConnection</c>.
/// </summary>
internal sealed class AzurePostgresOptions
{
    public string? Host { get; init; }

    public int Port { get; init; } = 5432;

    public string? Database { get; init; }

    public string? Username { get; init; }
}
