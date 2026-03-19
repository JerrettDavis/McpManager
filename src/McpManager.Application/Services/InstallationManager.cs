using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing server installations across agents.
/// Orchestrates between server manager, agent manager, and agent connectors.
/// </summary>
public class InstallationManager(
    IServerManager serverManager,
    IAgentManager agentManager,
    IEnumerable<IAgentConnector> connectors,
    ICollection<ServerInstallation> installations
)
    : IInstallationManager
{
    private readonly IServerManager _serverManager = serverManager;

    private static StringComparison KeyComparison => StringComparison.OrdinalIgnoreCase;

    public Task<IEnumerable<ServerInstallation>> GetAllInstallationsAsync()
    {
        return Task.FromResult<IEnumerable<ServerInstallation>>(installations);
    }

    public Task<IEnumerable<ServerInstallation>> GetInstallationsByServerIdAsync(string serverId)
    {
        var installations1 = installations.Where(i => i.ServerId == serverId);
        return Task.FromResult<IEnumerable<ServerInstallation>>(installations1);
    }

    public Task<IEnumerable<ServerInstallation>> GetInstallationsByAgentIdAsync(string agentId)
    {
        var installations1 = installations.Where(i => i.AgentId == agentId);
        return Task.FromResult<IEnumerable<ServerInstallation>>(installations1);
    }

    public Task<bool> UntrackServerAsync(string installationId)
    {
        var installation = installations.FirstOrDefault(i => i.Id == installationId);
        if (installation == null)
        {
            return Task.FromResult(false);
        }

        installations.Remove(installation);
        return Task.FromResult(true);
    }

    public async Task<ServerInstallation> AddServerToAgentAsync(string serverId, string agentId, Dictionary<string, string>? config = null)
    {
        // Find the connector for this agent
        var agent = await agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        var connector = connectors.FirstOrDefault(c => c.AgentType == agent.Type);
        if (connector == null)
        {
            throw new InvalidOperationException($"No connector found for agent type {agent.Type}");
        }

        var matchingConfiguredServers = agent.ConfiguredServers
            .Where(configuredServer => string.Equals(configuredServer.ServerId, serverId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var trackedInstallations = FindTrackedInstallations(agentId, serverId).ToList();
        var isAlreadyConfigured = matchingConfiguredServers.Any();
        if (trackedInstallations.Any() &&
            (!matchingConfiguredServers.Any() ||
             matchingConfiguredServers.All(configuredServer =>
                 trackedInstallations.Any(installation =>
                     string.Equals(installation.ConfiguredServerKey, configuredServer.ConfiguredServerKey, StringComparison.OrdinalIgnoreCase)))))
        {
            return trackedInstallations.First();
        }

        var resolvedConfig = config;

        if (resolvedConfig == null || resolvedConfig.Count == 0)
        {
            var server = await _serverManager.GetServerByIdAsync(serverId);
            if (server?.Configuration?.Any() == true)
            {
                resolvedConfig = new Dictionary<string, string>(server.Configuration);
            }
        }

        if (!isAlreadyConfigured)
        {
            // Add server to agent configuration file only if it's not already there
            var added = await connector.AddServerToAgentAsync(serverId, resolvedConfig);
            if (!added)
            {
                throw new InvalidOperationException($"Failed to add server {serverId} to agent {agentId}");
            }
            return await TrackConfiguredServerAsync(serverId, agentId, isEnabled: true, configuredServerKey: serverId, resolvedConfig);
        }

        ServerInstallation? trackedInstallation = trackedInstallations.FirstOrDefault();
        foreach (var configuredServer in matchingConfiguredServers)
        {
            trackedInstallation ??= await TrackConfiguredServerAsync(
                serverId,
                agentId,
                configuredServer.IsEnabled,
                configuredServer.ConfiguredServerKey,
                configuredServer.RawConfig.Any() ? configuredServer.RawConfig : resolvedConfig);

            if (!string.Equals(configuredServer.ConfiguredServerKey, trackedInstallation.ConfiguredServerKey, StringComparison.OrdinalIgnoreCase))
            {
                await TrackConfiguredServerAsync(
                    serverId,
                    agentId,
                    configuredServer.IsEnabled,
                    configuredServer.ConfiguredServerKey,
                    configuredServer.RawConfig.Any() ? configuredServer.RawConfig : resolvedConfig);
            }
        }

        return trackedInstallation!;
    }

    public async Task<ServerInstallation> TrackConfiguredServerAsync(string serverId, string agentId, bool isEnabled, string? configuredServerKey = null, Dictionary<string, string>? config = null)
    {
        var agent = await agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            throw new InvalidOperationException($"Agent {agentId} not found");
        }

        var existingInstallation = FindTrackedInstallation(agentId, serverId, configuredServerKey);
        if (existingInstallation != null)
        {
            existingInstallation.ServerId = serverId;
            existingInstallation.IsEnabled = isEnabled;
            existingInstallation.ConfiguredServerKey = configuredServerKey ?? existingInstallation.ConfiguredServerKey;
            existingInstallation.AgentSpecificConfig = config ?? existingInstallation.AgentSpecificConfig;
            existingInstallation.UpdatedAt = DateTime.UtcNow;
            return existingInstallation;
        }

        var installation = new ServerInstallation
        {
            ServerId = serverId,
            AgentId = agentId,
            ConfiguredServerKey = configuredServerKey ?? serverId,
            IsEnabled = isEnabled,
            AgentSpecificConfig = config ?? new Dictionary<string, string>()
        };

        installations.Add(installation);
        return installation;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId, string agentId)
    {
        var agent = await agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return false;
        }

        var connector = connectors.FirstOrDefault(c => c.AgentType == agent.Type);
        if (connector == null)
        {
            return false;
        }

        var removalTargets = GetOperationTargets(agent, agentId, serverId);
        if (!removalTargets.Any())
        {
            return false;
        }

        if (removalTargets.Count > 1 &&
            removalTargets.Any(installation => installation.AgentSpecificConfig == null || installation.AgentSpecificConfig.Count == 0))
        {
            return false;
        }

        var removedInstallations = new List<ServerInstallation>();
        foreach (var installation in removalTargets)
        {
            var configuredServerKey = installation.ConfiguredServerKey;
            var removed = await connector.RemoveServerFromAgentAsync(configuredServerKey);
            if (!removed)
            {
                await RestoreRemovedInstallationsAsync(connector, removedInstallations);
                return false;
            }

            removedInstallations.Add(installation);
        }

        foreach (var installation in removedInstallations)
        {
            var trackedInstallation = installations.FirstOrDefault(existing => existing.Id == installation.Id);
            if (trackedInstallation != null)
            {
                installations.Remove(trackedInstallation);
            }
        }

        return true;
    }

    public async Task<bool> ToggleServerEnabledAsync(string serverId, string agentId)
    {
        var agent = await agentManager.GetAgentByIdAsync(agentId);
        if (agent == null)
        {
            return false;
        }

        var operationTargets = GetOperationTargets(agent, agentId, serverId);
        if (!operationTargets.Any())
        {
            return false;
        }

        var connector = connectors.FirstOrDefault(c => c.AgentType == agent.Type);
        if (connector == null)
        {
            return false;
        }

        var newState = operationTargets.Any(installation => !installation.IsEnabled);
        var updatedInstallations = new List<(ServerInstallation installation, bool previousState)>();
        foreach (var installation in operationTargets)
        {
            var updated = await connector.SetServerEnabledAsync(installation.ConfiguredServerKey, newState);
            if (!updated)
            {
                await RestoreInstallationStatesAsync(connector, updatedInstallations);
                return false;
            }

            updatedInstallations.Add((installation, installation.IsEnabled));
        }

        foreach (var (installation, _) in updatedInstallations)
        {
            var trackedInstallation = installations.FirstOrDefault(existing => existing.Id == installation.Id);
            if (trackedInstallation != null)
            {
                trackedInstallation.IsEnabled = newState;
                trackedInstallation.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            await TrackConfiguredServerAsync(
                serverId,
                agentId,
                newState,
                installation.ConfiguredServerKey,
                installation.AgentSpecificConfig);
        }

        return true;
    }

    public Task<bool> UpdateInstallationConfigAsync(string installationId, Dictionary<string, string> config)
    {
        var installation = installations.FirstOrDefault(i => i.Id == installationId);
        if (installation == null)
        {
            return Task.FromResult(false);
        }

        installation.AgentSpecificConfig = config;
        installation.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(true);
    }

    private ServerInstallation? FindTrackedInstallation(string agentId, string serverId, string? configuredServerKey = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredServerKey))
        {
            return installations.FirstOrDefault(i =>
                string.Equals(i.AgentId, agentId, KeyComparison) &&
                string.Equals(i.ConfiguredServerKey, configuredServerKey, KeyComparison));
        }

        return installations.FirstOrDefault(i =>
            string.Equals(i.AgentId, agentId, KeyComparison) &&
            string.Equals(i.ServerId, serverId, KeyComparison));
    }

    private IEnumerable<ServerInstallation> FindTrackedInstallations(string agentId, string serverId)
    {
        return installations.Where(i =>
            string.Equals(i.AgentId, agentId, KeyComparison) &&
            string.Equals(i.ServerId, serverId, KeyComparison));
    }

    private List<ServerInstallation> GetOperationTargets(Agent agent, string agentId, string serverId)
    {
        var trackedInstallations = FindTrackedInstallations(agentId, serverId).ToList();
        var targetsByConfiguredKey = trackedInstallations
            .ToDictionary(
                installation => string.IsNullOrWhiteSpace(installation.ConfiguredServerKey) ? serverId : installation.ConfiguredServerKey,
                StringComparer.OrdinalIgnoreCase);

        foreach (var configuredServer in agent.ConfiguredServers
                     .Where(configuredServer => string.Equals(configuredServer.ServerId, serverId, StringComparison.OrdinalIgnoreCase)))
        {
            var configuredServerKey = string.IsNullOrWhiteSpace(configuredServer.ConfiguredServerKey)
                ? serverId
                : configuredServer.ConfiguredServerKey;

            if (!targetsByConfiguredKey.ContainsKey(configuredServerKey))
            {
                targetsByConfiguredKey[configuredServerKey] = new ServerInstallation
                {
                    AgentId = agentId,
                    ServerId = serverId,
                    ConfiguredServerKey = configuredServerKey,
                    IsEnabled = configuredServer.IsEnabled
                };
            }
        }

        if (targetsByConfiguredKey.Any())
        {
            return targetsByConfiguredKey.Values.ToList();
        }

        return trackedInstallations;
    }

    private async Task RestoreRemovedInstallationsAsync(IAgentConnector connector, IEnumerable<ServerInstallation> removedInstallations)
    {
        foreach (var installation in removedInstallations.Reverse())
        {
            var rollbackConfig = installation.AgentSpecificConfig;
            if (rollbackConfig == null || rollbackConfig.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Cannot safely restore OpenClaw alias '{installation.ConfiguredServerKey}' without original configuration.");
            }

            await connector.AddServerToAgentAsync(installation.ConfiguredServerKey, rollbackConfig);
            if (!installation.IsEnabled)
            {
                await connector.SetServerEnabledAsync(installation.ConfiguredServerKey, enabled: false);
            }
        }
    }

    private static async Task RestoreInstallationStatesAsync(
        IAgentConnector connector,
        IEnumerable<(ServerInstallation installation, bool previousState)> updatedInstallations)
    {
        foreach (var (installation, previousState) in updatedInstallations.Reverse())
        {
            await connector.SetServerEnabledAsync(installation.ConfiguredServerKey, previousState);
        }
    }
}
