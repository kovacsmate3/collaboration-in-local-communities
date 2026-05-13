using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Identity;

public sealed class RefreshTokenPruningBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<RefreshTokenPruningOptions> options,
    ILogger<RefreshTokenPruningBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pruningOptions = options.Value;
        if (!pruningOptions.Enabled)
        {
            logger.LogInformation("Refresh token pruning is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(pruningOptions.IntervalHours);

        if (pruningOptions.RunOnStartup)
        {
            await RunPruningAsync(stoppingToken);
        }

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunPruningAsync(stoppingToken);
        }
    }

    private async Task RunPruningAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var pruner = scope.ServiceProvider.GetRequiredService<RefreshTokenPruner>();
            var cutoff = DateTimeOffset.UtcNow.AddDays(-options.Value.RetentionDays);
            await pruner.PruneAsync(cutoff, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Refresh token pruning failed.");
        }
    }
}
