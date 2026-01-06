using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing AI agents and their configurations.
/// </summary>
public class AgentManager : IAgentManager
{
    private readonly IEnumerable<IAgentConnector> _connectors;

    public AgentManager(IEnumerable<IAgentConnector> connectors)
    {
        _connectors = connectors;
    }

    public async Task<IEnumerable<Agent>> DetectInstalledAgentsAsync()
    {
        var agents = new List<Agent>();

        foreach (var connector in _connectors)
        {
            var isInstalled = await connector.IsAgentInstalledAsync();
            if (isInstalled)
            {
                var configPath = await connector.GetConfigurationPathAsync();
                var serverIds = await connector.GetConfiguredServerIdsAsync();

                agents.Add(new Agent
                {
                    Id = connector.AgentType.ToString().ToLowerInvariant(),
                    Name = GetAgentDisplayName(connector.AgentType),
                    Type = connector.AgentType,
                    IsDetected = true,
                    ConfigPath = configPath,
                    ConfiguredServerIds = serverIds.ToList()
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
        return agent?.ConfiguredServerIds ?? Enumerable.Empty<string>();
    }

    private static string GetAgentDisplayName(AgentType agentType)
    {
        return agentType switch
        {
            AgentType.Claude => "Claude Desktop",
            AgentType.GitHubCopilot => "GitHub Copilot",
            AgentType.OpenAICodex => "OpenAI Codex",
            _ => agentType.ToString()
        };
    }
}
