using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using McpManager.Core.Interfaces;

namespace McpManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that periodically refreshes cached registry data.
/// Runs asynchronously without blocking the UI.
/// </summary>
public class RegistryRefreshWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RegistryRefreshWorker> _logger;
    private readonly TimeSpan _refreshInterval;

    public RegistryRefreshWorker(
        IServiceProvider serviceProvider,
        ILogger<RegistryRefreshWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _refreshInterval = TimeSpan.FromMinutes(60); // Default 1 hour
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Registry Refresh Worker starting");

        // Initial delay to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshRegistriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during registry refresh");
            }

            // Wait for next refresh interval
            await Task.Delay(_refreshInterval, stoppingToken);
        }

        _logger.LogInformation("Registry Refresh Worker stopping");
    }

    private async Task RefreshRegistriesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IRegistryCacheRepository>();
        var registries = scope.ServiceProvider.GetServices<IServerRegistry>().ToList();

        _logger.LogInformation("Starting registry refresh for {Count} registries", registries.Count);

        foreach (var registry in registries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await RefreshRegistryAsync(registry, cacheRepository, cancellationToken);
        }

        _logger.LogInformation("Registry refresh completed");
    }

    private async Task RefreshRegistryAsync(
        IServerRegistry registry, 
        IRegistryCacheRepository cacheRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Refreshing registry: {RegistryName}", registry.Name);

            // Check if this is a cached wrapper - if so, skip it to avoid double-wrapping
            if (registry is ICachedServerRegistry)
            {
                _logger.LogDebug("Skipping cached wrapper for {RegistryName}", registry.Name);
                return;
            }

            var servers = await registry.GetAllServersAsync();
            var serverList = servers.ToList();

            var count = await cacheRepository.UpsertManyAsync(registry.Name, serverList);
            await cacheRepository.UpdateRegistryMetadataAsync(registry.Name, count, true);

            _logger.LogInformation(
                "Successfully refreshed {RegistryName}: {Count} servers cached",
                registry.Name, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh registry: {RegistryName}", registry.Name);
            await cacheRepository.UpdateRegistryMetadataAsync(
                registry.Name, 
                0, 
                false, 
                ex.Message);
        }
    }
}
