using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using McpManager.Infrastructure.Services;

namespace McpManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that periodically updates package download statistics.
/// Runs less frequently than registry refresh to avoid excessive API calls.
/// </summary>
public class DownloadStatsWorker(
    IServiceProvider serviceProvider,
    ILogger<DownloadStatsWorker> logger
)
    : BackgroundService
{
    private readonly TimeSpan _refreshInterval = TimeSpan.FromDays(1); // Daily updates

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Download Stats Worker starting");

        // Initial delay to let the application start up and registries populate
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateDownloadStatsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during download stats update");
            }

            // Wait for next refresh interval
            await Task.Delay(_refreshInterval, stoppingToken);
        }

        logger.LogInformation("Download Stats Worker stopping");
    }

    private async Task UpdateDownloadStatsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var statsService = scope.ServiceProvider.GetRequiredService<IDownloadStatsService>();

        logger.LogInformation("Starting download statistics update");

        try
        {
            await statsService.UpdateDownloadCountsAsync();
            logger.LogInformation("Download statistics update completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update download statistics");
        }
    }
}
