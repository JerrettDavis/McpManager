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
public class ClaudeCodeConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.ClaudeCode;

    public Task<bool> IsAgentInstalledAsync()
    {
        // Check for Claude Code installation by looking for settings directory or the executable
        var settingsDir = GetClaudeSettingsDirectory();
        var claudeExePath = GetClaudeCodeExecutablePath();
        var userConfigPath = GetUserConfigPath();

        return Task.FromResult(
            Directory.Exists(settingsDir) ||
            File.Exists(claudeExePath) ||
            File.Exists(userConfigPath)
        );
    }

    public Task<string> GetConfigurationPathAsync()
    {
        // Prefer the user config path (~/.claude.json) as it's the primary config location
        var userConfigPath = GetUserConfigPath();
        if (File.Exists(userConfigPath))
        {
            return Task.FromResult(userConfigPath);
        }

        return Task.FromResult(GetClaudeSettingsPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var serverIds = new HashSet<string>();

        // Check user config (~/.claude.json)
        var userConfigPath = GetUserConfigPath();
        Console.WriteLine($"[ClaudeCodeConnector] Checking user config at: {userConfigPath}");
        Console.WriteLine($"[ClaudeCodeConnector] File exists: {File.Exists(userConfigPath)}");

        if (File.Exists(userConfigPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(userConfigPath);
                Console.WriteLine($"[ClaudeCodeConnector] Config file size: {json.Length} bytes");

                var config = JsonSerializer.Deserialize<ClaudeUserConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Add user-level MCP servers
                if (config?.McpServers != null)
                {
                    Console.WriteLine($"[ClaudeCodeConnector] Found {config.McpServers.Count} user-level MCP servers");
                    foreach (var key in config.McpServers.Keys)
                    {
                        Console.WriteLine($"[ClaudeCodeConnector] Adding server: {key}");
                        serverIds.Add(key);
                    }
                }
                else
                {
                    Console.WriteLine("[ClaudeCodeConnector] No user-level MCP servers found");
                }

                // Add project-level MCP servers for the current working directory
                if (config?.Projects != null)
                {
                    var currentDir = Directory.GetCurrentDirectory();
                    Console.WriteLine($"[ClaudeCodeConnector] Current directory: {currentDir}");
                    Console.WriteLine($"[ClaudeCodeConnector] Projects in config: {string.Join(", ", config.Projects.Keys)}");

                    // Try both forward and backslash paths as keys
                    var normalizedPaths = new[]
                    {
                        currentDir,
                        currentDir.Replace('\\', '/'),
                        currentDir.Replace('/', '\\')
                    };

                    foreach (var path in normalizedPaths)
                    {
                        if (config.Projects.TryGetValue(path, out var project) && project.McpServers != null)
                        {
                            Console.WriteLine($"[ClaudeCodeConnector] Found {project.McpServers.Count} project-level servers for {path}");
                            foreach (var key in project.McpServers.Keys)
                            {
                                Console.WriteLine($"[ClaudeCodeConnector] Adding project server: {key}");
                                serverIds.Add(key);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ClaudeCodeConnector] Error reading user config: {ex.Message}");
                // Ignore errors and try legacy config
            }
        }

        // Also check legacy settings file (~/.claude/settings.json)
        var settingsPath = GetClaudeSettingsPath();
        if (File.Exists(settingsPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(settingsPath);
                var config = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json);

                if (config?.McpServers != null)
                {
                    foreach (var key in config.McpServers.Keys)
                    {
                        serverIds.Add(key);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        return serverIds;
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        // Prefer writing to user config (~/.claude.json) if it exists
        var userConfigPath = GetUserConfigPath();
        if (File.Exists(userConfigPath))
        {
            return await AddServerToUserConfigAsync(serverId, config);
        }

        // Fall back to legacy settings file
        var settingsPath = GetClaudeSettingsPath();
        ClaudeSettingsConfig settings;

        if (File.Exists(settingsPath))
        {
            var json = await File.ReadAllTextAsync(settingsPath);
            settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json) ?? new ClaudeSettingsConfig();
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
            Args = config?.GetValueOrDefault("args", $"-y {serverId}").Split(' ').ToList(),
            Env = config != null && config.ContainsKey("env")
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(config["env"])
                : new Dictionary<string, string>()
        };

        var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, updatedJson);

        return true;
    }

    private async Task<bool> AddServerToUserConfigAsync(string serverId, Dictionary<string, string>? config)
    {
        var userConfigPath = GetUserConfigPath();
        ClaudeUserConfig userConfig;

        if (File.Exists(userConfigPath))
        {
            var json = await File.ReadAllTextAsync(userConfigPath);
            userConfig = JsonSerializer.Deserialize<ClaudeUserConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ClaudeUserConfig();
        }
        else
        {
            userConfig = new ClaudeUserConfig();
        }

        userConfig.McpServers ??= new Dictionary<string, UserServerConfig>();

        // Determine server config type (stdio or http)
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
                Args = config?.GetValueOrDefault("args", $"-y {serverId}").Split(' ').ToList(),
                Env = config != null && config.ContainsKey("env")
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(config["env"])
                    : null
            };
        }

        var updatedJson = JsonSerializer.Serialize(userConfig, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        await File.WriteAllTextAsync(userConfigPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        // Try user config first
        var userConfigPath = GetUserConfigPath();
        if (File.Exists(userConfigPath))
        {
            var json = await File.ReadAllTextAsync(userConfigPath);
            var userConfig = JsonSerializer.Deserialize<ClaudeUserConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userConfig?.McpServers != null && userConfig.McpServers.ContainsKey(serverId))
            {
                userConfig.McpServers.Remove(serverId);

                var updatedJson = JsonSerializer.Serialize(userConfig, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                await File.WriteAllTextAsync(userConfigPath, updatedJson);
                return true;
            }
        }

        // Fall back to legacy settings
        var settingsPath = GetClaudeSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return false;
        }

        var settingsJson = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(settingsJson);

        if (settings?.McpServers == null || !settings.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        settings.McpServers.Remove(serverId);

        var updatedSettingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, updatedSettingsJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        // Try user config first
        var userConfigPath = GetUserConfigPath();
        if (File.Exists(userConfigPath))
        {
            var json = await File.ReadAllTextAsync(userConfigPath);
            var userConfig = JsonSerializer.Deserialize<ClaudeUserConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (userConfig?.McpServers != null && userConfig.McpServers.ContainsKey(serverId))
            {
                if (enabled)
                {
                    userConfig.McpServers[serverId].Disabled = null;
                }
                else
                {
                    userConfig.McpServers[serverId].Disabled = true;
                }

                var updatedJson = JsonSerializer.Serialize(userConfig, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                await File.WriteAllTextAsync(userConfigPath, updatedJson);
                return true;
            }
        }

        // Fall back to legacy settings
        var settingsPath = GetClaudeSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return false;
        }

        var settingsJson = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(settingsJson);

        if (settings?.McpServers == null || !settings.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        if (enabled)
        {
            settings.McpServers[serverId].Disabled = null;
        }
        else
        {
            settings.McpServers[serverId].Disabled = true;
        }

        var updatedSettingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, updatedSettingsJson);

        return true;
    }

    private static string GetClaudeSettingsDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".claude");
    }

    private static string GetClaudeSettingsPath()
    {
        return Path.Combine(GetClaudeSettingsDirectory(), "settings.json");
    }

    private static string GetUserConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".claude.json");
    }

    private static string GetClaudeCodeExecutablePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // Claude Code CLI is typically installed in ~/.local/bin/claude.exe on Windows
        return Path.Combine(home, ".local", "bin", "claude.exe");
    }

    /// <summary>
    /// Represents the ~/.claude.json user configuration structure.
    /// Contains both user-level and project-level MCP server configurations.
    /// </summary>
    private class ClaudeUserConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, UserServerConfig>? McpServers { get; set; }

        [JsonPropertyName("projects")]
        public Dictionary<string, ProjectConfig>? Projects { get; set; }
    }

    /// <summary>
    /// Represents a project-specific configuration within the user config.
    /// </summary>
    private class ProjectConfig
    {
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, UserServerConfig>? McpServers { get; set; }
    }

    /// <summary>
    /// Represents an MCP server configuration in the user config.
    /// Supports both stdio and http server types.
    /// </summary>
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

    /// <summary>
    /// Represents the legacy Claude Code settings.json structure.
    /// See: https://code.claude.com/docs/en/plugins-reference#mcp-servers
    /// </summary>
    private class ClaudeSettingsConfig
    {
        public Dictionary<string, ServerConfig>? McpServers { get; set; }
    }

    private class ServerConfig
    {
        public string Command { get; set; } = string.Empty;
        public List<string> Args { get; set; } = [];
        public Dictionary<string, string>? Env { get; set; }
        public bool? Disabled { get; set; }
    }
}
