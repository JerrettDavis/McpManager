using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Detects version and configuration conflicts for MCP servers across agents.
/// </summary>
public interface IConflictDetector
{
    /// <summary>
    /// Scans all agents and installations to detect conflicts.
    /// </summary>
    Task<IReadOnlyList<ServerConflict>> DetectAllConflictsAsync();

    /// <summary>
    /// Checks a specific server for conflicts across agents.
    /// </summary>
    Task<ServerConflict?> DetectConflictForServerAsync(string serverId);
}
