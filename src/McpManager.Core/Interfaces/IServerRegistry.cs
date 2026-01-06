using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for searching MCP servers across registries.
/// Follows Single Responsibility Principle - only handles search functionality.
/// </summary>
public interface IServerRegistry
{
    /// <summary>
    /// Gets the name of this registry.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Searches for MCP servers matching the query.
    /// </summary>
    Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50);

    /// <summary>
    /// Gets all available servers from this registry.
    /// </summary>
    Task<IEnumerable<ServerSearchResult>> GetAllServersAsync();

    /// <summary>
    /// Gets detailed information about a specific server.
    /// </summary>
    Task<McpServer?> GetServerDetailsAsync(string serverId);
}
