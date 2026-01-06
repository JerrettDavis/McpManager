namespace McpManager.Core.Models;

/// <summary>
/// Represents a search result from an MCP server registry.
/// </summary>
public class ServerSearchResult
{
    /// <summary>
    /// The MCP server information.
    /// </summary>
    public McpServer Server { get; set; } = new();

    /// <summary>
    /// Relevance score for this search result (0-1).
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Name of the registry this result came from.
    /// </summary>
    public string RegistryName { get; set; } = string.Empty;

    /// <summary>
    /// Download count or popularity metric.
    /// </summary>
    public long DownloadCount { get; set; }

    /// <summary>
    /// Date this server was last updated in the registry.
    /// </summary>
    public DateTime? LastUpdated { get; set; }
}
