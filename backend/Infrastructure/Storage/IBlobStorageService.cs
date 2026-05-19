namespace Backend.Infrastructure.Storage;

public interface IBlobStorageService
{
    Task<Uri> UploadProfilePhotoAsync(
        Guid userId,
        Stream content,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken);

    Task DeleteBlobByUrlAsync(string blobUrl, CancellationToken cancellationToken);

    Task EnsureContainerExistsAsync(CancellationToken cancellationToken);
}
