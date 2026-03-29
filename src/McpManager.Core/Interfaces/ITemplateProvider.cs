using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Provides server templates for one-click installation of pre-configured server bundles.
/// </summary>
public interface ITemplateProvider
{
    /// <summary>
    /// Gets all available server templates.
    /// </summary>
    Task<IEnumerable<ServerTemplate>> GetTemplatesAsync();

    /// <summary>
    /// Gets a specific template by its ID.
    /// </summary>
    /// <param name="templateId">The template identifier.</param>
    /// <returns>The template, or null if not found.</returns>
    Task<ServerTemplate?> GetTemplateByIdAsync(string templateId);

    /// <summary>
    /// Installs all servers in a template to the specified agents.
    /// </summary>
    /// <param name="templateId">The template to install.</param>
    /// <param name="agentIds">The agents to install the template servers to.</param>
    Task<TemplateInstallResult> InstallTemplateAsync(string templateId, IEnumerable<string> agentIds);
}

/// <summary>
/// Result of installing a template's servers.
/// </summary>
public class TemplateInstallResult
{
    public bool Success { get; set; }
    public List<TemplateServerInstallResult> ServerResults { get; set; } = [];
    public string? Error { get; set; }
}

/// <summary>
/// Result of installing an individual server from a template.
/// </summary>
public class TemplateServerInstallResult
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public bool Installed { get; set; }
    public string? Error { get; set; }
}
