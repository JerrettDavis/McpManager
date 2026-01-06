using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for detecting and managing AI agents.
/// Follows Single Responsibility Principle - only handles agent management.
/// </summary>
public interface IAgentManager
{
    /// <summary>
    /// Detects all installed AI agents on the system.
    /// </summary>
    Task<IEnumerable<Agent>> DetectInstalledAgentsAsync();

    /// <summary>
    /// Gets a specific agent by ID.
    /// </summary>
    Task<Agent?> GetAgentByIdAsync(string agentId);

    /// <summary>
    /// Gets all MCP servers configured for a specific agent.
    /// </summary>
    Task<IEnumerable<string>> GetAgentServerIdsAsync(string agentId);
}
