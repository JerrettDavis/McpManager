using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for browsing and searching MCP servers from the database cache.
/// Provides efficient querying without hitting external registries.
/// </summary>
public interface IServerBrowseService
{
    /// <summary>
    /// Search servers in the database cache with optional filters.
    /// </summary>
    Task<ServerBrowseResult> SearchServersAsync(
        string? searchQuery = null,
        string? registryFilter = null,
        string? categoryFilter = null,
        string sortBy = "downloads",
        int page = 1,
        int pageSize = 12);

    /// <summary>
    /// Get all unique categories/tags from cached servers.
    /// </summary>
    Task<IEnumerable<string>> GetAvailableCategoriesAsync();

    /// <summary>
    /// Get list of registries with server counts.
    /// </summary>
    Task<IEnumerable<RegistryInfo>> GetRegistriesAsync();
}

/// <summary>
/// Result of a server browse/search operation.
/// </summary>
public class ServerBrowseResult
{
    public List<ServerSearchResult> Servers { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Information about a registry.
/// </summary>
public class RegistryInfo
{
    public required string Name { get; set; }
    public int ServerCount { get; set; }
    public DateTime? LastRefresh { get; set; }
}

/// <summary>
/// Implementation of server browse service using the database cache.
/// </summary>
public class ServerBrowseService(IRegistryCacheRepository cacheRepository) : IServerBrowseService
{
    public async Task<ServerBrowseResult> SearchServersAsync(
        string? searchQuery = null,
        string? registryFilter = null,
        string? categoryFilter = null,
        string sortBy = "downloads",
        int page = 1,
        int pageSize = 12)
    {
        // Get base results from cache (all or by registry)
        IEnumerable<ServerSearchResult> results;
        
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Use database search with LIKE query
            results = await cacheRepository.SearchAsync(searchQuery, maxResults: 1000);
            
            // Filter by registry if specified
            if (!string.IsNullOrWhiteSpace(registryFilter))
            {
                results = results.Where(r => r.RegistryName == registryFilter);
            }
        }
        else if (!string.IsNullOrWhiteSpace(registryFilter))
        {
            // Get all from specific registry
            results = await cacheRepository.GetByRegistryAsync(registryFilter);
        }
        else
        {
            // Get all servers
            results = await cacheRepository.GetAllAsync();
        }

        // Apply category filter in memory (tags are stored as JSON)
        if (!string.IsNullOrWhiteSpace(categoryFilter))
        {
            results = results.Where(r => r.Server.Tags.Contains(categoryFilter, StringComparer.OrdinalIgnoreCase));
        }

        // Apply sorting
        results = sortBy.ToLowerInvariant() switch
        {
            "downloads" => results.OrderByDescending(r => r.DownloadCount),
            "recent" => results.OrderByDescending(r => r.LastUpdated ?? DateTime.MinValue),
            "name" => results.OrderBy(r => r.Server.Name, StringComparer.OrdinalIgnoreCase),
            "score" => results.OrderByDescending(r => r.Score),
            _ => results.OrderByDescending(r => r.DownloadCount)
        };

        var resultList = results.ToList();
        var totalCount = resultList.Count;

        // Apply pagination
        var pagedResults = resultList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new ServerBrowseResult
        {
            Servers = pagedResults,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<string>> GetAvailableCategoriesAsync()
    {
        var allServers = await cacheRepository.GetAllAsync();
        
        var categories = allServers
            .SelectMany(r => r.Server.Tags)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return categories;
    }

    public async Task<IEnumerable<RegistryInfo>> GetRegistriesAsync()
    {
        var allServers = await cacheRepository.GetAllAsync();
        
        var registries = allServers
            .GroupBy(r => r.RegistryName)
            .Select(g => new RegistryInfo
            {
                Name = g.Key,
                ServerCount = g.Count(),
                LastRefresh = g.Max(s => s.LastUpdated)
            })
            .OrderBy(r => r.Name)
            .ToList();

        return registries;
    }
}
