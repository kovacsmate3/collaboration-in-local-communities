namespace Backend.Infrastructure.Storage;

/// <summary>
/// Abstracts blob storage operations for profile photo management.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a profile photo and returns the public URI of the stored blob.
    /// </summary>
    /// <param name="userId">The ID of the user who owns the photo, used to namespace the blob path.</param>
    /// <param name="content">The image data to upload.</param>
    /// <param name="contentType">The MIME type of the image (e.g. <c>image/jpeg</c>).</param>
    /// <param name="fileExtension">The file extension without a leading dot (e.g. <c>jpg</c>).</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The public URI of the uploaded blob.</returns>
    Task<Uri> UploadProfilePhotoAsync(
        Guid userId,
        Stream content,
        string contentType,
        string fileExtension,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the blob identified by the given URL, if it exists.
    /// Does nothing when the URL belongs to a different container or cannot be parsed.
    /// </summary>
    /// <param name="blobUrl">The full URL of the blob to delete.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteBlobByUrlAsync(string blobUrl, CancellationToken cancellationToken);

    /// <summary>
    /// Creates the configured blob container if it does not already exist.
    /// Intended to be called once at application startup.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A task that represents the asynchronous initialization operation.</returns>
    Task EnsureContainerExistsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rewrites a blob URL stored in the database so that its host matches the configured
    /// public endpoint. Use this whenever a stored photo URL is included in an API response,
    /// so clients always receive a URL they can reach (e.g. <c>localhost</c> rather than an
    /// internal Docker service name). Returns <see langword="null"/> when <paramref name="url"/>
    /// is <see langword="null"/> or empty, and returns the URL unchanged when no public
    /// endpoint override is configured.
    /// </summary>
    /// <param name="url">The blob URL as stored in the database, or <see langword="null"/>.</param>
    /// <returns>The client-facing URL, or <see langword="null"/>.</returns>
    string? RewriteToPublicUrl(string? url);
}
