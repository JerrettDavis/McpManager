namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for watching configuration file changes.
/// Notifies when agent configuration files are modified externally.
/// </summary>
public interface IConfigurationWatcher
{
    /// <summary>
    /// Event raised when a configuration file changes.
    /// </summary>
    event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Starts watching the specified configuration paths.
    /// </summary>
    Task StartWatchingAsync();

    /// <summary>
    /// Stops watching all configuration paths.
    /// </summary>
    Task StopWatchingAsync();
}

/// <summary>
/// Event arguments for configuration change events.
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs
{
    public string ConfigurationPath { get; init; } = string.Empty;
    public string AgentId { get; init; } = string.Empty;
}
