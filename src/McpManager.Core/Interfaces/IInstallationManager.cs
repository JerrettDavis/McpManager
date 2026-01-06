using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for managing server installations across agents.
/// Follows Single Responsibility Principle - manages the relationship between servers and agents.
/// </summary>
public interface IInstallationManager
{
    /// <summary>
    /// Gets all server installations.
    /// </summary>
    Task<IEnumerable<ServerInstallation>> GetAllInstallationsAsync();

    /// <summary>
    /// Gets installations for a specific server.
    /// </summary>
    Task<IEnumerable<ServerInstallation>> GetInstallationsByServerIdAsync(string serverId);

    /// <summary>
    /// Gets installations for a specific agent.
    /// </summary>
    Task<IEnumerable<ServerInstallation>> GetInstallationsByAgentIdAsync(string agentId);

    /// <summary>
    /// Adds a server to an agent.
    /// </summary>
    Task<ServerInstallation> AddServerToAgentAsync(string serverId, string agentId, Dictionary<string, string>? config = null);

    /// <summary>
    /// Removes a server from an agent.
    /// </summary>
    Task<bool> RemoveServerFromAgentAsync(string serverId, string agentId);

    /// <summary>
    /// Toggles a server's enabled state for an agent.
    /// </summary>
    Task<bool> ToggleServerEnabledAsync(string serverId, string agentId);

    /// <summary>
    /// Updates agent-specific configuration for a server installation.
    /// </summary>
    Task<bool> UpdateInstallationConfigAsync(string installationId, Dictionary<string, string> config);
}
