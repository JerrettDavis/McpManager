using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that syncs MCP servers from detected agents.
/// Automatically discovers servers configured in agents and ensures they are installed in McpManager.
/// </summary>
public class AgentServerSyncWorker(
    IServiceProvider serviceProvider,
    ILogger<AgentServerSyncWorker> logger
)
    : BackgroundService
{
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Agent Server Sync Worker starting");

        // Initial delay to let the application and registries initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // Run initial sync
        await SyncAgentServersAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
                await SyncAgentServersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation, exit gracefully
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during agent server sync");
            }
        }

        logger.LogInformation("Agent Server Sync Worker stopping");
    }

    private async Task SyncAgentServersAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var agentManager = scope.ServiceProvider.GetRequiredService<IAgentManager>();
        var serverManager = scope.ServiceProvider.GetRequiredService<IServerManager>();
        var installationManager = scope.ServiceProvider.GetRequiredService<IInstallationManager>();
        var registries = scope.ServiceProvider.GetServices<IServerRegistry>().ToList();

        try
        {
            // Get all detected agents
            var agents = await agentManager.DetectInstalledAgentsAsync();
            var agentList = agents.ToList();

            if (!agentList.Any())
            {
                logger.LogDebug("No agents detected for server sync");
                return;
            }

            logger.LogInformation("Syncing servers from {Count} detected agent(s)", agentList.Count);

            var totalSynced = 0;
            var totalNew = 0;

            foreach (var agent in agentList)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var (synced, newServers) = await SyncAgentAsync(
                    agent, 
                    serverManager, 
                    installationManager, 
                    registries, 
                    cancellationToken);
                totalSynced += synced;
                totalNew += newServers;
            }

            if (totalNew > 0)
            {
                logger.LogInformation(
                    "Agent server sync completed: {Total} server(s) processed, {New} new server(s) installed",
                    totalSynced, totalNew);
            }
            else
            {
                logger.LogDebug("Agent server sync completed: {Total} server(s) checked, no new servers", totalSynced);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync agent servers");
        }
    }

    private async Task<(int synced, int newServers)> SyncAgentAsync(
        Agent agent,
        IServerManager serverManager,
        IInstallationManager installationManager,
        List<IServerRegistry> registries,
        CancellationToken cancellationToken)
    {
        var synced = 0;
        var newServers = 0;

        try
        {
            if (!agent.ConfiguredServerIds.Any())
            {
                logger.LogDebug("Agent {AgentName} has no configured servers", agent.Name);
                return (0, 0);
            }

            logger.LogDebug(
                "Syncing {Count} server(s) from agent {AgentName}",
                agent.ConfiguredServerIds.Count,
                agent.Name);

            foreach (var serverId in agent.ConfiguredServerIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                synced++;

                // Check if server is already installed
                var existingServer = await serverManager.GetServerByIdAsync(serverId);
                var wasNewlyInstalled = false;
                var actualServerId = serverId; // Track the actual installed server ID

                if (existingServer == null)
                {
                    // Server with this exact ID doesn't exist
                    // Before creating a new one, check if one with the same name already exists
                    var allServers = await serverManager.GetInstalledServersAsync();
                    var existingByName = allServers.FirstOrDefault(s =>
                        s.Name.Equals(serverId, StringComparison.OrdinalIgnoreCase));

                    if (existingByName != null)
                    {
                        // Found a server with matching name but different ID - use it instead
                        actualServerId = existingByName.Id;
                        logger.LogInformation(
                            "Server with name '{Name}' already exists as '{ExistingId}', using existing server",
                            serverId,
                            existingByName.Id);
                    }
                    else
                    {
                        // No existing server found - try to find in registries
                        var serverFromRegistry = await FindServerInRegistriesAsync(serverId, registries, cancellationToken);

                        if (serverFromRegistry != null)
                        {
                            // Check again if a server with this name was created in the meantime
                            allServers = await serverManager.GetInstalledServersAsync();
                            existingByName = allServers.FirstOrDefault(s =>
                                s.Name.Equals(serverFromRegistry.Name, StringComparison.OrdinalIgnoreCase));

                            if (existingByName != null)
                            {
                                // Server was created by another process - use it
                                actualServerId = existingByName.Id;
                                logger.LogInformation(
                                    "Server '{Name}' was created concurrently as '{ExistingId}', using existing server",
                                    serverFromRegistry.Name,
                                    existingByName.Id);
                            }
                            else
                            {
                                // Safe to install - override ID to match config
                                logger.LogDebug(
                                    "Found server in registry with ID '{RegistryId}', overriding to use config ID '{ConfigId}'",
                                    serverFromRegistry.Id,
                                    serverId);

                                serverFromRegistry.Id = serverId;

                                // Install the server with the config file's ID
                                var installed = await serverManager.InstallServerAsync(serverFromRegistry);
                                if (installed)
                                {
                                    wasNewlyInstalled = true;
                                    newServers++;
                                    actualServerId = serverId;
                                    logger.LogInformation(
                                        "Auto-installed server '{ServerId}' from agent {AgentName}",
                                        serverId,
                                        agent.Name);
                                }
                            }
                        }
                        else
                        {
                            // Server not found in registries, create a minimal entry
                            var minimalServer = new McpServer
                            {
                                Id = serverId,
                                Name = serverId,
                                Description = $"Auto-discovered from {agent.Name}",
                                Version = "unknown",
                                Author = "Unknown",
                                RepositoryUrl = string.Empty,
                                InstallCommand = string.Empty,
                                Tags = new List<string> { "auto-discovered", agent.Type.ToString().ToLowerInvariant() }
                            };

                            var installed = await serverManager.InstallServerAsync(minimalServer);
                            if (installed)
                            {
                                wasNewlyInstalled = true;
                                newServers++;
                                actualServerId = serverId;
                                logger.LogInformation(
                                    "Auto-installed unknown server '{ServerId}' from agent {AgentName} (not found in registries)",
                                    serverId,
                                    agent.Name);
                            }
                        }
                    }
                }
                else
                {
                    // Server already exists - use its ID
                    actualServerId = existingServer.Id;
                }

                // Check if installation relationship already exists
                var existingInstallations = await installationManager.GetInstallationsByServerIdAsync(actualServerId);
                var hasRelationship = existingInstallations.Any(i => i.AgentId == agent.Id);

                if (!hasRelationship)
                {
                    try
                    {
                        // Create the agent-server relationship using the actual installed server ID
                        await installationManager.AddServerToAgentAsync(actualServerId, agent.Id);

                        if (wasNewlyInstalled)
                        {
                            logger.LogDebug(
                                "Created installation relationship: server '{ServerId}' -> agent {AgentName}",
                                actualServerId,
                                agent.Name);
                        }
                        else
                        {
                            logger.LogInformation(
                                "Linked existing server '{ServerId}' to agent {AgentName}",
                                actualServerId,
                                agent.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Failed to create installation relationship for server '{ServerId}' and agent {AgentName}",
                            actualServerId,
                            agent.Name);
                    }
                }
            }

            return (synced, newServers);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync servers from agent {AgentName}", agent.Name);
            return (synced, newServers);
        }
    }

    private async Task<McpServer?> FindServerInRegistriesAsync(
        string serverId,
        List<IServerRegistry> registries,
        CancellationToken cancellationToken)
    {
        foreach (var registry in registries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var searchResults = await registry.GetAllServersAsync();
                var result = searchResults.FirstOrDefault(s => 
                    s.Server.Id.Equals(serverId, StringComparison.OrdinalIgnoreCase) ||
                    s.Server.Name.Equals(serverId, StringComparison.OrdinalIgnoreCase));

                if (result != null)
                {
                    logger.LogDebug(
                        "Found server '{ServerId}' in registry {RegistryName}",
                        serverId,
                        registry.Name);
                    return result.Server;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Error searching for server '{ServerId}' in registry {RegistryName}",
                    serverId,
                    registry.Name);
            }
        }

        return null;
    }
}
