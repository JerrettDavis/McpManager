using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;
using Xunit;

namespace McpManager.Tests.Services;

public class InstallationManagerTests
{
    private readonly Mock<IServerManager> _mockServerManager;
    private readonly Mock<IAgentManager> _mockAgentManager;
    private readonly Mock<IAgentConnector> _mockConnector;
    private readonly InstallationManager _installationManager;

    public InstallationManagerTests()
    {
        _mockServerManager = new Mock<IServerManager>();
        _mockAgentManager = new Mock<IAgentManager>();
        _mockConnector = new Mock<IAgentConnector>();
        _mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);

        _installationManager = new InstallationManager(
            _mockServerManager.Object,
            _mockAgentManager.Object,
            new[] { _mockConnector.Object });
    }

    [Fact]
    public async Task GetAllInstallationsAsync_InitiallyReturnsEmpty()
    {
        // Act
        var installations = await _installationManager.GetAllInstallationsAsync();

        // Assert
        Assert.Empty(installations);
    }

    [Fact]
    public async Task AddServerToAgentAsync_CreatesInstallation()
    {
        // Arrange
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        // Act
        var installation = await _installationManager.AddServerToAgentAsync("server1", "claude");
        var allInstallations = await _installationManager.GetAllInstallationsAsync();

        // Assert
        Assert.NotNull(installation);
        Assert.Equal("server1", installation.ServerId);
        Assert.Equal("claude", installation.AgentId);
        Assert.True(installation.IsEnabled);
        Assert.Single(allInstallations);
    }

    [Fact]
    public async Task AddServerToAgentAsync_ThrowsWhenAgentNotFound()
    {
        // Arrange
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("non-existent")).ReturnsAsync((Agent?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _installationManager.AddServerToAgentAsync("server1", "non-existent"));
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_RemovesInstallation()
    {
        // Arrange
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        await _installationManager.AddServerToAgentAsync("server1", "claude");

        // Act
        var result = await _installationManager.RemoveServerFromAgentAsync("server1", "claude");
        var allInstallations = await _installationManager.GetAllInstallationsAsync();

        // Assert
        Assert.True(result);
        Assert.Empty(allInstallations);
    }

    [Fact]
    public async Task ToggleServerEnabledAsync_TogglesIsEnabledFlag()
    {
        // Arrange
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        _mockConnector.Setup(c => c.SetServerEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(true);

        await _installationManager.AddServerToAgentAsync("server1", "claude");

        // Act
        var result = await _installationManager.ToggleServerEnabledAsync("server1", "claude");
        var installations = await _installationManager.GetInstallationsByServerIdAsync("server1");

        // Assert
        Assert.True(result);
        var installation = installations.First();
        Assert.False(installation.IsEnabled); // Should be toggled from true to false
    }

    [Fact]
    public async Task GetInstallationsByServerIdAsync_ReturnsCorrectInstallations()
    {
        // Arrange
        var mockConnector2 = new Mock<IAgentConnector>();
        mockConnector2.Setup(c => c.AgentType).Returns(AgentType.GitHubCopilot);
        mockConnector2.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        var installationManager = new InstallationManager(
            _mockServerManager.Object,
            _mockAgentManager.Object,
            new[] { _mockConnector.Object, mockConnector2.Object });

        var agent1 = new Agent { Id = "claude", Type = AgentType.Claude };
        var agent2 = new Agent { Id = "copilot", Type = AgentType.GitHubCopilot };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent1);
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("copilot")).ReturnsAsync(agent2);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        await installationManager.AddServerToAgentAsync("server1", "claude");
        await installationManager.AddServerToAgentAsync("server1", "copilot");
        await installationManager.AddServerToAgentAsync("server2", "claude");

        // Act
        var installations = await installationManager.GetInstallationsByServerIdAsync("server1");

        // Assert
        Assert.Equal(2, installations.Count());
        Assert.All(installations, i => Assert.Equal("server1", i.ServerId));
    }

    [Fact]
    public async Task GetInstallationsByAgentIdAsync_ReturnsCorrectInstallations()
    {
        // Arrange
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        await _installationManager.AddServerToAgentAsync("server1", "claude");
        await _installationManager.AddServerToAgentAsync("server2", "claude");

        // Act
        var installations = await _installationManager.GetInstallationsByAgentIdAsync("claude");

        // Assert
        Assert.Equal(2, installations.Count());
        Assert.All(installations, i => Assert.Equal("claude", i.AgentId));
    }
}
