using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing server installations across agents.
/// Orchestrates between server manager, agent manager, and agent connectors.
/// </summary>
public class InstallationManager : IInstallationManager
{
    private readonly IServerManager _serverManager;
    private readonly IAgentManager _agentManager;
    private readonly IEnumerable<IAgentConnector> _connectors;
    private readonly List<ServerInstallation> _installations = new();

    public InstallationManager(
        IServerManager serverManager,
        IAgentManager agentManager,
        IEnumerable<IAgentConnector> connectors)
    {
        _serverManager = serverManager;
        _agentManager = agentManager;
        _connectors = connectors;
    }

    public Task<IEnumerable<ServerInstallation>> GetAllInstallationsAsync()
    {
        return Task.FromResult<IEnumerable<ServerInstallation>>(_installations);
    }

    public Task<IEnumerable<ServerInstallation>> GetInstallationsByServerIdAsync(string serverId)
    {
        var installations = _installations.Where(i => i.ServerId == serverId);
        return Task.FromResult<IEnumerable<ServerInstallation>>(installations);
    }

    public Task<IEnumerable<ServerInstallation>> GetInstallationsByAgentIdAsync(string agentId)
    {
        var installations = _installations.Where(i => i.AgentId == agentId);
        return Task.FromResult<IEnumerable<ServerInstallation>>(installations);
    }

    public async Task<ServerInstallation> AddServerToAgentAsync(string serverId, string agentId, Dictionary<string, string>? config = null)
    {
        // Find the connector for this agent
        var agent = await _agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        var connector = _connectors.FirstOrDefault(c => c.AgentType == agent.Type);
        if (connector == null)
        {
            throw new InvalidOperationException($"No connector found for agent type {agent.Type}");
        }

        // Add server to agent configuration
        await connector.AddServerToAgentAsync(serverId, config);

        // Create installation record
        var installation = new ServerInstallation
        {
            ServerId = serverId,
            AgentId = agentId,
            IsEnabled = true,
            AgentSpecificConfig = config ?? new Dictionary<string, string>()
        };

        _installations.Add(installation);
        return installation;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId, string agentId)
    {
        var agent = await _agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return false;
        }

        var connector = _connectors.FirstOrDefault(c => c.AgentType == agent.Type);
        if (connector == null)
        {
            return false;
        }

        // Remove from agent configuration
        await connector.RemoveServerFromAgentAsync(serverId);

        // Remove installation record
        var installation = _installations.FirstOrDefault(i => i.ServerId == serverId && i.AgentId == agentId);
        if (installation != null)
        {
            _installations.Remove(installation);
        }

        return true;
    }

    public async Task<bool> ToggleServerEnabledAsync(string serverId, string agentId)
    {
        var installation = _installations.FirstOrDefault(i => i.ServerId == serverId && i.AgentId == agentId);
        if (installation == null)
        {
            return false;
        }

        var agent = await _agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return false;
        }

        var connector = _connectors.FirstOrDefault(c => c.AgentType == agent.Type);
        if (connector == null)
        {
            return false;
        }

        installation.IsEnabled = !installation.IsEnabled;
        installation.UpdatedAt = DateTime.UtcNow;

        await connector.SetServerEnabledAsync(serverId, installation.IsEnabled);

        return true;
    }

    public Task<bool> UpdateInstallationConfigAsync(string installationId, Dictionary<string, string> config)
    {
        var installation = _installations.FirstOrDefault(i => i.Id == installationId);
        if (installation == null)
        {
            return Task.FromResult(false);
        }

        installation.AgentSpecificConfig = config;
        installation.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }
}
