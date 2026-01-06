using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Repository interface for caching and retrieving registry server data.
/// </summary>
public interface IRegistryCacheRepository
{
    /// <summary>
    /// Gets all cached servers from a specific registry.
    /// </summary>
    Task<IEnumerable<ServerSearchResult>> GetByRegistryAsync(string registryName);

    /// <summary>
    /// Gets all cached servers across all registries.
    /// </summary>
    Task<IEnumerable<ServerSearchResult>> GetAllAsync();

    /// <summary>
    /// Searches cached servers by query string.
    /// </summary>
    Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50);

    /// <summary>
    /// Gets a specific cached server by registry and server ID.
    /// </summary>
    Task<ServerSearchResult?> GetByIdAsync(string registryName, string serverId);

    /// <summary>
    /// Adds or updates multiple servers in the cache for a specific registry.
    /// </summary>
    Task<int> UpsertManyAsync(string registryName, IEnumerable<ServerSearchResult> servers);

    /// <summary>
    /// Gets the last refresh time for a registry.
    /// </summary>
    Task<DateTime?> GetLastRefreshTimeAsync(string registryName);

    /// <summary>
    /// Updates registry metadata after a refresh operation.
    /// </summary>
    Task UpdateRegistryMetadataAsync(string registryName, int serverCount, bool success, string? error = null);

    /// <summary>
    /// Gets all registries that need refresh based on their intervals.
    /// </summary>
    Task<IEnumerable<string>> GetRegistriesNeedingRefreshAsync();

    /// <summary>
    /// Checks if cache is stale for a given registry.
    /// </summary>
    Task<bool> IsCacheStaleAsync(string registryName, TimeSpan maxAge);
}
