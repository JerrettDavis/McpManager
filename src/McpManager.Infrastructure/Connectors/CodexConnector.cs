using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;

namespace McpManager.Infrastructure.Connectors;

/// <summary>
/// Connector for OpenAI Codex (https://developers.openai.com/codex/mcp).
/// Handles Codex-specific configuration and MCP server management.
/// </summary>
public class CodexConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.OpenAICodex;

    public Task<bool> IsAgentInstalledAsync()
    {
        // Check for Codex installation
        var configPath = GetCodexConfigPath();
        return Task.FromResult(File.Exists(configPath) || Directory.Exists(Path.GetDirectoryName(configPath)));
    }

    public Task<string> GetConfigurationPathAsync()
    {
        return Task.FromResult(GetCodexConfigPath());
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        var configPath = GetCodexConfigPath();
        if (!File.Exists(configPath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(configPath);
            var config = JsonSerializer.Deserialize<CodexConfig>(json);
            return config?.McpServers?.Keys ?? Enumerable.Empty<string>();
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
    {
        var configPath = GetCodexConfigPath();
        CodexConfig codexConfig;

        if (File.Exists(configPath))
        {
            var json = await File.ReadAllTextAsync(configPath);
            codexConfig = JsonSerializer.Deserialize<CodexConfig>(json) ?? new CodexConfig();
        }
        else
        {
            codexConfig = new CodexConfig();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        }

        codexConfig.McpServers ??= new Dictionary<string, ServerConfig>();
        
        codexConfig.McpServers[serverId] = new ServerConfig
        {
            Command = config?.GetValueOrDefault("command", "node"),
            Args = config?.GetValueOrDefault("args", $"{serverId}/index.js").Split(' ').ToList(),
            Env = config != null && config.ContainsKey("env") 
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(config["env"])
                : new Dictionary<string, string>(),
            Enabled = true
        };

        var updatedJson = JsonSerializer.Serialize(codexConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        var configPath = GetCodexConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var codexConfig = JsonSerializer.Deserialize<CodexConfig>(json);

        if (codexConfig?.McpServers == null || !codexConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        codexConfig.McpServers.Remove(serverId);

        var updatedJson = JsonSerializer.Serialize(codexConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        var configPath = GetCodexConfigPath();
        if (!File.Exists(configPath))
        {
            return false;
        }

        var json = await File.ReadAllTextAsync(configPath);
        var codexConfig = JsonSerializer.Deserialize<CodexConfig>(json);

        if (codexConfig?.McpServers == null || !codexConfig.McpServers.ContainsKey(serverId))
        {
            return false;
        }

        codexConfig.McpServers[serverId].Enabled = enabled;

        var updatedJson = JsonSerializer.Serialize(codexConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, updatedJson);

        return true;
    }

    private static string GetCodexConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return OperatingSystem.IsWindows()
            ? Path.Combine(home, ".codex", "mcp_config.json")
            : OperatingSystem.IsMacOS()
                ? Path.Combine(home, "Library", "Application Support", "Codex", "mcp_config.json")
                : Path.Combine(home, ".config", "codex", "mcp_config.json");
    }

    private class CodexConfig
    {
        public Dictionary<string, ServerConfig>? McpServers { get; set; }
    }

    private class ServerConfig
    {
        public string Command { get; set; } = string.Empty;
        public List<string> Args { get; set; } = [];
        public Dictionary<string, string>? Env { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
