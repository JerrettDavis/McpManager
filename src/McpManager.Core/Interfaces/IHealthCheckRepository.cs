using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

public interface IHealthCheckRepository
{
    Task AddAsync(HealthCheckResult result);
    Task<IEnumerable<HealthCheckResult>> GetRecentAsync(string serverId, int count = 20);
    Task<HealthCheckResult?> GetLatestAsync(string serverId);
    Task<IEnumerable<HealthCheckResult>> GetAllRecentAsync(int countPerServer = 20);
    Task<int> DeleteOlderThanAsync(DateTime cutoff);
    Task<(int total, int failed)> GetErrorCountAsync(string serverId, int lastNChecks = 20);
}
