using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing MCP servers.
/// Implements business logic and orchestration with persistent storage.
/// </summary>
public class ServerManager(IServerRepository repository) : IServerManager
{
    public async Task<IEnumerable<McpServer>> GetInstalledServersAsync()
    {
        return await repository.GetAllAsync();
    }

    public async Task<McpServer?> GetServerByIdAsync(string serverId)
    {
        return await repository.GetByIdAsync(serverId);
    }

    public async Task<bool> InstallServerAsync(McpServer server)
    {
        if (await repository.ExistsAsync(server.Id))
        {
            return false;
        }

        server.IsInstalled = true;
        server.InstalledAt = DateTime.UtcNow;
        return await repository.AddAsync(server);
    }

    public async Task<bool> UninstallServerAsync(string serverId)
    {
        return await repository.DeleteAsync(serverId);
    }

    public async Task<bool> UpdateServerConfigurationAsync(string serverId, Dictionary<string, string> configuration)
    {
        var server = await repository.GetByIdAsync(serverId);
        if (server == null)
        {
            return false;
        }

        server.Configuration = configuration;
        return await repository.UpdateAsync(server);
    }
}
