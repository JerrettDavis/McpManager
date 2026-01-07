using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;

namespace McpManager.Tests.Services;

public class ServerManagerTests
{
    private readonly ServerManager _serverManager;
    private readonly Mock<IServerRepository> _mockRepository;

    public ServerManagerTests()
    {
        _mockRepository = new Mock<IServerRepository>();
        _serverManager = new ServerManager(_mockRepository.Object);
    }

    [Fact]
    public async Task GetInstalledServersAsync_InitiallyReturnsEmpty()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<McpServer>());

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

        _mockRepository.Setup(r => r.ExistsAsync(server.Id))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<McpServer>()))
            .ReturnsAsync(true);

        // Act
        var result = await _serverManager.InstallServerAsync(server);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.AddAsync(It.Is<McpServer>(s => 
            s.Id == "test-server" && s.IsInstalled)), Times.Once);
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

        _mockRepository.Setup(r => r.ExistsAsync(server.Id))
            .ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<McpServer>()))
            .ReturnsAsync(true);

        // Act
        await _serverManager.InstallServerAsync(server);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.Is<McpServer>(s => 
            s.IsInstalled && s.InstalledAt != null)), Times.Once);
    }

    [Fact]
    public async Task InstallServerAsync_ReturnsFalseForDuplicateId()
    {
        // Arrange
        var server1 = new McpServer { Id = "test-server", Name = "Server 1" };

        _mockRepository.Setup(r => r.ExistsAsync("test-server"))
            .ReturnsAsync(true);

        // Act
        var result = await _serverManager.InstallServerAsync(server1);

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<McpServer>()), Times.Never);
    }

    [Fact]
    public async Task UninstallServerAsync_RemovesServer()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync("test-server"))
            .ReturnsAsync(true);

        // Act
        var result = await _serverManager.UninstallServerAsync("test-server");

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.DeleteAsync("test-server"), Times.Once);
    }

    [Fact]
    public async Task UninstallServerAsync_ReturnsFalseForNonExistentServer()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync("non-existent"))
            .ReturnsAsync(false);

        // Act
        var result = await _serverManager.UninstallServerAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetServerByIdAsync_ReturnsCorrectServer()
    {
        // Arrange
        var server = new McpServer { Id = "server2", Name = "Server 2" };
        _mockRepository.Setup(r => r.GetByIdAsync("server2"))
            .ReturnsAsync(server);

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
        var config = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        _mockRepository.Setup(r => r.GetByIdAsync("test-server"))
            .ReturnsAsync(server);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<McpServer>()))
            .ReturnsAsync(true);

        // Act
        var result = await _serverManager.UpdateServerConfigurationAsync("test-server", config);

        // Assert
        Assert.True(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<McpServer>(s => 
            s.Configuration.Count == 2 && s.Configuration["key1"] == "value1")), Times.Once);
    }

    [Fact]
    public async Task UpdateServerConfigurationAsync_ReturnsFalseForNonExistentServer()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync("non-existent"))
            .ReturnsAsync((McpServer?)null);

        // Act
        var result = await _serverManager.UpdateServerConfigurationAsync("non-existent", new Dictionary<string, string>());

        // Assert
        Assert.False(result);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<McpServer>()), Times.Never);
    }
}
