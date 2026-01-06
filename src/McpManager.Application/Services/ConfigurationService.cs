using System.Text.Json;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing MCP server configurations.
/// Handles configuration comparison, propagation, and validation.
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IInstallationManager _installationManager;

    public ConfigurationService(IInstallationManager installationManager)
    {
        _installationManager = installationManager;
    }

    public bool AreConfigurationsEqual(Dictionary<string, string> config1, Dictionary<string, string> config2)
    {
        // Handle null cases
        if (config1 == null && config2 == null) return true;
        if (config1 == null || config2 == null) return false;

        // Check if counts are equal
        if (config1.Count != config2.Count) return false;

        // Check if all keys and values match
        foreach (var kvp in config1)
        {
            if (!config2.TryGetValue(kvp.Key, out var value) || value != kvp.Value)
            {
                return false;
            }
        }

        return true;
    }

    public Dictionary<string, string> GetEffectiveConfiguration(McpServer server, ServerInstallation? installation)
    {
        // If installation has agent-specific config and it's not empty, use it
        if (installation?.AgentSpecificConfig != null && installation.AgentSpecificConfig.Any())
        {
            return new Dictionary<string, string>(installation.AgentSpecificConfig);
        }

        // Otherwise, use the global server configuration
        return new Dictionary<string, string>(server.Configuration ?? new Dictionary<string, string>());
    }

    public bool DoesAgentConfigMatchGlobal(McpServer server, ServerInstallation installation)
    {
        var globalConfig = server.Configuration ?? new Dictionary<string, string>();
        var agentConfig = installation.AgentSpecificConfig ?? new Dictionary<string, string>();

        return AreConfigurationsEqual(globalConfig, agentConfig);
    }

    public async Task<IEnumerable<string>> PropagateConfigurationUpdateAsync(
        string serverId,
        Dictionary<string, string> oldGlobalConfig,
        Dictionary<string, string> newGlobalConfig)
    {
        var updatedInstallationIds = new List<string>();

        // Get all installations for this server
        var installations = await _installationManager.GetInstallationsByServerIdAsync(serverId);

        foreach (var installation in installations)
        {
            // Only update if agent config exactly matches the old global config
            if (AreConfigurationsEqual(installation.AgentSpecificConfig, oldGlobalConfig))
            {
                // Update to the new global config
                await _installationManager.UpdateInstallationConfigAsync(installation.Id, new Dictionary<string, string>(newGlobalConfig));
                updatedInstallationIds.Add(installation.Id);
            }
        }

        return updatedInstallationIds;
    }

    public ConfigurationValidationResult ValidateConfiguration(Dictionary<string, string> config)
    {
        var result = new ConfigurationValidationResult { IsValid = true };

        if (config == null)
        {
            result.IsValid = false;
            result.Errors.Add("Configuration cannot be null");
            return result;
        }

        // Validate each key-value pair
        foreach (var kvp in config)
        {
            // Check for null or empty keys
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                result.IsValid = false;
                result.Errors.Add("Configuration keys cannot be null or empty");
            }

            // Check for null values (empty strings are allowed)
            if (kvp.Value == null)
            {
                result.IsValid = false;
                result.Errors.Add($"Configuration value for key '{kvp.Key}' cannot be null");
            }
        }

        return result;
    }

    public string SerializeConfiguration(Dictionary<string, string> config)
    {
        if (config == null || !config.Any())
        {
            return "{}";
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(config, options);
    }

    public Dictionary<string, string>? DeserializeConfiguration(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return config ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            // Invalid JSON
            return null;
        }
    }
}
