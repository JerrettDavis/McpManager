using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for monitoring MCP server health and status.
/// </summary>
public class ServerMonitor : IServerMonitor
{
    private readonly Dictionary<string, ServerHealthStatus> _healthStatuses = new();

    public Task<bool> IsServerRunningAsync(string serverId)
    {
        if (_healthStatuses.TryGetValue(serverId, out var status))
        {
            return Task.FromResult(status.IsRunning);
        }
        return Task.FromResult(false);
    }

    public Task<ServerHealthStatus> GetServerHealthAsync(string serverId)
    {
        if (_healthStatuses.TryGetValue(serverId, out var status))
        {
            return Task.FromResult(status);
        }

        var newStatus = new ServerHealthStatus
        {
            ServerId = serverId,
            IsRunning = false,
            IsHealthy = false,
            LastChecked = DateTime.UtcNow
        };

        return Task.FromResult(newStatus);
    }

    public Task StartMonitoringAsync(string serverId)
    {
        if (!_healthStatuses.ContainsKey(serverId))
        {
            _healthStatuses[serverId] = new ServerHealthStatus
            {
                ServerId = serverId,
                IsRunning = true,
                IsHealthy = true,
                LastChecked = DateTime.UtcNow
            };
        }

        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(string serverId)
    {
        if (_healthStatuses.ContainsKey(serverId))
        {
            _healthStatuses[serverId].IsRunning = false;
            _healthStatuses[serverId].LastChecked = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }
}
