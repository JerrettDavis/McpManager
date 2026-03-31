namespace McpManager.Core.Models;

/// <summary>
/// Types of conflicts that can occur when the same MCP server is configured across multiple agents.
/// </summary>
public enum ConflictType
{
    /// <summary>
    /// Same server ID configured with different command/args versions across agents.
    /// </summary>
    VersionMismatch,

    /// <summary>
    /// Same server ID configured with different non-version configuration values across agents.
    /// </summary>
    ConfigMismatch,

    /// <summary>
    /// Same server appears under different config keys within a single agent.
    /// </summary>
    Duplicate
}

/// <summary>
/// Represents a detected conflict for an MCP server across one or more agents.
/// </summary>
public class ServerConflict
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ServerId { get; set; } = string.Empty;
    public ConflictType Type { get; set; }
    public List<AgentConflictEntry> Entries { get; set; } = [];
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Human-readable summary of the conflict for display in alerts and lists.
    /// </summary>
    public string Summary => Type switch
    {
        ConflictType.VersionMismatch =>
            $"Server '{ServerId}' has different versions across {Entries.Count} agent(s)",
        ConflictType.ConfigMismatch =>
            $"Server '{ServerId}' has different configurations across {Entries.Count} agent(s)",
        ConflictType.Duplicate =>
            $"Server '{ServerId}' has {Entries.Count} duplicate entries in {Entries.Select(e => e.AgentId).Distinct().Count()} agent(s)",
        _ => $"Conflict detected for server '{ServerId}'"
    };
}

/// <summary>
/// One side of a conflict — a specific agent's configuration of the conflicting server.
/// </summary>
public class AgentConflictEntry
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string ConfiguredServerKey { get; set; } = string.Empty;
    public Dictionary<string, string> RawConfig { get; set; } = new();
}
