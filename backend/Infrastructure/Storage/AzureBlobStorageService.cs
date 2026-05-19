using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Storage;

public sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    public async Task<Uri> UploadProfilePhotoAsync(
        Guid userId,
        Stream content,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var blobName = $"profiles/{userId}/{Guid.NewGuid():N}.{fileExtension}";
        var container = GetContainer();
        var blobClient = container.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            },
            cancellationToken);

        return blobClient.Uri;
    }

    public async Task DeleteBlobByUrlAsync(string blobUrl, CancellationToken cancellationToken)
    {
        try
        {
            var parsed = new BlobUriBuilder(new Uri(blobUrl));
            if (!string.Equals(parsed.BlobContainerName, options.Value.ContainerName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var container = GetContainer();
            await container.GetBlobClient(parsed.BlobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob at {BlobUrl}.", blobUrl);
        }
    }

    public async Task EnsureContainerExistsAsync(CancellationToken cancellationToken)
    {
        var container = GetContainer();
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
    }

    private BlobContainerClient GetContainer() =>
        blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
}
