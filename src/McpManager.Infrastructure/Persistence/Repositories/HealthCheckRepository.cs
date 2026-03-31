using Microsoft.EntityFrameworkCore;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using McpManager.Infrastructure.Persistence.Entities;

namespace McpManager.Infrastructure.Persistence.Repositories;

public class HealthCheckRepository(McpManagerDbContext context) : IHealthCheckRepository
{
    public async Task AddAsync(HealthCheckResult result)
    {
        var entity = new HealthCheckEntity
        {
            Id = result.Id,
            ServerId = result.ServerId,
            CheckedAt = result.CheckedAt,
            IsHealthy = result.IsHealthy,
            ResponseTimeMs = result.ResponseTimeMs,
            ErrorMessage = result.ErrorMessage
        };
        context.HealthChecks.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<HealthCheckResult>> GetRecentAsync(string serverId, int count = 20)
    {
        var entities = await context.HealthChecks
            .Where(e => e.ServerId == serverId)
            .OrderByDescending(e => e.CheckedAt)
            .Take(count)
            .ToListAsync();
        return entities.Select(MapToModel);
    }

    public async Task<HealthCheckResult?> GetLatestAsync(string serverId)
    {
        var entity = await context.HealthChecks
            .Where(e => e.ServerId == serverId)
            .OrderByDescending(e => e.CheckedAt)
            .FirstOrDefaultAsync();
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<IEnumerable<HealthCheckResult>> GetAllRecentAsync(int countPerServer = 20)
    {
        var serverIds = await context.HealthChecks
            .Select(e => e.ServerId)
            .Distinct()
            .ToListAsync();

        var results = new List<HealthCheckResult>();
        foreach (var serverId in serverIds)
        {
            var recent = await GetRecentAsync(serverId, countPerServer);
            results.AddRange(recent);
        }
        return results;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime cutoff)
    {
        var old = await context.HealthChecks
            .Where(e => e.CheckedAt < cutoff)
            .ToListAsync();
        context.HealthChecks.RemoveRange(old);
        await context.SaveChangesAsync();
        return old.Count;
    }

    public async Task<(int total, int failed)> GetErrorCountAsync(string serverId, int lastNChecks = 20)
    {
        var recent = await context.HealthChecks
            .Where(e => e.ServerId == serverId)
            .OrderByDescending(e => e.CheckedAt)
            .Take(lastNChecks)
            .ToListAsync();
        return (recent.Count, recent.Count(e => !e.IsHealthy));
    }

    private static HealthCheckResult MapToModel(HealthCheckEntity entity) => new()
    {
        Id = entity.Id,
        ServerId = entity.ServerId,
        CheckedAt = entity.CheckedAt,
        IsHealthy = entity.IsHealthy,
        ResponseTimeMs = entity.ResponseTimeMs,
        ErrorMessage = entity.ErrorMessage
    };
}
