using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Caching wrapper for IServerRegistry that reads from local cache first.
/// Implements read-through caching pattern with cache population on miss.
/// </summary>
public class CachedServerRegistry(
    IServerRegistry innerRegistry,
    IServiceProvider serviceProvider,
    TimeSpan? cacheMaxAge = null
)
    : ICachedServerRegistry
{
    private readonly TimeSpan _cacheMaxAge = cacheMaxAge ?? TimeSpan.FromHours(1);

    public string Name => innerRegistry.Name;

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        // Try cache first
        using var scope = serviceProvider.CreateScope();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IRegistryCacheRepository>();
        
        var isCacheStale = await cacheRepository.IsCacheStaleAsync(Name, _cacheMaxAge);
        
        if (!isCacheStale)
        {
            var cachedResults = await cacheRepository.SearchAsync(query, maxResults);
            var cachedList = cachedResults.Where(r => r.RegistryName == Name).ToList();
            
            if (cachedList.Any())
            {
                return cachedList;
            }
        }

        // Fall back to remote registry and cache results
        var remoteResults = await innerRegistry.SearchAsync(query, maxResults);
        var resultsList = remoteResults.ToList();
        
        if (resultsList.Any())
        {
            // Try to cache the results for future use
            // If database doesn't exist, silently fail (cache will be populated by background worker)
            try
            {
                await cacheRepository.UpsertManyAsync(Name, resultsList);
                await cacheRepository.UpdateRegistryMetadataAsync(Name, resultsList.Count, true);
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Database doesn't exist yet - skip caching
                // Background worker will populate cache when database is initialized
            }
        }
        
        return resultsList;
    }

    public async Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
    {
        // Try cache first
        using var scope = serviceProvider.CreateScope();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IRegistryCacheRepository>();
        
        var isCacheStale = await cacheRepository.IsCacheStaleAsync(Name, _cacheMaxAge);
        
        if (!isCacheStale)
        {
            var cachedResults = await cacheRepository.GetByRegistryAsync(Name);
            var cachedList = cachedResults.ToList();
            
            if (cachedList.Any())
            {
                return cachedList;
            }
        }

        // Fall back to remote registry and cache results
        var remoteResults = await innerRegistry.GetAllServersAsync();
        var resultsList = remoteResults.ToList();
        
        if (resultsList.Any())
        {
            // Try to cache the results for future use
            // If database doesn't exist, silently fail (cache will be populated by background worker)
            try
            {
                await cacheRepository.UpsertManyAsync(Name, resultsList);
                await cacheRepository.UpdateRegistryMetadataAsync(Name, resultsList.Count, true);
            }
            catch (Microsoft.Data.Sqlite.SqliteException)
            {
                // Database doesn't exist yet - skip caching
                // Background worker will populate cache when database is initialized
            }
        }
        
        return resultsList;
    }

    public async Task<McpServer?> GetServerDetailsAsync(string serverId)
    {
        // Try cache first
        using var scope = serviceProvider.CreateScope();
        var cacheRepository = scope.ServiceProvider.GetRequiredService<IRegistryCacheRepository>();
        
        var isCacheStale = await cacheRepository.IsCacheStaleAsync(Name, _cacheMaxAge);
        
        if (!isCacheStale)
        {
            var cached = await cacheRepository.GetByIdAsync(Name, serverId);
            if (cached != null)
            {
                return cached.Server;
            }
        }

        // Fall back to remote registry
        // Note: Single server details are not cached to avoid partial cache state
        return await innerRegistry.GetServerDetailsAsync(serverId);
    }
}
