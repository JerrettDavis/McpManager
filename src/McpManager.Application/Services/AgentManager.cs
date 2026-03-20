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
            var agent = await BuildAgentAsync(connector, includeRuntimeCatalog: false);
            if (agent != null)
            {
                agents.Add(agent);
            }
        }

        return agents;
    }

    public async Task<Agent?> GetAgentByIdAsync(string agentId, bool includeRuntimeCatalog = false)
    {
        var connector = connectors.FirstOrDefault(candidate =>
            string.Equals(candidate.AgentType.ToString(), agentId, StringComparison.OrdinalIgnoreCase));

        return connector == null
            ? null
            : await BuildAgentAsync(connector, includeRuntimeCatalog);
    }

    public async Task<IEnumerable<string>> GetAgentServerIdsAsync(string agentId)
    {
        var agent = await GetAgentByIdAsync(agentId);
        return agent?.ConfiguredServers
            .Select(server => string.IsNullOrWhiteSpace(server.ConfiguredServerKey) ? server.ServerId : server.ConfiguredServerKey)
            ?? Enumerable.Empty<string>();
    }

    private async Task<Agent?> BuildAgentAsync(IAgentConnector connector, bool includeRuntimeCatalog)
    {
        if (!await connector.IsAgentInstalledAsync())
        {
            return null;
        }

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

        AgentRuntimeCatalog? runtimeCatalog = null;
        if (includeRuntimeCatalog && connector is IAgentRuntimeConnector runtimeConnector)
        {
            runtimeCatalog = await runtimeConnector.GetRuntimeCatalogAsync();
        }

        return new Agent
        {
            Id = connector.AgentType.ToString().ToLowerInvariant(),
            Name = GetAgentDisplayName(connector.AgentType),
            Type = connector.AgentType,
            IsDetected = true,
            ConfigPath = configPath,
            ConfiguredServers = configuredServersList,
            RuntimeCatalog = runtimeCatalog
        };
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
