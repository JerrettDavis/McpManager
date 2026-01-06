namespace McpManager.Infrastructure.Persistence.Entities;

/// <summary>
/// Entity representing an installed MCP server in the database.
/// </summary>
public class InstalledServerEntity
{
    /// <summary>
    /// Unique identifier for the server.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the MCP server.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the server's functionality.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current version of the server.
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
    /// Date and time when the server was installed.
    /// </summary>
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date and time when the server record was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Configuration settings specific to this server, stored as JSON.
    /// </summary>
    public string ConfigurationJson { get; set; } = "{}";

    /// <summary>
    /// Registry source where this server originated from.
    /// </summary>
    public string? RegistrySource { get; set; }

    /// <summary>
    /// Installation location/path if applicable.
    /// </summary>
    public string? InstallLocation { get; set; }
}
