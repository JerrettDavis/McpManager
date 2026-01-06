using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for Claude Desktop AI agent.
/// Handles Claude-specific configuration and MCP server management.
/// </summary>
public class ClaudeConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.Claude;

    public Task<bool> IsAgentInstalledAsync()
    {
        // Check for Claude Desktop installation
        var configPath = GetClaudeConfigPath();
        return Task.FromResult(File.Exists(configPath));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(GetClaudeConfigPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configPath = GetClaudeConfigPath();
        if (!File.Exists(configPath))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<ClaudeConfig>(json);
            return config?.McpServers?.Keys ?? Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var configPath = GetClaudeConfigPath();
        ClaudeConfig claudeConfig;

        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            claudeConfig = JsonSerializer.Deserialize<ClaudeConfig>(json) ?? new ClaudeConfig();
        }
        else
        {
            claudeConfig = new ClaudeConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        }

        claudeConfig.McpServers ??= new Dictionary<string, Dictionary<string, string>>();
        claudeConfig.McpServers[serverId] = config ?? new Dictionary<string, string>();

        var updatedJson = JsonSerializer.Serialize(claudeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var configPath = GetClaudeConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var claudeConfig = JsonSerializer.Deserialize<ClaudeConfig>(json);

        if (claudeConfig?.McpServers == null || !claudeConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        claudeConfig.McpServers.Remove(serverId);

        var updatedJson = JsonSerializer.Serialize(claudeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var configPath = GetClaudeConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var claudeConfig = JsonSerializer.Deserialize<ClaudeConfig>(json);

        if (claudeConfig?.McpServers == null || !claudeConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        claudeConfig.McpServers[serverId]["enabled"] = enabled.ToString().ToLower();

        var updatedJson = JsonSerializer.Serialize(claudeConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    private static string GetClaudeConfigPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return OperatingSystem.IsWindows()
            ? Path.Combine(appData, "Claude", "claude_desktop_config.json")
            : OperatingSystem.IsMacOS()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Claude", "claude_desktop_config.json")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "Claude", "claude_desktop_config.json");
    }

    private class ClaudeConfig
    {
        public Dictionary<string, Dictionary<string, string>>? McpServers { get; set; }
    }
}
