using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Application.Services;

/// <summary>
/// Service for managing MCP servers.
/// Implements business logic and orchestration.
/// </summary>
public class ServerManager : IServerManager
{
    private readonly List<McpServer> _installedServers = new();

    public Task<IEnumerable<McpServer>> GetInstalledServersAsync()
    {
        return Task.FromResult<IEnumerable<McpServer>>(_installedServers);
    }

    public Task<McpServer?> GetServerByIdAsync(string serverId)
    {
        var server = _installedServers.FirstOrDefault(s => s.Id == serverId);
        return Task.FromResult(server);
    }

    public Task<bool> InstallServerAsync(McpServer server)
    {
        if (_installedServers.Any(s => s.Id == server.Id))
        {
            return Task.FromResult(false);
        }

        server.IsInstalled = true;
        server.InstalledAt = DateTime.UtcNow;
        _installedServers.Add(server);
        return Task.FromResult(true);
    }

    public Task<bool> UninstallServerAsync(string serverId)
    {
        var server = _installedServers.FirstOrDefault(s => s.Id == serverId);
        if (server == null)
        {
            return Task.FromResult(false);
        }

        _installedServers.Remove(server);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateServerConfigurationAsync(string serverId, Dictionary<string, string> configuration)
    {
        var server = _installedServers.FirstOrDefault(s => s.Id == serverId);
        if (server == null)
        {
            return Task.FromResult(false);
        }

        server.Configuration = configuration;
        return Task.FromResult(true);
    }
}
