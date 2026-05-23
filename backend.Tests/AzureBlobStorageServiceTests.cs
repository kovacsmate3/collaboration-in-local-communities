using Backend.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace backend.Tests;

public sealed class AzureBlobStorageServiceTests
{
    [Fact]
    public async Task DeleteBlobByUrlAsync_IgnoresSilently_WhenContainerDoesNotMatch()
    {
        var (service, logger) = CreateService("media");
        var url = "https://fakeaccount.blob.core.windows.net/wrong-container/profiles/user/photo.jpg";

        await service.DeleteBlobByUrlAsync(url, CancellationToken.None);

        Assert.Empty(logger.Warnings);
    }

    [Fact]
    public async Task DeleteBlobByUrlAsync_RefusesAndLogs_WhenBlobNameLacksProfilesPrefix()
    {
        var (service, logger) = CreateService("media");
        var url = "https://fakeaccount.blob.core.windows.net/media/avatars/photo.jpg";

        await service.DeleteBlobByUrlAsync(url, CancellationToken.None);

        Assert.Contains(logger.Warnings, w => w.Contains("Refusing to delete blob"));
    }

    [Fact]
    public async Task DeleteBlobByUrlAsync_PassesPrefixCheck_ForProfilesBlob()
    {
        var (service, logger) = CreateService("media");
        var url = "https://fakeaccount.blob.core.windows.net/media/profiles/user-id/photo.jpg";

        // BlobServiceClient is null! — the prefix guard passes, GetContainer() throws NullReferenceException,
        // which the catch block handles and logs as "Failed to delete blob". The test verifies only
        // that the "Refusing" guard did not fire (prefix check passed as expected).
        await service.DeleteBlobByUrlAsync(url, CancellationToken.None);

        Assert.DoesNotContain(logger.Warnings, w => w.Contains("Refusing to delete blob"));
        Assert.Contains(logger.Warnings, w => w.Contains("Failed to delete blob"));
    }

    private static (AzureBlobStorageService service, SpyLogger<AzureBlobStorageService> logger) CreateService(
        string containerName)
    {
        var logger = new SpyLogger<AzureBlobStorageService>();
        var options = Options.Create(new BlobStorageOptions { ContainerName = containerName });
        var service = new AzureBlobStorageService(null!, options, logger);
        return (service, logger);
    }

    private sealed class SpyLogger<T> : ILogger<T>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
        }
    }
}
