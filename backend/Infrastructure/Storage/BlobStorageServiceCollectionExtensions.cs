using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

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

        services.AddSingleton(serviceProvider =>
        {
            // Read everything from the bound options (configuration) rather than the
            // environment directly, so the lookup is consistent with the rest of the host.
            var options = serviceProvider.GetRequiredService<IOptions<BlobStorageOptions>>().Value;

            if (!string.IsNullOrEmpty(options.AccountName))
            {
                // Azure: use managed identity — no connection string required
                var serviceUri = new Uri($"https://{options.AccountName}.blob.core.windows.net");
                return new BlobServiceClient(serviceUri, new DefaultAzureCredential());
            }

            var connectionString = options.ConnectionString
                ?? throw new InvalidOperationException(
                    "No blob storage config found. Set BlobStorage:AccountName (Azure) or BlobStorage:ConnectionString (local).");

            return new BlobServiceClient(connectionString);
        });

        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        return services;
    }
}
