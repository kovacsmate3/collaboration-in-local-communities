using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Storage;

public sealed class AzureBlobStorageService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options,
    ILogger<AzureBlobStorageService> logger) : IBlobStorageService
{
    private static readonly Dictionary<string, string> MimeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    private BlobContainerClient GetContainer() =>
        blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);

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

        await blobClient.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
        }, cancellationToken);

        return blobClient.Uri;
    }

    public async Task DeleteBlobByUrlAsync(string blobUrl, CancellationToken cancellationToken)
    {
        try
        {
            var uri = new Uri(blobUrl);
            // URI path is /<container>/<blob-name…>
            var containerName = options.Value.ContainerName;
            var prefix = $"/{containerName}/";
            if (!uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var blobName = uri.AbsolutePath[prefix.Length..];
            var container = GetContainer();
            await container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken);
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
}
