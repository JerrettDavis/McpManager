using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for managing MCP servers (CRUD operations).
/// Follows Single Responsibility Principle - only handles server management.
/// </summary>
public interface IServerManager
{
    /// <summary>
    /// Gets all locally installed MCP servers.
    /// </summary>
    Task<IEnumerable<McpServer>> GetInstalledServersAsync();

    /// <summary>
    /// Gets a specific server by ID.
    /// </summary>
    Task<McpServer?> GetServerByIdAsync(string serverId);

    /// <summary>
    /// Installs an MCP server locally.
    /// </summary>
    Task<bool> InstallServerAsync(McpServer server);

    /// <summary>
    /// Uninstalls an MCP server.
    /// </summary>
    Task<bool> UninstallServerAsync(string serverId);

    /// <summary>
    /// Updates server configuration.
    /// </summary>
    Task<bool> UpdateServerConfigurationAsync(string serverId, Dictionary<string, string> configuration);
}
