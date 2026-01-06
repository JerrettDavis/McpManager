using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for Claude Code (https://code.claude.com/docs/en/mcp).
/// Handles Claude Code-specific configuration and MCP server management.
/// </summary>
public class ClaudeCodeConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.ClaudeCode;

    public Task<bool> IsAgentInstalledAsync()
    {
        // Check for Claude Code installation
        var configPath = GetClaudeCodeConfigPath();
        return Task.FromResult(File.Exists(configPath) || Directory.Exists(Path.GetDirectoryName(configPath)));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(GetClaudeCodeConfigPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configPath = GetClaudeCodeConfigPath();
        if (!File.Exists(configPath))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<ClaudeCodeConfig>(json);
            return config?.Mcp?.Servers?.Keys ?? Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var configPath = GetClaudeCodeConfigPath();
        ClaudeCodeConfig claudeCodeConfig;

        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            claudeCodeConfig = JsonSerializer.Deserialize<ClaudeCodeConfig>(json) ?? new ClaudeCodeConfig();
        }
        else
        {
            claudeCodeConfig = new ClaudeCodeConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        }

        claudeCodeConfig.Mcp ??= new McpSection();
        claudeCodeConfig.Mcp.Servers ??= new Dictionary<string, ServerConfig>();
        
        claudeCodeConfig.Mcp.Servers[serverId] = new ServerConfig
        {
            Command = config?.GetValueOrDefault("command", "npx"),
            Args = config?.GetValueOrDefault("args", $"-y {serverId}").Split(' ').ToList(),
            Env = config != null && config.ContainsKey("env") 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(config["env"])
                : new Dictionary<string, string>()
        };

        var updatedJson = JsonSerializer.Serialize(claudeCodeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var configPath = GetClaudeCodeConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var claudeCodeConfig = JsonSerializer.Deserialize<ClaudeCodeConfig>(json);

        if (claudeCodeConfig?.Mcp?.Servers == null || !claudeCodeConfig.Mcp.Servers.ContainsKey(serverId))
        {
            return false;
        }

        claudeCodeConfig.Mcp.Servers.Remove(serverId);

        var updatedJson = JsonSerializer.Serialize(claudeCodeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var configPath = GetClaudeCodeConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var claudeCodeConfig = JsonSerializer.Deserialize<ClaudeCodeConfig>(json);

        if (claudeCodeConfig?.Mcp?.Servers == null || !claudeCodeConfig.Mcp.Servers.ContainsKey(serverId))
        {
            return false;
        }

        if (enabled)
        {
            claudeCodeConfig.Mcp.Servers[serverId].Disabled = null;
        }
        else
        {
            claudeCodeConfig.Mcp.Servers[serverId].Disabled = true;
        }

        var updatedJson = JsonSerializer.Serialize(claudeCodeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    private static string GetClaudeCodeConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return OperatingSystem.IsWindows()
            ? Path.Combine(home, ".claude-code", "mcp.json")
            : Path.Combine(home, ".claude-code", "mcp.json");
    }

    private class ClaudeCodeConfig
    {
        public McpSection? Mcp { get; set; }
    }

    private class McpSection
    {
        public Dictionary<string, ServerConfig>? Servers { get; set; }
    }

    private class ServerConfig
    {
        public string Command { get; set; } = string.Empty;
        public List<string> Args { get; set; } = new();
        public Dictionary<string, string>? Env { get; set; }
        public bool? Disabled { get; set; }
    }
}
