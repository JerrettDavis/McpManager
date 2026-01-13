using McpManager.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that monitors configuration file changes.
/// Automatically starts the configuration watcher when the application starts.
/// </summary>
public class ConfigurationWatcherWorker : BackgroundService
{
    private readonly IConfigurationWatcher _configurationWatcher;
    private readonly ILogger<ConfigurationWatcherWorker> _logger;

    public ConfigurationWatcherWorker(
        IConfigurationWatcher configurationWatcher,
        ILogger<ConfigurationWatcherWorker> logger)
    {
        _configurationWatcher = configurationWatcher;
        _logger = logger;

        // Subscribe to configuration changes
        _configurationWatcher.ConfigurationChanged += OnConfigurationChanged;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Configuration watcher worker starting");

        // Start watching configuration files
        await _configurationWatcher.StartWatchingAsync();

        // Keep the worker running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when the application is shutting down
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Configuration watcher worker stopping");
        await _configurationWatcher.StopWatchingAsync();
        await base.StopAsync(cancellationToken);
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        _logger.LogInformation(
            "Configuration changed for agent {AgentId} at {Path}. Agents should reload their configuration.",
            e.AgentId,
            e.ConfigurationPath);

        // The UI will automatically refresh when it detects this change
        // through its reactive components
    }
}
