using McpManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace McpManager.Application.Services;

/// <summary>
/// Service for watching configuration file changes across all AI agents.
/// Monitors both user-level and project-level configuration files.
/// </summary>
public class ConfigurationWatcher(IAgentManager agentManager, ILogger<ConfigurationWatcher>? logger = null) : IConfigurationWatcher, IDisposable
{
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new();
    private readonly object _lock = new();
    private bool _isWatching;

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    public async Task StartWatchingAsync()
    {
        if (_isWatching)
        {
            return;
        }

        lock (_lock)
        {
            if (_isWatching)
            {
                return;
            }
            _isWatching = true;
        }

        // Get all installed agents and their configuration paths
        var agents = await agentManager.DetectInstalledAgentsAsync();

        foreach (var agent in agents)
        {
            if (string.IsNullOrEmpty(agent.ConfigPath) || !File.Exists(agent.ConfigPath))
            {
                continue;
            }

            try
            {
                var directory = Path.GetDirectoryName(agent.ConfigPath);
                var fileName = Path.GetFileName(agent.ConfigPath);

                if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName))
                {
                    continue;
                }

                var watcher = new FileSystemWatcher(directory)
                {
                    Filter = fileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                watcher.Changed += (sender, e) => OnFileChanged(e.FullPath, agent.Id);
                watcher.Created += (sender, e) => OnFileChanged(e.FullPath, agent.Id);

                _watchers[agent.Id] = watcher;

                logger?.LogInformation("Started watching configuration file for agent {AgentId} at {Path}",
                    agent.Id, agent.ConfigPath);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to start watching configuration file for agent {AgentId} at {Path}",
                    agent.Id, agent.ConfigPath);
            }
        }

        // Also watch ~/.claude.json specifically (user-level config)
        var userConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude.json");

        if (File.Exists(userConfigPath))
        {
            try
            {
                var directory = Path.GetDirectoryName(userConfigPath);
                var fileName = Path.GetFileName(userConfigPath);

                if (!string.IsNullOrEmpty(directory) && !string.IsNullOrEmpty(fileName))
                {
                    var watcher = new FileSystemWatcher(directory)
                    {
                        Filter = fileName,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                        EnableRaisingEvents = true
                    };

                    watcher.Changed += (sender, e) => OnFileChanged(e.FullPath, "claudecode");
                    watcher.Created += (sender, e) => OnFileChanged(e.FullPath, "claudecode");

                    _watchers["claudecode-user"] = watcher;

                    logger?.LogInformation("Started watching user-level Claude configuration at {Path}", userConfigPath);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to start watching user-level Claude configuration at {Path}", userConfigPath);
            }
        }
    }

    public Task StopWatchingAsync()
    {
        lock (_lock)
        {
            if (!_isWatching)
            {
                return Task.CompletedTask;
            }
            _isWatching = false;
        }

        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();
        logger?.LogInformation("Stopped watching all configuration files");

        return Task.CompletedTask;
    }

    private void OnFileChanged(string path, string agentId)
    {
        try
        {
            // Debounce rapid file changes (some editors trigger multiple events)
            Task.Delay(100).ContinueWith(_ =>
            {
                logger?.LogInformation("Configuration file changed for agent {AgentId} at {Path}", agentId, path);

                ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs
                {
                    ConfigurationPath = path,
                    AgentId = agentId
                });
            });
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling configuration file change for agent {AgentId}", agentId);
        }
    }

    public void Dispose()
    {
        StopWatchingAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
