using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Persistence.Analytics;

public sealed class ProfileReputationRefreshBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ProfileReputationRefreshOptions> options,
    ILogger<ProfileReputationRefreshBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var refreshOptions = options.Value;
        if (!refreshOptions.Enabled)
        {
            logger.LogInformation("Profile reputation view refresh is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(refreshOptions.IntervalMinutes);

        if (refreshOptions.RunOnStartup)
        {
            await RunRefreshAsync(stoppingToken);
        }

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunRefreshAsync(stoppingToken);
        }
    }

    private async Task RunRefreshAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var refresher = scope.ServiceProvider.GetRequiredService<ProfileReputationViewRefresher>();
            await refresher.RefreshAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Profile reputation view refresh failed.");
        }
    }
}
