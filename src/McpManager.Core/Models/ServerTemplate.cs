namespace McpManager.Core.Models;

/// <summary>
/// A pre-configured bundle of MCP servers that can be installed together.
/// </summary>
public class ServerTemplate
{
    /// <summary>
    /// Unique identifier for the template.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this template provides.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping templates (e.g., "Development", "Research").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Icon identifier for UI display.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Template version.
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Author of the template.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Servers included in this template.
    /// </summary>
    public List<TemplateServer> Servers { get; set; } = [];

    /// <summary>
    /// Environment variables required by this template's servers.
    /// Keys are variable names, values are descriptions.
    /// </summary>
    public Dictionary<string, string> RequiredEnvironmentVariables { get; set; } = [];
}

/// <summary>
/// A server entry within a template, with default configuration.
/// </summary>
public class TemplateServer
{
    /// <summary>
    /// The MCP server ID to install.
    /// </summary>
    public string ServerId { get; set; } = string.Empty;

    /// <summary>
    /// Display name for this server in the template context.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this server is required or optional in the template.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Default configuration to apply when installing this server.
    /// </summary>
    public Dictionary<string, object> DefaultConfig { get; set; } = [];
}
