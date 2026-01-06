using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for GitHub Copilot.
/// Handles Copilot-specific configuration and MCP server management.
/// </summary>
public class CopilotConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.GitHubCopilot;

    public Task<bool> IsAgentInstalledAsync()
    {
        // Check for GitHub Copilot installation via VS Code
        var configPath = GetCopilotConfigPath();
        var vsCodePath = Path.GetDirectoryName(Path.GetDirectoryName(configPath));
        return Task.FromResult(Directory.Exists(vsCodePath));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(GetCopilotConfigPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configPath = GetCopilotConfigPath();
        if (!File.Exists(configPath))
        {
            return Enumerable.Empty<string>();
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<CopilotConfig>(json);
            return config?.McpServers?.Keys ?? Enumerable.Empty<string>();
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var configPath = GetCopilotConfigPath();
        CopilotConfig copilotConfig;

        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            copilotConfig = JsonSerializer.Deserialize<CopilotConfig>(json) ?? new CopilotConfig();
        }
        else
        {
            copilotConfig = new CopilotConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        }

        copilotConfig.McpServers ??= new Dictionary<string, Dictionary<string, string>>();
        copilotConfig.McpServers[serverId] = config ?? new Dictionary<string, string>();

        var updatedJson = JsonSerializer.Serialize(copilotConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var configPath = GetCopilotConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var copilotConfig = JsonSerializer.Deserialize<CopilotConfig>(json);

        if (copilotConfig?.McpServers == null || !copilotConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        copilotConfig.McpServers.Remove(serverId);

        var updatedJson = JsonSerializer.Serialize(copilotConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var configPath = GetCopilotConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var copilotConfig = JsonSerializer.Deserialize<CopilotConfig>(json);

        if (copilotConfig?.McpServers == null || !copilotConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        copilotConfig.McpServers[serverId]["enabled"] = enabled.ToString().ToLower();

        var updatedJson = JsonSerializer.Serialize(copilotConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    private static string GetCopilotConfigPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return OperatingSystem.IsWindows()
            ? Path.Combine(userProfile, ".vscode", "mcp", "config.json")
            : Path.Combine(userProfile, ".vscode", "mcp", "config.json");
    }

    private class CopilotConfig
    {
        public Dictionary<string, Dictionary<string, string>>? McpServers { get; set; }
    }
}
