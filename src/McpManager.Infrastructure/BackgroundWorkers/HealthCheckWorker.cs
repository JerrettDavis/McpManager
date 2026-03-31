using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using McpManager.Core.Interfaces;

namespace McpManager.Infrastructure.BackgroundWorkers;

public class HealthCheckWorker(
    IServiceProvider serviceProvider,
    ILogger<HealthCheckWorker> logger
) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(7);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Health Check Worker starting (interval: {Interval}s)",
            _checkInterval.TotalSeconds);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthChecksAsync(stoppingToken);
                await CleanupOldRecordsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during health check cycle");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        logger.LogInformation("Health Check Worker stopping");
    }

    private async Task PerformHealthChecksAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var serverManager = scope.ServiceProvider.GetRequiredService<IServerManager>();
        var monitor = scope.ServiceProvider.GetRequiredService<IServerMonitor>();

        var servers = await serverManager.GetInstalledServersAsync();
        var serverList = servers.ToList();

        logger.LogDebug("Performing health checks for {Count} server(s)", serverList.Count);

        foreach (var server in serverList)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await monitor.StartMonitoringAsync(server.Id);
                var result = await monitor.PerformHealthCheckAsync(server.Id);
                logger.LogDebug(
                    "Health check for '{ServerId}': healthy={IsHealthy}, responseTime={ResponseTime}ms",
                    server.Id, result.IsHealthy, result.ResponseTimeMs);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Health check failed for server '{ServerId}'", server.Id);
            }
        }
    }

    private async Task CleanupOldRecordsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IHealthCheckRepository>();

        var cutoff = DateTime.UtcNow - _retentionPeriod;
        var deleted = await repo.DeleteOlderThanAsync(cutoff);
        if (deleted > 0)
        {
            logger.LogInformation("Cleaned up {Count} health check records older than {Cutoff}",
                deleted, cutoff);
        }
    }
}
