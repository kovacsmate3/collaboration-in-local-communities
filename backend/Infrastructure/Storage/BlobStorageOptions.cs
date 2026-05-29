namespace Backend.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Gets or sets the storage account name. When set, the client authenticates with a
    /// managed identity against <c>https://&lt;account&gt;.blob.core.windows.net</c> (Azure).
    /// When unset, <see cref="ConnectionString"/> is used (local/Azurite).
    /// </summary>
    public string? AccountName { get; set; }

    /// <summary>
    /// Gets or sets the blob storage connection string used when <see cref="AccountName"/>
    /// is not supplied (local development against Azurite).
    /// </summary>
    public string? ConnectionString { get; set; }

    public string ContainerName { get; set; } = "profile-photos";

    /// <summary>
    /// Gets or Sets the optional base URL used to rewrite blob URIs returned to clients.
    /// Set this when the storage endpoint reachable by the backend differs from the one
    /// reachable by clients (e.g. <c>http://azurite:10000</c> inside Docker vs
    /// <c>http://localhost:10000</c> for browsers).
    /// </summary>
    public string? PublicEndpoint { get; set; }
}
