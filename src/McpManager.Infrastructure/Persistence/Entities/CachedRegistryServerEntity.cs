namespace McpManager.Infrastructure.Persistence.Entities;

/// <summary>
/// Entity representing a cached MCP server entry from a registry.
/// </summary>
public class CachedRegistryServerEntity
{
    /// <summary>
    /// Unique identifier for the cached entry (composite of RegistryName and ServerId).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name of the registry this entry came from.
    /// </summary>
    public string RegistryName { get; set; } = string.Empty;

    /// <summary>
    /// Server identifier in the registry.
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the MCP server.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the server's functionality.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Version of the server.
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Author or organization that maintains the server.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// URL to the server's repository or homepage.
    /// </summary>
    public string RepositoryUrl { get; set; } = string.Empty;

    /// <summary>
    /// Installation command or package identifier.
    /// </summary>
    public string InstallCommand { get; set; } = string.Empty;

    /// <summary>
    /// Tags/categories for the server, stored as JSON.
    /// </summary>
    public string TagsJson { get; set; } = "[]";

    /// <summary>
    /// Download count or popularity metric.
    /// </summary>
    public long DownloadCount { get; set; }

    /// <summary>
    /// Relevance score for search results.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Date this server was last updated in the registry.
    /// </summary>
    public DateTime? LastUpdatedInRegistry { get; set; }

    /// <summary>
    /// Date and time when this cache entry was fetched.
    /// </summary>
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata payload from the registry, stored as JSON.
    /// </summary>
    public string? MetadataJson { get; set; }
}
