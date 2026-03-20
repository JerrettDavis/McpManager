using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for Claude Code (https://code.claude.com).
/// Handles Claude Code-specific configuration and MCP server management.
/// MCP servers are configured in:
/// - ~/.claude.json (user-scoped config with both user-level and project-level servers)
/// - ~/.claude/settings.json (legacy settings file)
/// See: https://code.claude.com/docs/en/plugins-reference#mcp-servers
/// </summary>
public class ClaudeCodeConnector(
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
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };
    private static readonly JsonSerializerOptions IndentedIgnoringNullJsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AgentType AgentType => AgentType.ClaudeCode;

    public Task<bool> IsAgentInstalledAsync()
    {
        var settingsDir = GetClaudeSettingsDirectory();
        var claudeExePath = GetClaudeCodeExecutablePath();
        var userConfigPath = GetUserConfigPath();

        return Task.FromResult(
            _directoryExists(settingsDir) ||
            _fileExists(claudeExePath) ||
            _fileExists(userConfigPath));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        var userConfigPath = GetUserConfigPath();
        if (_fileExists(userConfigPath))
        {
            return Task.FromResult(userConfigPath);
        }

        return Task.FromResult(GetClaudeSettingsPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configuredServers = await GetConfiguredServersAsync();
        return configuredServers.Select(server => server.ServerId).ToList();
    }

    public async Task<IEnumerable<ConfiguredAgentServer>> GetConfiguredServersAsync()
    {
        var configuredServers = new Dictionary<string, ConfiguredAgentServer>(StringComparer.OrdinalIgnoreCase);

        var settingsPath = GetClaudeSettingsPath();
        if (_fileExists(settingsPath))
        {
            try
            {
                var json = await _readAllTextAsync(settingsPath);
                var config = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json, CaseInsensitiveJsonOptions);

                if (config?.McpServers != null)
                {
                    foreach (var (key, serverConfig) in config.McpServers)
                    {
                        configuredServers[key] = CreateConfiguredServer(key, serverConfig);
                    }
                }
            }
            catch
            {
                // Ignore legacy settings parse failures.
            }
        }

        var userConfigPath = GetUserConfigPath();
        if (_fileExists(userConfigPath))
        {
            try
            {
                var json = await _readAllTextAsync(userConfigPath);
                var config = JsonSerializer.Deserialize<ClaudeUserConfig>(json, CaseInsensitiveJsonOptions);

                if (config?.McpServers != null)
                {
                    foreach (var (key, serverConfig) in config.McpServers)
                    {
                        configuredServers[key] = CreateConfiguredServer(key, key, serverConfig);
                    }
                }

                if (config?.Projects != null)
                {
                    foreach (var (projectPath, project) in config.Projects)
                    {
                        if (project.McpServers == null)
                        {
                            continue;
                        }

                        foreach (var (key, serverConfig) in project.McpServers)
                        {
                            var configuredServerKey = CreateProjectScopedKey(projectPath, key);
                            configuredServers[configuredServerKey] = CreateConfiguredServer(configuredServerKey, key, serverConfig);
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors and fall back to legacy config.
            }
        }

        return configuredServers.Values;
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var userConfigPath = GetUserConfigPath();
        if (_fileExists(userConfigPath))
        {
            return await AddServerToUserConfigAsync(serverId, config);
        }

        var settingsPath = GetClaudeSettingsPath();
        ClaudeSettingsConfig settings;

        if (_fileExists(settingsPath))
        {
            var json = await _readAllTextAsync(settingsPath);
            settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json, CaseInsensitiveJsonOptions) ?? new ClaudeSettingsConfig();
        }
        else
        {
            settings = new ClaudeSettingsConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        }

        settings.McpServers ??= new Dictionary<string, ServerConfig>();
        settings.McpServers[serverId] = new ServerConfig
        {
            Command = config?.GetValueOrDefault("command") ?? "npx",
            Args = ParseArgs(config?.GetValueOrDefault("args"), $"-y {serverId}"),
            Env = config != null && config.ContainsKey("env")
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(config["env"])
                : new Dictionary<string, string>()
        };

        var updatedJson = JsonSerializer.Serialize(settings, IndentedJsonOptions);
        await _writeAllTextAsync(settingsPath, updatedJson);

        return true;
    }

    private async Task<bool> AddServerToUserConfigAsync(string serverId, Dictionary<string, string>? config)
    {
        var userConfigPath = GetUserConfigPath();
        ClaudeUserConfig userConfig;

        if (_fileExists(userConfigPath))
        {
            var json = await _readAllTextAsync(userConfigPath);
            userConfig = JsonSerializer.Deserialize<ClaudeUserConfig>(json, CaseInsensitiveJsonOptions) ?? new ClaudeUserConfig();
        }
        else
        {
            userConfig = new ClaudeUserConfig();
        }

        userConfig.McpServers ??= new Dictionary<string, UserServerConfig>();

        var serverType = config?.GetValueOrDefault("type", "stdio") ?? "stdio";
        if (serverType == "http")
        {
            userConfig.McpServers[serverId] = new UserServerConfig
            {
                Type = "http",
                Url = config?.GetValueOrDefault("url")
            };
        }
        else
        {
            userConfig.McpServers[serverId] = new UserServerConfig
            {
                Type = "stdio",
                Command = config?.GetValueOrDefault("command") ?? "npx",
                Args = ParseArgs(config?.GetValueOrDefault("args"), $"-y {serverId}"),
                Env = config != null && config.ContainsKey("env")
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(config["env"])
                    : null
            };
        }

        var updatedJson = JsonSerializer.Serialize(userConfig, IndentedIgnoringNullJsonOptions);
        await _writeAllTextAsync(userConfigPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var userConfigPath = GetUserConfigPath();
        if (_fileExists(userConfigPath))
        {
            try
            {
                var json = await _readAllTextAsync(userConfigPath);
                var userConfig = JsonSerializer.Deserialize<ClaudeUserConfig>(json, CaseInsensitiveJsonOptions);

                if (TryParseProjectScopedKey(serverId, out var projectPath, out var projectServerId))
                {
                    if (TryGetProjectServerConfig(userConfig, projectPath, out var projectServers) &&
                        projectServers.Remove(projectServerId))
                    {
                        var updatedJson = JsonSerializer.Serialize(userConfig, IndentedIgnoringNullJsonOptions);
                        await _writeAllTextAsync(userConfigPath, updatedJson);
                        return true;
                    }
                }
                else if (userConfig?.McpServers != null && userConfig.McpServers.ContainsKey(serverId))
                {
                    userConfig.McpServers.Remove(serverId);

                    var updatedJson = JsonSerializer.Serialize(userConfig, IndentedIgnoringNullJsonOptions);
                    await _writeAllTextAsync(userConfigPath, updatedJson);
                    return true;
                }
            }
            catch
            {
                // Fall back to legacy settings.json when user config exists but is unreadable.
            }
        }

        var settingsPath = GetClaudeSettingsPath();
        if (!_fileExists(settingsPath))
        {
            return false;
        }

        var settingsJson = await _readAllTextAsync(settingsPath);
        var settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(settingsJson, CaseInsensitiveJsonOptions);

        if (settings?.McpServers == null || !settings.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        settings.McpServers.Remove(serverId);

        var updatedSettingsJson = JsonSerializer.Serialize(settings, IndentedJsonOptions);
        await _writeAllTextAsync(settingsPath, updatedSettingsJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var userConfigPath = GetUserConfigPath();
        if (_fileExists(userConfigPath))
        {
            try
            {
                var json = await _readAllTextAsync(userConfigPath);
                var userConfig = JsonSerializer.Deserialize<ClaudeUserConfig>(json, CaseInsensitiveJsonOptions);

                if (TryParseProjectScopedKey(serverId, out var projectPath, out var projectServerId))
                {
                    if (TryGetProjectServerConfig(userConfig, projectPath, out var projectServers) &&
                        projectServers.TryGetValue(projectServerId, out var projectServerConfig))
                    {
                        projectServerConfig.Disabled = enabled ? null : true;

                        var updatedJson = JsonSerializer.Serialize(userConfig, IndentedIgnoringNullJsonOptions);
                        await _writeAllTextAsync(userConfigPath, updatedJson);
                        return true;
                    }
                }
                else if (userConfig?.McpServers != null && userConfig.McpServers.ContainsKey(serverId))
                {
                    userConfig.McpServers[serverId].Disabled = enabled ? null : true;

                    var updatedJson = JsonSerializer.Serialize(userConfig, IndentedIgnoringNullJsonOptions);
                    await _writeAllTextAsync(userConfigPath, updatedJson);
                    return true;
                }
            }
            catch
            {
                // Fall back to legacy settings.json when user config exists but is unreadable.
            }
        }

        var settingsPath = GetClaudeSettingsPath();
        if (!_fileExists(settingsPath))
        {
            return false;
        }

        var settingsJson = await _readAllTextAsync(settingsPath);
        var settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(settingsJson, CaseInsensitiveJsonOptions);

        if (settings?.McpServers == null || !settings.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        settings.McpServers[serverId].Disabled = enabled ? null : true;

        var updatedSettingsJson = JsonSerializer.Serialize(settings, IndentedJsonOptions);
        await _writeAllTextAsync(settingsPath, updatedSettingsJson);

        return true;
    }

    private string GetClaudeSettingsDirectory()
    {
        return Path.Combine(_homeDirectoryResolver(), ".claude");
    }

    private string GetClaudeSettingsPath()
    {
        return Path.Combine(GetClaudeSettingsDirectory(), "settings.json");
    }

    private string GetUserConfigPath()
    {
        return Path.Combine(_homeDirectoryResolver(), ".claude.json");
    }

    private string GetClaudeCodeExecutablePath()
    {
        return Path.Combine(_homeDirectoryResolver(), ".local", "bin", "claude.exe");
    }

    private static ConfiguredAgentServer CreateConfiguredServer(string configuredServerKey, string serverId, UserServerConfig serverConfig)
    {
        var rawConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(serverConfig.Type))
        {
            rawConfig["type"] = serverConfig.Type;
        }

        if (!string.IsNullOrWhiteSpace(serverConfig.Command))
        {
            rawConfig["command"] = serverConfig.Command;
        }

        if (serverConfig.Args?.Any() == true)
        {
            rawConfig["args"] = JsonSerializer.Serialize(serverConfig.Args);
        }

        if (serverConfig.Env?.Any() == true)
        {
            rawConfig["env"] = JsonSerializer.Serialize(serverConfig.Env);
        }

        if (!string.IsNullOrWhiteSpace(serverConfig.Url))
        {
            rawConfig["url"] = serverConfig.Url;
        }

        if (serverConfig.Disabled.HasValue)
        {
            rawConfig["disabled"] = serverConfig.Disabled.Value.ToString().ToLowerInvariant();
        }

        return new ConfiguredAgentServer
        {
            ConfiguredServerKey = configuredServerKey,
            ServerId = serverId,
            IsEnabled = serverConfig.Disabled != true,
            RawConfig = rawConfig
        };
    }

    private static ConfiguredAgentServer CreateConfiguredServer(string key, ServerConfig serverConfig)
    {
        var rawConfig = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["command"] = serverConfig.Command
        };

        if (serverConfig.Args.Any())
        {
            rawConfig["args"] = JsonSerializer.Serialize(serverConfig.Args);
        }

        if (serverConfig.Env?.Any() == true)
        {
            rawConfig["env"] = JsonSerializer.Serialize(serverConfig.Env);
        }

        if (serverConfig.Disabled.HasValue)
        {
            rawConfig["disabled"] = serverConfig.Disabled.Value.ToString().ToLowerInvariant();
        }

        return new ConfiguredAgentServer
        {
            ConfiguredServerKey = key,
            ServerId = key,
            IsEnabled = serverConfig.Disabled != true,
            RawConfig = rawConfig
        };
    }

    private class ClaudeUserConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, UserServerConfig>? McpServers { get; set; }

        [JsonPropertyName("projects")]
        public Dictionary<string, ProjectConfig>? Projects { get; set; }
    }

    private class ProjectConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, UserServerConfig>? McpServers { get; set; }
    }

    private class UserServerConfig
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("command")]
        public string? Command { get; set; }

        [JsonPropertyName("args")]
        public List<string>? Args { get; set; }

        [JsonPropertyName("env")]
        public Dictionary<string, string>? Env { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }
    }

    private class ClaudeSettingsConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, ServerConfig>? McpServers { get; set; }
    }

    private class ServerConfig
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        [JsonPropertyName("args")]
        public List<string> Args { get; set; } = [];

        [JsonPropertyName("env")]
        public Dictionary<string, string>? Env { get; set; }

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }
    }

    private static string CreateProjectScopedKey(string projectPath, string serverId)
    {
        return $"project:{NormalizeProjectPath(projectPath)}::{serverId}";
    }

    private static bool TryParseProjectScopedKey(string configuredServerKey, out string projectPath, out string serverId)
    {
        projectPath = string.Empty;
        serverId = string.Empty;

        if (!configuredServerKey.StartsWith("project:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var separatorIndex = configuredServerKey.LastIndexOf("::", StringComparison.Ordinal);
        if (separatorIndex < 0)
        {
            return false;
        }

        projectPath = NormalizeProjectPath(configuredServerKey["project:".Length..separatorIndex]);
        serverId = configuredServerKey[(separatorIndex + 2)..];
        return !string.IsNullOrWhiteSpace(projectPath) && !string.IsNullOrWhiteSpace(serverId);
    }

    private static string NormalizeProjectPath(string projectPath)
    {
        return projectPath
            .Replace('\\', '/')
            .TrimEnd('/');
    }

    private static bool TryGetProjectServerConfig(
        ClaudeUserConfig? userConfig,
        string normalizedProjectPath,
        out Dictionary<string, UserServerConfig> projectServers)
    {
        projectServers = [];
        if (userConfig?.Projects == null)
        {
            return false;
        }

        foreach (var (projectKey, projectConfig) in userConfig.Projects)
        {
            if (!string.Equals(NormalizeProjectPath(projectKey), normalizedProjectPath, StringComparison.OrdinalIgnoreCase) ||
                projectConfig.McpServers == null)
            {
                continue;
            }

            projectServers = projectConfig.McpServers;
            return true;
        }

        return false;
    }

    private static List<string> ParseArgs(string? rawArgs, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawArgs))
        {
            return fallback.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        try
        {
            var parsedArgs = JsonSerializer.Deserialize<List<string>>(rawArgs);
            if (parsedArgs is { Count: > 0 })
            {
                return parsedArgs;
            }
        }
        catch
        {
            // Fall back to legacy space-delimited values.
        }

        return rawArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
