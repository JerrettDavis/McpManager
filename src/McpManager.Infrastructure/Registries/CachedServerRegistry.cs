using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Caching wrapper for IServerRegistry that reads from local cache first.
/// Implements read-through caching pattern.
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

        // Fall back to remote registry
        return await innerRegistry.SearchAsync(query, maxResults);
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

        // Fall back to remote registry
        return await innerRegistry.GetAllServersAsync();
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
        return await innerRegistry.GetServerDetailsAsync(serverId);
    }
}
