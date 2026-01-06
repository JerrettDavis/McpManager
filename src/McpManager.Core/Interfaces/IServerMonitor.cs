using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for monitoring MCP server health and status.
/// Follows Single Responsibility Principle - only handles monitoring.
/// </summary>
public interface IServerMonitor
{
    /// <summary>
    /// Checks if a server is running.
    /// </summary>
    Task<bool> IsServerRunningAsync(string serverId);

    /// <summary>
    /// Gets health status of a server.
    /// </summary>
    Task<ServerHealthStatus> GetServerHealthAsync(string serverId);

    /// <summary>
    /// Starts monitoring a server.
    /// </summary>
    Task StartMonitoringAsync(string serverId);

    /// <summary>
    /// Stops monitoring a server.
    /// </summary>
    Task StopMonitoringAsync(string serverId);
}

/// <summary>
/// Represents the health status of an MCP server.
/// </summary>
public class ServerHealthStatus
{
    public string ServerId { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metrics { get; set; } = new();
}
