using Azure.Identity;
using Azure.Storage.Blobs;

namespace Backend.Infrastructure.Storage;

public static class BlobStorageServiceCollectionExtensions
{
    public static IServiceCollection AddBlobStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(BlobStorageOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton(_ =>
        {
            var accountName = Environment.GetEnvironmentVariable("AZURE_STORAGE_ACCOUNT_NAME");
            if (!string.IsNullOrEmpty(accountName))
            {
                // Azure: use managed identity — no connection string required
                var serviceUri = new Uri($"https://{accountName}.blob.core.windows.net");
                return new BlobServiceClient(serviceUri, new DefaultAzureCredential());
            }

            var connectionString = configuration["BlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "No blob storage config found. Set AZURE_STORAGE_ACCOUNT_NAME (Azure) or BlobStorage:ConnectionString (local).");

            return new BlobServiceClient(connectionString);
        });

        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        return services;
    }
}
