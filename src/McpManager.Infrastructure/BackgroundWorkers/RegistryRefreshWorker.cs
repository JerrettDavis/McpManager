using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using McpManager.Core.Interfaces;

namespace McpManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that periodically refreshes cached registry data.
/// Runs asynchronously without blocking the UI.
/// </summary>
public class RegistryRefreshWorker(
    IServiceProvider serviceProvider,
    ILogger<RegistryRefreshWorker> logger
)
    : BackgroundService
{
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(60); // Default 1 hour

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Registry Refresh Worker starting");

        // Initial delay to let the application start up (reduced to 2 seconds for better UX)
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshRegistriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during registry refresh");
            }

            // Wait for next refresh interval
            await Task.Delay(_refreshInterval, stoppingToken);
        }

        logger.LogInformation("Registry Refresh Worker stopping");
    }

    private async Task RefreshRegistriesAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IRegistryCacheRepository>();
        var registries = scope.ServiceProvider.GetServices<IServerRegistry>().ToList();

        logger.LogInformation("Starting registry refresh for {Count} registries", registries.Count);

        foreach (var registry in registries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await RefreshRegistryAsync(registry, cacheRepository, cancellationToken);
        }

        logger.LogInformation("Registry refresh completed");
    }

    private async Task RefreshRegistryAsync(
        IServerRegistry registry, 
        IRegistryCacheRepository cacheRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Refreshing registry: {RegistryName}", registry.Name);

            // For cached registries, force a refresh by checking if cache is stale
            // The CachedServerRegistry will handle calling the inner registry and caching
            var servers = await registry.GetAllServersAsync();
            var serverList = servers.ToList();

            // Ensure the cache is updated
            var count = await cacheRepository.UpsertManyAsync(registry.Name, serverList);
            await cacheRepository.UpdateRegistryMetadataAsync(registry.Name, count, true);

            logger.LogInformation(
                "Successfully refreshed {RegistryName}: {Count} servers cached",
                registry.Name, count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh registry: {RegistryName}", registry.Name);
            await cacheRepository.UpdateRegistryMetadataAsync(
                registry.Name, 
                0, 
                false, 
                ex.Message);
        }
    }
}
