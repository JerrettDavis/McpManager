using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing MCP servers.
/// Implements business logic and orchestration with persistent storage.
/// </summary>
public class ServerManager : IServerManager
{
    private readonly IServerRepository _repository;

    public ServerManager(IServerRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<McpServer>> GetInstalledServersAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<McpServer?> GetServerByIdAsync(string serverId)
    {
        return await _repository.GetByIdAsync(serverId);
    }

    public async Task<bool> InstallServerAsync(McpServer server)
    {
        if (await _repository.ExistsAsync(server.Id))
        {
            return false;
        }

        server.IsInstalled = true;
        server.InstalledAt = DateTime.UtcNow;
        return await _repository.AddAsync(server);
    }

    public async Task<bool> UninstallServerAsync(string serverId)
    {
        return await _repository.DeleteAsync(serverId);
    }

    public async Task<bool> UpdateServerConfigurationAsync(string serverId, Dictionary<string, string> configuration)
    {
        var server = await _repository.GetByIdAsync(serverId);
        if (server == null)
        {
            return false;
        }

        server.Configuration = configuration;
        return await _repository.UpdateAsync(server);
    }
}
