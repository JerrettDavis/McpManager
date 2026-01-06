using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Interface for managing MCP server configurations.
/// Handles configuration comparison, propagation, and validation.
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Compares two configuration dictionaries to determine if they are equal.
    /// </summary>
    /// <param name="config1">First configuration to compare</param>
    /// <param name="config2">Second configuration to compare</param>
    /// <returns>True if configurations are equal, false otherwise</returns>
    bool AreConfigurationsEqual(Dictionary<string, string> config1, Dictionary<string, string> config2);

    /// <summary>
    /// Gets the effective configuration for a server installation.
    /// If agent-specific config is not set, returns the global server configuration.
    /// </summary>
    /// <param name="server">The MCP server</param>
    /// <param name="installation">The server installation (can be null)</param>
    /// <returns>Effective configuration dictionary</returns>
    Dictionary<string, string> GetEffectiveConfiguration(McpServer server, ServerInstallation? installation);

    /// <summary>
    /// Determines if an agent's configuration matches the global server configuration.
    /// </summary>
    /// <param name="server">The MCP server</param>
    /// <param name="installation">The server installation</param>
    /// <returns>True if configurations match, false if they differ</returns>
    bool DoesAgentConfigMatchGlobal(McpServer server, ServerInstallation installation);

    /// <summary>
    /// Propagates a global configuration update to agent installations that match the old global config.
    /// Only updates installations where agent config exactly matches the previous global config.
    /// </summary>
    /// <param name="serverId">The server ID</param>
    /// <param name="oldGlobalConfig">The previous global configuration</param>
    /// <param name="newGlobalConfig">The new global configuration</param>
    /// <returns>List of installation IDs that were updated</returns>
    Task<IEnumerable<string>> PropagateConfigurationUpdateAsync(
        string serverId,
        Dictionary<string, string> oldGlobalConfig,
        Dictionary<string, string> newGlobalConfig);

    /// <summary>
    /// Validates a configuration dictionary.
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    /// <returns>Validation result with any error messages</returns>
    ConfigurationValidationResult ValidateConfiguration(Dictionary<string, string> config);

    /// <summary>
    /// Serializes a configuration dictionary to JSON.
    /// </summary>
    /// <param name="config">The configuration to serialize</param>
    /// <returns>JSON string representation</returns>
    string SerializeConfiguration(Dictionary<string, string> config);

    /// <summary>
    /// Deserializes a JSON string to a configuration dictionary.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>Configuration dictionary, or null if invalid</returns>
    Dictionary<string, string>? DeserializeConfiguration(string json);
}

/// <summary>
/// Result of configuration validation.
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// Whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation error messages.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
