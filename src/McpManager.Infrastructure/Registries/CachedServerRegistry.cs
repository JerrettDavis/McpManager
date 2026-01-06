using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Caching wrapper for IServerRegistry that reads from local cache first.
/// Implements read-through caching pattern.
/// </summary>
public class CachedServerRegistry : ICachedServerRegistry
{
    private readonly IServerRegistry _innerRegistry;
    private readonly IRegistryCacheRepository _cacheRepository;
    private readonly TimeSpan _cacheMaxAge;

    public string Name => _innerRegistry.Name;

    public CachedServerRegistry(
        IServerRegistry innerRegistry, 
        IRegistryCacheRepository cacheRepository,
        TimeSpan? cacheMaxAge = null)
    {
        _innerRegistry = innerRegistry;
        _cacheRepository = cacheRepository;
        _cacheMaxAge = cacheMaxAge ?? TimeSpan.FromHours(1);
    }

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        // Try cache first
        var isCacheStale = await _cacheRepository.IsCacheStaleAsync(Name, _cacheMaxAge);
        
        if (!isCacheStale)
        {
            var cachedResults = await _cacheRepository.SearchAsync(query, maxResults);
            var cachedList = cachedResults.Where(r => r.RegistryName == Name).ToList();
            
            if (cachedList.Any())
            {
                return cachedList;
            }
        }

        // Fall back to remote registry
        return await _innerRegistry.SearchAsync(query, maxResults);
    }

    public async Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
    {
        // Try cache first
        var isCacheStale = await _cacheRepository.IsCacheStaleAsync(Name, _cacheMaxAge);
        
        if (!isCacheStale)
        {
            var cachedResults = await _cacheRepository.GetByRegistryAsync(Name);
            var cachedList = cachedResults.ToList();
            
            if (cachedList.Any())
            {
                return cachedList;
            }
        }

        // Fall back to remote registry
        return await _innerRegistry.GetAllServersAsync();
    }

    public async Task<McpServer?> GetServerDetailsAsync(string serverId)
    {
        // Try cache first
        var isCacheStale = await _cacheRepository.IsCacheStaleAsync(Name, _cacheMaxAge);
        
        if (!isCacheStale)
        {
            var cached = await _cacheRepository.GetByIdAsync(Name, serverId);
            if (cached != null)
            {
                return cached.Server;
            }
        }

        // Fall back to remote registry
        return await _innerRegistry.GetServerDetailsAsync(serverId);
    }
}
