namespace McpManager.Core.Models;

/// <summary>
/// Represents an MCP (Model Context Protocol) server that can be installed and managed.
/// </summary>
public class McpServer
{
    /// <summary>
    /// Unique identifier for the MCP server.
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
    /// Tags/categories for the server (e.g., "database", "api", "filesystem").
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Whether this server is currently installed locally.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Date and time when the server was installed.
    /// </summary>
    public DateTime? InstalledAt { get; set; }

    /// <summary>
    /// Configuration settings specific to this server.
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();
}
