using System.Collections.Concurrent;
using System.Diagnostics;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

public class ServerMonitor(
    IHealthCheckRepository healthCheckRepository,
    IServerManager serverManager) : IServerMonitor
{
    private readonly ConcurrentDictionary<string, bool> _monitoredServers = new();

    public Task<bool> IsServerRunningAsync(string serverId)
        => Task.FromResult(_monitoredServers.ContainsKey(serverId));

    public async Task<ServerHealthStatus> GetServerHealthAsync(string serverId)
    {
        var summary = await GetHealthSummaryAsync(serverId);
        return new ServerHealthStatus
        {
            ServerId = serverId,
            IsRunning = _monitoredServers.ContainsKey(serverId),
            IsHealthy = summary?.Status == HealthStatus.Healthy,
            LastChecked = summary?.LastCheckedAt ?? DateTime.MinValue,
            ErrorMessage = summary?.LastErrorMessage,
            Metrics = new Dictionary<string, string>
            {
                ["response_time_ms"] = summary?.LastResponseTimeMs?.ToString() ?? "N/A",
                ["error_rate"] = $"{summary?.ErrorRatePercent:F1}%",
                ["status"] = summary?.Status.ToString() ?? "Unknown"
            }
        };
    }

    public Task StartMonitoringAsync(string serverId)
    {
        _monitoredServers.TryAdd(serverId, true);
        return Task.CompletedTask;
    }

    public Task StopMonitoringAsync(string serverId)
    {
        _monitoredServers.TryRemove(serverId, out _);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<ServerHealthSummary>> GetAllHealthSummariesAsync()
    {
        var servers = await serverManager.GetInstalledServersAsync();
        var summaries = new List<ServerHealthSummary>();
        foreach (var server in servers)
        {
            var summary = await BuildSummaryAsync(server.Id, server.Name);
            summaries.Add(summary);
        }
        return summaries;
    }

    public async Task<ServerHealthSummary?> GetHealthSummaryAsync(string serverId)
    {
        var server = await serverManager.GetServerByIdAsync(serverId);
        return await BuildSummaryAsync(serverId, server?.Name ?? serverId);
    }

    public async Task<HealthCheckResult> PerformHealthCheckAsync(string serverId)
    {
        var sw = Stopwatch.StartNew();
        bool isHealthy;
        string? errorMessage = null;

        try
        {
            // MVP: check if server is in the monitored set
            // Future: custom health check endpoints, process probing
            isHealthy = _monitoredServers.ContainsKey(serverId);
        }
        catch (Exception ex)
        {
            isHealthy = false;
            errorMessage = ex.Message;
        }

        sw.Stop();
        var result = new HealthCheckResult
        {
            ServerId = serverId,
            CheckedAt = DateTime.UtcNow,
            IsHealthy = isHealthy,
            ResponseTimeMs = sw.ElapsedMilliseconds,
            ErrorMessage = errorMessage
        };

        await healthCheckRepository.AddAsync(result);
        return result;
    }

    public IReadOnlySet<string> GetMonitoredServerIds()
        => _monitoredServers.Keys.ToHashSet();

    private async Task<ServerHealthSummary> BuildSummaryAsync(string serverId, string serverName)
    {
        var latest = await healthCheckRepository.GetLatestAsync(serverId);
        var (total, failed) = await healthCheckRepository.GetErrorCountAsync(serverId, 20);
        var recentChecks = (await healthCheckRepository.GetRecentAsync(serverId, 20)).ToList();

        var errorRate = total > 0 ? (double)failed / total * 100 : 0;
        var hasChecks = total > 0;

        return new ServerHealthSummary
        {
            ServerId = serverId,
            ServerName = serverName,
            Status = ServerHealthSummary.ComputeStatus(errorRate, latest?.ResponseTimeMs, hasChecks),
            LastCheckedAt = latest?.CheckedAt,
            LastResponseTimeMs = latest?.ResponseTimeMs,
            ErrorRatePercent = errorRate,
            TotalChecks = total,
            FailedChecks = failed,
            LastErrorMessage = latest?.ErrorMessage,
            RecentChecks = recentChecks
        };
    }
}
