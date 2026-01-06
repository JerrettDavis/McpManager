using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Repository interface for persisting and retrieving installed MCP servers.
/// </summary>
public interface IServerRepository
{
    /// <summary>
    /// Gets all installed servers.
    /// </summary>
    Task<IEnumerable<McpServer>> GetAllAsync();

    /// <summary>
    /// Gets a specific server by ID.
    /// </summary>
    Task<McpServer?> GetByIdAsync(string serverId);

    /// <summary>
    /// Adds a new server to the repository.
    /// </summary>
    Task<bool> AddAsync(McpServer server);

    /// <summary>
    /// Updates an existing server in the repository.
    /// </summary>
    Task<bool> UpdateAsync(McpServer server);

    /// <summary>
    /// Removes a server from the repository.
    /// </summary>
    Task<bool> DeleteAsync(string serverId);

    /// <summary>
    /// Checks if a server exists in the repository.
    /// </summary>
    Task<bool> ExistsAsync(string serverId);
}
