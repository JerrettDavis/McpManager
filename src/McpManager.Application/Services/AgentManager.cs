using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing AI agents and their configurations.
/// </summary>
public class AgentManager(IEnumerable<IAgentConnector> connectors) : IAgentManager
{
    public async Task<IEnumerable<Agent>> DetectInstalledAgentsAsync()
    {
        var agents = new List<Agent>();

        foreach (var connector in connectors)
        {
            var isInstalled = await connector.IsAgentInstalledAsync();
            if (isInstalled)
            {
                var configPath = await connector.GetConfigurationPathAsync();
                List<ConfiguredAgentServer>? configuredServersList = null;
                var configuredServersTask = connector.GetConfiguredServersAsync();
                if (configuredServersTask != null)
                {
                    var configuredServers = await configuredServersTask;
                    configuredServersList = configuredServers?.ToList();
                }

                configuredServersList ??= (await connector.GetConfiguredServerIdsAsync())
                    .Select(serverId => new ConfiguredAgentServer
                    {
                        ConfiguredServerKey = serverId,
                        ServerId = serverId,
                        IsEnabled = true
                    })
                    .ToList();

                agents.Add(new Agent
                {
                    Id = connector.AgentType.ToString().ToLowerInvariant(),
                    Name = GetAgentDisplayName(connector.AgentType),
                    Type = connector.AgentType,
                    IsDetected = true,
                    ConfigPath = configPath,
                    ConfiguredServers = configuredServersList
                });
            }
        }

        return agents;
    }

    public async Task<Agent?> GetAgentByIdAsync(string agentId)
    {
        var agents = await DetectInstalledAgentsAsync();
        return agents.FirstOrDefault(a => a.Id == agentId);
    }

    public async Task<IEnumerable<string>> GetAgentServerIdsAsync(string agentId)
    {
        var agent = await GetAgentByIdAsync(agentId);
        return agent?.ConfiguredServers
            .Select(server => string.IsNullOrWhiteSpace(server.ConfiguredServerKey) ? server.ServerId : server.ConfiguredServerKey)
            ?? Enumerable.Empty<string>();
    }

    private static string GetAgentDisplayName(AgentType agentType)
    {
        return agentType switch
        {
            AgentType.Claude => "Claude Desktop",
            AgentType.ClaudeCode => "Claude Code",
            AgentType.GitHubCopilot => "GitHub Copilot",
            AgentType.OpenAICodex => "OpenAI Codex",
            AgentType.OpenClaw => "OpenClaw",
            _ => agentType.ToString()
        };
    }
}
