using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for GitHub Copilot.
/// Handles Copilot-specific configuration and MCP server management.
/// </summary>
public class CopilotConnector(
    Func<string>? homeDirectoryResolver = null,
    Func<string, bool>? fileExists = null,
    Func<string, bool>? directoryExists = null,
    Func<string, Task<string>>? readAllTextAsync = null,
    Func<string, string, Task>? writeAllTextAsync = null) : IAgentConnector
{
    private readonly Func<string> _homeDirectoryResolver = homeDirectoryResolver ??
        (() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    private readonly Func<string, bool> _fileExists = fileExists ?? File.Exists;
    private readonly Func<string, bool> _directoryExists = directoryExists ?? Directory.Exists;
    private readonly Func<string, Task<string>> _readAllTextAsync = readAllTextAsync ?? (path => File.ReadAllTextAsync(path));
    private readonly Func<string, string, Task> _writeAllTextAsync = writeAllTextAsync ?? ((path, content) => File.WriteAllTextAsync(path, content));

    public AgentType AgentType => AgentType.GitHubCopilot;

    public Task<bool> IsAgentInstalledAsync()
    {
        var configPath = GetCopilotConfigPath();
        var copilotDirectory = Path.GetDirectoryName(configPath);
        return Task.FromResult(
            _fileExists(configPath) ||
            (!string.IsNullOrWhiteSpace(copilotDirectory) && _directoryExists(copilotDirectory)));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(GetCopilotConfigPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configuredServers = await GetConfiguredServersAsync();
        return configuredServers.Select(server => server.ServerId).ToList();
    }

    public async Task<IEnumerable<ConfiguredAgentServer>> GetConfiguredServersAsync()
    {
        var configPath = GetCopilotConfigPath();
        if (!_fileExists(configPath))
        {
            return [];
        }

        try
        {
            var json = await _readAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<CopilotConfig>(json);

            return config?.McpServers?.Select(server => CreateConfiguredServer(server.Key, server.Value)).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var configPath = GetCopilotConfigPath();
        CopilotConfig copilotConfig;

        if (_fileExists(configPath))
        {
            var json = await _readAllTextAsync(configPath);
            copilotConfig = JsonSerializer.Deserialize<CopilotConfig>(json) ?? new CopilotConfig();
        }
        else
        {
            copilotConfig = new CopilotConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        }

        copilotConfig.McpServers ??= new Dictionary<string, JsonElement>();
        copilotConfig.McpServers[serverId] = CreateServerConfigElement(config);

        var updatedJson = JsonSerializer.Serialize(copilotConfig, new JsonSerializerOptions { WriteIndented = true });
        await _writeAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var configPath = GetCopilotConfigPath();
        if (!_fileExists(configPath))
        {
            return false;
        }

        var json = await _readAllTextAsync(configPath);
        var copilotConfig = JsonSerializer.Deserialize<CopilotConfig>(json);

        if (copilotConfig?.McpServers == null || !copilotConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        copilotConfig.McpServers.Remove(serverId);

        var updatedJson = JsonSerializer.Serialize(copilotConfig, new JsonSerializerOptions { WriteIndented = true });
        await _writeAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var configPath = GetCopilotConfigPath();
        if (!_fileExists(configPath))
        {
            return false;
        }

        var json = await _readAllTextAsync(configPath);
        var copilotConfig = JsonSerializer.Deserialize<CopilotConfig>(json);

        if (copilotConfig?.McpServers == null || !copilotConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        var existingConfig = copilotConfig.McpServers[serverId];
        if (existingConfig.ValueKind == JsonValueKind.Object)
        {
            var rawProperties = GetRawProperties(existingConfig);
            rawProperties["enabled"] = JsonSerializer.SerializeToElement(enabled);
            copilotConfig.McpServers[serverId] = JsonSerializer.SerializeToElement(rawProperties);
        }
        else if (existingConfig.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            copilotConfig.McpServers[serverId] = JsonSerializer.SerializeToElement(enabled);
        }
        else
        {
            return false;
        }

        var updatedJson = JsonSerializer.Serialize(copilotConfig, new JsonSerializerOptions { WriteIndented = true });
        await _writeAllTextAsync(configPath, updatedJson);

        return true;
    }

    private string GetCopilotConfigPath()
    {
        return Path.Combine(_homeDirectoryResolver(), ".copilot", "mcp-config.json");
    }

    private class CopilotConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, JsonElement>? McpServers { get; set; }
    }

    private static JsonElement CreateServerConfigElement(Dictionary<string, string>? config)
    {
        var rawProperties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (config == null)
        {
            return JsonSerializer.SerializeToElement(rawProperties);
        }

        foreach (var (key, value) in config)
        {
            rawProperties[key] = ParseJsonElement(value);
        }

        return JsonSerializer.SerializeToElement(rawProperties);
    }

    private static ConfiguredAgentServer CreateConfiguredServer(string serverId, JsonElement serverConfig)
    {
        if (serverConfig.ValueKind == JsonValueKind.Object)
        {
            var rawProperties = GetRawProperties(serverConfig);
            return new ConfiguredAgentServer
            {
                ConfiguredServerKey = serverId,
                ServerId = serverId,
                IsEnabled = !IsDisabled(rawProperties),
                RawConfig = CreateRawConfig(rawProperties)
            };
        }

        return new ConfiguredAgentServer
        {
            ConfiguredServerKey = serverId,
            ServerId = serverId,
            IsEnabled = serverConfig.ValueKind != JsonValueKind.False,
            RawConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["$raw"] = serverConfig.GetRawText()
            }
        };
    }

    private static Dictionary<string, JsonElement> GetRawProperties(JsonElement serverConfig)
    {
        return serverConfig.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> CreateRawConfig(Dictionary<string, JsonElement> rawProperties)
    {
        var rawConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in rawProperties)
        {
            rawConfig[key] = value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : value.GetRawText();
        }

        return rawConfig;
    }

    private static bool IsDisabled(Dictionary<string, JsonElement> rawProperties)
    {
        if (!rawProperties.TryGetValue("enabled", out var enabledValue))
        {
            return false;
        }

        return enabledValue.ValueKind switch
        {
            JsonValueKind.False => true,
            JsonValueKind.True => false,
            JsonValueKind.String when bool.TryParse(enabledValue.GetString(), out var enabled) => !enabled,
            _ => false
        };
    }

    private static JsonElement ParseJsonElement(string value)
    {
        var trimmed = value.Trim();
        if ((trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal)) ||
            (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal)))
        {
            try
            {
                return JsonSerializer.SerializeToElement(JsonSerializer.Deserialize<JsonElement>(trimmed));
            }
            catch
            {
                return JsonSerializer.SerializeToElement(value);
            }
        }

        if (bool.TryParse(trimmed, out var boolValue))
        {
            return JsonSerializer.SerializeToElement(boolValue);
        }

        return JsonSerializer.SerializeToElement(value);
    }
}
