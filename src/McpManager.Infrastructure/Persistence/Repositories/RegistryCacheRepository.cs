using Microsoft.EntityFrameworkCore;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using McpManager.Infrastructure.Persistence.Entities;
using System.Text.Json;

namespace McpManager.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for caching registry server data using EF Core.
/// </summary>
public class RegistryCacheRepository : IRegistryCacheRepository
{
    private readonly McpManagerDbContext _context;

    public RegistryCacheRepository(McpManagerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ServerSearchResult>> GetByRegistryAsync(string registryName)
    {
        var entities = await _context.CachedRegistryServers
            .Where(s => s.RegistryName == registryName)
            .ToListAsync();

        return entities.Select(MapToModel);
    }

    public async Task<IEnumerable<ServerSearchResult>> GetAllAsync()
    {
        var entities = await _context.CachedRegistryServers.ToListAsync();
        return entities.Select(MapToModel);
    }

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        var lowerQuery = query.ToLowerInvariant();

        var entities = await _context.CachedRegistryServers
            .Where(s => EF.Functions.Like(s.Name, $"%{lowerQuery}%") ||
                       EF.Functions.Like(s.Description, $"%{lowerQuery}%") ||
                       EF.Functions.Like(s.TagsJson, $"%{lowerQuery}%"))
            .OrderByDescending(s => s.Score)
            .Take(maxResults)
            .ToListAsync();

        return entities.Select(MapToModel);
    }

    public async Task<ServerSearchResult?> GetByIdAsync(string registryName, string serverId)
    {
        var id = GenerateId(registryName, serverId);
        var entity = await _context.CachedRegistryServers.FindAsync(id);
        return entity == null ? null : MapToModel(entity);
    }

    public async Task<int> UpsertManyAsync(string registryName, IEnumerable<ServerSearchResult> servers)
    {
        var serversList = servers.ToList();
        var count = 0;

        foreach (var server in serversList)
        {
            var id = GenerateId(registryName, server.Server.Id);
            var existingEntity = await _context.CachedRegistryServers.FindAsync(id);

            if (existingEntity != null)
            {
                // Update existing
                UpdateEntity(existingEntity, server);
            }
            else
            {
                // Add new
                var entity = MapToEntity(registryName, server);
                _context.CachedRegistryServers.Add(entity);
            }
            count++;
        }

        await _context.SaveChangesAsync();
        return count;
    }

    public async Task<DateTime?> GetLastRefreshTimeAsync(string registryName)
    {
        var metadata = await _context.RegistryMetadata.FindAsync(registryName);
        return metadata?.LastRefreshAt;
    }

    public async Task UpdateRegistryMetadataAsync(string registryName, int serverCount, bool success, string? error = null)
    {
        var metadata = await _context.RegistryMetadata.FindAsync(registryName);

        if (metadata == null)
        {
            metadata = new RegistryMetadataEntity
            {
                RegistryName = registryName,
                LastRefreshAt = DateTime.UtcNow,
                TotalServersCached = serverCount,
                LastRefreshSuccessful = success,
                LastRefreshError = error,
                NextRefreshAt = DateTime.UtcNow.AddMinutes(60)
            };
            _context.RegistryMetadata.Add(metadata);
        }
        else
        {
            metadata.LastRefreshAt = DateTime.UtcNow;
            metadata.TotalServersCached = serverCount;
            metadata.LastRefreshSuccessful = success;
            metadata.LastRefreshError = error;
            metadata.NextRefreshAt = DateTime.UtcNow.AddMinutes(metadata.RefreshIntervalMinutes);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> GetRegistriesNeedingRefreshAsync()
    {
        var now = DateTime.UtcNow;
        var registries = await _context.RegistryMetadata
            .Where(m => m.NextRefreshAt == null || m.NextRefreshAt <= now)
            .Select(m => m.RegistryName)
            .ToListAsync();

        return registries;
    }

    public async Task<bool> IsCacheStaleAsync(string registryName, TimeSpan maxAge)
    {
        var metadata = await _context.RegistryMetadata.FindAsync(registryName);
        
        if (metadata == null)
        {
            return true; // No cache exists, it's stale
        }

        var age = DateTime.UtcNow - metadata.LastRefreshAt;
        return age > maxAge;
    }

    private static string GenerateId(string registryName, string serverId)
    {
        return $"{registryName}::{serverId}";
    }

    private static ServerSearchResult MapToModel(CachedRegistryServerEntity entity)
    {
        return new ServerSearchResult
        {
            Server = new McpServer
            {
                Id = entity.ServerId,
                Name = entity.Name,
                Description = entity.Description,
                Version = entity.Version,
                Author = entity.Author,
                RepositoryUrl = entity.RepositoryUrl,
                InstallCommand = entity.InstallCommand,
                Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson) ?? new List<string>(),
                IsInstalled = false
            },
            Score = entity.Score,
            RegistryName = entity.RegistryName,
            DownloadCount = entity.DownloadCount,
            LastUpdated = entity.LastUpdatedInRegistry
        };
    }

    private static CachedRegistryServerEntity MapToEntity(string registryName, ServerSearchResult result)
    {
        return new CachedRegistryServerEntity
        {
            Id = GenerateId(registryName, result.Server.Id),
            RegistryName = registryName,
            ServerId = result.Server.Id,
            Name = result.Server.Name,
            Description = result.Server.Description,
            Version = result.Server.Version,
            Author = result.Server.Author,
            RepositoryUrl = result.Server.RepositoryUrl,
            InstallCommand = result.Server.InstallCommand,
            TagsJson = JsonSerializer.Serialize(result.Server.Tags),
            DownloadCount = result.DownloadCount,
            Score = result.Score,
            LastUpdatedInRegistry = result.LastUpdated,
            FetchedAt = DateTime.UtcNow
        };
    }

    private static void UpdateEntity(CachedRegistryServerEntity entity, ServerSearchResult result)
    {
        entity.Name = result.Server.Name;
        entity.Description = result.Server.Description;
        entity.Version = result.Server.Version;
        entity.Author = result.Server.Author;
        entity.RepositoryUrl = result.Server.RepositoryUrl;
        entity.InstallCommand = result.Server.InstallCommand;
        entity.TagsJson = JsonSerializer.Serialize(result.Server.Tags);
        entity.DownloadCount = result.DownloadCount;
        entity.Score = result.Score;
        entity.LastUpdatedInRegistry = result.LastUpdated;
        entity.FetchedAt = DateTime.UtcNow;
    }
}
