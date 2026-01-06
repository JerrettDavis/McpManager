using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for agent-specific connectors.
/// Follows Open/Closed Principle - open for extension (new agent types), closed for modification.
/// Each agent type implements this interface to provide agent-specific behavior.
/// </summary>
public interface IAgentConnector
{
    /// <summary>
    /// Gets the type of agent this connector supports.
    /// </summary>
    AgentType AgentType { get; }

    /// <summary>
    /// Detects if this agent is installed on the system.
    /// </summary>
    Task<bool> IsAgentInstalledAsync();

    /// <summary>
    /// Gets the configuration file path for this agent.
    /// </summary>
    Task<string> GetConfigurationPathAsync();

    /// <summary>
    /// Reads the agent's configuration to get installed MCP servers.
    /// </summary>
    Task<IEnumerable<string>> GetConfiguredServerIdsAsync();

    /// <summary>
    /// Adds an MCP server to the agent's configuration.
    /// </summary>
    Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null);

    /// <summary>
    /// Removes an MCP server from the agent's configuration.
    /// </summary>
    Task<bool> RemoveServerFromAgentAsync(string serverId);

    /// <summary>
    /// Enables or disables a server for this agent.
    /// </summary>
    Task<bool> SetServerEnabledAsync(string serverId, bool enabled);
}
