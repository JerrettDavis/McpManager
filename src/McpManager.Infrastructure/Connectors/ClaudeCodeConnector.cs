using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for Claude Code (https://code.claude.com).
/// Handles Claude Code-specific configuration and MCP server management.
/// MCP servers are configured in ~/.claude/settings.json under the mcpServers key.
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
        
        return Task.FromResult(
            Directory.Exists(settingsDir) ||
            File.Exists(claudeExePath)
        );
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(GetClaudeSettingsPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var settingsPath = GetClaudeSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(settingsPath);
            var config = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json);
            
            // Claude Code stores MCP servers in settings.json under mcpServers key
            return config?.McpServers?.Keys ?? Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
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

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var settingsPath = GetClaudeSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json);

        if (settings?.McpServers == null || !settings.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        settings.McpServers.Remove(serverId);

        var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, updatedJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var settingsPath = GetClaudeSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(settingsPath);
        var settings = JsonSerializer.Deserialize<ClaudeSettingsConfig>(json);

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

        var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, updatedJson);

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

    private static string GetClaudeCodeExecutablePath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        // Claude Code CLI is typically installed in ~/.local/bin/claude.exe on Windows
        return Path.Combine(home, ".local", "bin", "claude.exe");
    }

    /// <summary>
    /// Represents the Claude Code settings.json structure.
    /// See: https://code.claude.com/docs/en/plugins-reference#mcp-servers
    /// </summary>
    private class ClaudeSettingsConfig
    {
        public Dictionary<string, ServerConfig>? McpServers { get; set; }
    }

    private class ServerConfig
    {
        public string Command { get; set; } = string.Empty;
        public List<string> Args { get; set; } = new();
        public Dictionary<string, string>? Env { get; set; }
        public bool? Disabled { get; set; }
    }
}
