namespace McpManager.Core.Models;

/// <summary>
/// Represents the installation state of an MCP server for a specific agent.
/// </summary>
public class ServerInstallation
{
    /// <summary>
    /// Unique identifier for this installation record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID of the MCP server.
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the agent this server is installed for.
    /// </summary>
    public string AgentId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this server is enabled for the agent.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Date and time when this installation was created.
    /// </summary>
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when this installation was last modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Agent-specific configuration for this server.
    /// </summary>
    public Dictionary<string, string> AgentSpecificConfig { get; set; } = new();
}
