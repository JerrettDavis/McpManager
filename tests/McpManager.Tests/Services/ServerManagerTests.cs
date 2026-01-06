using McpManager.Application.Services;
using McpManager.Core.Models;
using Xunit;

namespace McpManager.Tests.Services;

public class ServerManagerTests
{
    private readonly ServerManager _serverManager;

    public ServerManagerTests()
    {
        _serverManager = new ServerManager();
    }

    [Fact]
    public async Task GetInstalledServersAsync_InitiallyReturnsEmpty()
    {
        // Act
        var servers = await _serverManager.GetInstalledServersAsync();

        // Assert
        Assert.Empty(servers);
    }

    [Fact]
    public async Task InstallServerAsync_AddsServerToInstalledList()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Version = "1.0.0"
        };

        // Act
        var result = await _serverManager.InstallServerAsync(server);
        var installedServers = await _serverManager.GetInstalledServersAsync();

        // Assert
        Assert.True(result);
        Assert.Single(installedServers);
        Assert.Contains(installedServers, s => s.Id == "test-server");
    }

    [Fact]
    public async Task InstallServerAsync_SetsIsInstalledAndInstalledAt()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Version = "1.0.0"
        };

        // Act
        await _serverManager.InstallServerAsync(server);
        var installedServer = await _serverManager.GetServerByIdAsync("test-server");

        // Assert
        Assert.NotNull(installedServer);
        Assert.True(installedServer.IsInstalled);
        Assert.NotNull(installedServer.InstalledAt);
    }

    [Fact]
    public async Task InstallServerAsync_ReturnsFalseForDuplicateId()
    {
        // Arrange
        var server1 = new McpServer { Id = "test-server", Name = "Server 1" };
        var server2 = new McpServer { Id = "test-server", Name = "Server 2" };

        // Act
        var result1 = await _serverManager.InstallServerAsync(server1);
        var result2 = await _serverManager.InstallServerAsync(server2);

        // Assert
        Assert.True(result1);
        Assert.False(result2);
    }

    [Fact]
    public async Task UninstallServerAsync_RemovesServer()
    {
        // Arrange
        var server = new McpServer { Id = "test-server", Name = "Test Server" };
        await _serverManager.InstallServerAsync(server);

        // Act
        var result = await _serverManager.UninstallServerAsync("test-server");
        var installedServers = await _serverManager.GetInstalledServersAsync();

        // Assert
        Assert.True(result);
        Assert.Empty(installedServers);
    }

    [Fact]
    public async Task UninstallServerAsync_ReturnsFalseForNonExistentServer()
    {
        // Act
        var result = await _serverManager.UninstallServerAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetServerByIdAsync_ReturnsCorrectServer()
    {
        // Arrange
        var server1 = new McpServer { Id = "server1", Name = "Server 1" };
        var server2 = new McpServer { Id = "server2", Name = "Server 2" };
        await _serverManager.InstallServerAsync(server1);
        await _serverManager.InstallServerAsync(server2);

        // Act
        var result = await _serverManager.GetServerByIdAsync("server2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("server2", result.Id);
        Assert.Equal("Server 2", result.Name);
    }

    [Fact]
    public async Task UpdateServerConfigurationAsync_UpdatesConfiguration()
    {
        // Arrange
        var server = new McpServer { Id = "test-server", Name = "Test Server" };
        await _serverManager.InstallServerAsync(server);
        var config = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var result = await _serverManager.UpdateServerConfigurationAsync("test-server", config);
        var updatedServer = await _serverManager.GetServerByIdAsync("test-server");

        // Assert
        Assert.True(result);
        Assert.NotNull(updatedServer);
        Assert.Equal(2, updatedServer.Configuration.Count);
        Assert.Equal("value1", updatedServer.Configuration["key1"]);
    }

    [Fact]
    public async Task UpdateServerConfigurationAsync_ReturnsFalseForNonExistentServer()
    {
        // Act
        var result = await _serverManager.UpdateServerConfigurationAsync("non-existent", new Dictionary<string, string>());

        // Assert
        Assert.False(result);
    }
}
