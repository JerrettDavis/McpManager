using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;

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
            [_mockConnector.Object],
            []);
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
    public async Task AddServerToAgentAsync_UsesStoredServerConfigurationWhenConfigIsNotProvided()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        var server = new McpServer
        {
            Id = "server1",
            Configuration = new Dictionary<string, string>
            {
                ["command"] = "npx",
                ["args"] = """["-y","@modelcontextprotocol/server-filesystem"]"""
            }
        };

        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockServerManager.Setup(m => m.GetServerByIdAsync("server1")).ReturnsAsync(server);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(
                "server1",
                It.Is<Dictionary<string, string>>(configuration =>
                    configuration["command"] == "npx" &&
                    configuration["args"].Contains("@modelcontextprotocol/server-filesystem"))))
            .ReturnsAsync(true);

        await _installationManager.AddServerToAgentAsync("server1", "claude");

        _mockConnector.VerifyAll();
    }

    [Fact]
    public async Task AddServerToAgentAsync_ThrowsWhenConnectorAddFails()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _installationManager.AddServerToAgentAsync("server1", "claude"));

        var allInstallations = await _installationManager.GetAllInstallationsAsync();
        Assert.Empty(allInstallations);
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
    public async Task RemoveServerFromAgentAsync_DoesNotRemoveTrackedInstallationWhenConnectorFails()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude, ConfiguredServerIds = [] };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        await _installationManager.AddServerToAgentAsync("server1", "claude");

        var removed = await _installationManager.RemoveServerFromAgentAsync("server1", "claude");
        var allInstallations = await _installationManager.GetAllInstallationsAsync();

        Assert.False(removed);
        Assert.Single(allInstallations);
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
    public async Task ToggleServerEnabledAsync_DoesNotChangeStateWhenConnectorFails()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude, ConfiguredServerIds = [] };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);
        _mockConnector.Setup(c => c.SetServerEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(false);

        await _installationManager.AddServerToAgentAsync("server1", "claude");

        var result = await _installationManager.ToggleServerEnabledAsync("server1", "claude");
        var installation = (await _installationManager.GetInstallationsByServerIdAsync("server1")).Single();

        Assert.False(result);
        Assert.True(installation.IsEnabled);
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
            [_mockConnector.Object, mockConnector2.Object],
            []);

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

    [Fact]
    public async Task TrackConfiguredServerAsync_CreatesTrackedInstallationWithoutCallingConnector()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);

        var installation = await _installationManager.TrackConfiguredServerAsync("server1", "claude", isEnabled: false, configuredServerKey: "server1");

        Assert.Equal("server1", installation.ServerId);
        Assert.Equal("claude", installation.AgentId);
        Assert.Equal("server1", installation.ConfiguredServerKey);
        Assert.False(installation.IsEnabled);
        _mockConnector.Verify(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task AddServerToAgentAsync_DoesNotRewriteAliasTrackedInstallation()
    {
        var agent = new Agent
        {
            Id = "claude",
            Type = AgentType.Claude,
            ConfiguredServers =
            [
                new ConfiguredAgentServer { ConfiguredServerKey = "my-fs", ServerId = "filesystem", IsEnabled = true }
            ]
        };

        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "my-fs");

        var installation = await _installationManager.AddServerToAgentAsync("filesystem", "claude");

        Assert.Equal("filesystem", installation.ServerId);
        Assert.Equal("my-fs", installation.ConfiguredServerKey);
        _mockConnector.Verify(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task AddServerToAgentAsync_TracksExistingConfiguredAliasWhenNotPreviouslyTracked()
    {
        var agent = new Agent
        {
            Id = "claude",
            Type = AgentType.Claude,
            ConfiguredServers =
            [
                new ConfiguredAgentServer { ConfiguredServerKey = "my-fs", ServerId = "filesystem", IsEnabled = false }
            ]
        };

        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);

        var installation = await _installationManager.AddServerToAgentAsync("filesystem", "claude");

        Assert.Equal("filesystem", installation.ServerId);
        Assert.Equal("my-fs", installation.ConfiguredServerKey);
        Assert.False(installation.IsEnabled);
        _mockConnector.Verify(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
    }

    [Fact]
    public async Task TrackConfiguredServerAsync_PreservesDistinctAliasesForSameCanonicalServer()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-a");
        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: false, configuredServerKey: "fs-b");

        var trackedInstallations = (await _installationManager.GetInstallationsByAgentIdAsync("claude")).ToList();

        Assert.Equal(2, trackedInstallations.Count);
        Assert.Contains(trackedInstallations, installation => installation.ConfiguredServerKey == "fs-a" && installation.IsEnabled);
        Assert.Contains(trackedInstallations, installation => installation.ConfiguredServerKey == "fs-b" && !installation.IsEnabled);
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_RemovesAllTrackedAliasesForCanonicalServer()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync("fs-a")).ReturnsAsync(true);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync("fs-b")).ReturnsAsync(true);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-a",
            new Dictionary<string, string> { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem","C:\\a"]""" });
        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-b",
            new Dictionary<string, string> { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem","C:\\b"]""" });

        var removed = await _installationManager.RemoveServerFromAgentAsync("filesystem", "claude");
        var remainingInstallations = await _installationManager.GetInstallationsByAgentIdAsync("claude");

        Assert.True(removed);
        Assert.Empty(remainingInstallations);
        _mockConnector.Verify(c => c.RemoveServerFromAgentAsync("fs-a"), Times.Once);
        _mockConnector.Verify(c => c.RemoveServerFromAgentAsync("fs-b"), Times.Once);
    }

    [Fact]
    public async Task ToggleServerEnabledAsync_EnablesAllTrackedAliasesForCanonicalServer()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.SetServerEnabledAsync("fs-a", true)).ReturnsAsync(true);
        _mockConnector.Setup(c => c.SetServerEnabledAsync("fs-b", true)).ReturnsAsync(true);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-a");
        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: false, configuredServerKey: "fs-b");

        var toggled = await _installationManager.ToggleServerEnabledAsync("filesystem", "claude");
        var installations = (await _installationManager.GetInstallationsByAgentIdAsync("claude")).ToList();

        Assert.True(toggled);
        Assert.All(installations, installation => Assert.True(installation.IsEnabled));
        _mockConnector.Verify(c => c.SetServerEnabledAsync("fs-a", true), Times.Once);
        _mockConnector.Verify(c => c.SetServerEnabledAsync("fs-b", true), Times.Once);
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_RollsBackTrackedAliasesWhenLaterRemovalFails()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync("fs-a")).ReturnsAsync(true);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync("fs-b")).ReturnsAsync(false);
        _mockConnector.Setup(c => c.AddServerToAgentAsync("fs-a", It.IsAny<Dictionary<string, string>>())).ReturnsAsync(true);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-a",
            new Dictionary<string, string> { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem","C:\\a"]""" });
        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-b",
            new Dictionary<string, string> { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem","C:\\b"]""" });

        var removed = await _installationManager.RemoveServerFromAgentAsync("filesystem", "claude");
        var remainingInstallations = (await _installationManager.GetInstallationsByAgentIdAsync("claude")).ToList();

        Assert.False(removed);
        Assert.Equal(2, remainingInstallations.Count);
        _mockConnector.Verify(c => c.AddServerToAgentAsync("fs-a", It.IsAny<Dictionary<string, string>>()), Times.Once);
    }

    [Fact]
    public async Task ToggleServerEnabledAsync_RollsBackTrackedAliasesWhenLaterUpdateFails()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.SetServerEnabledAsync("fs-a", false)).ReturnsAsync(true);
        _mockConnector.Setup(c => c.SetServerEnabledAsync("fs-b", false)).ReturnsAsync(false);
        _mockConnector.Setup(c => c.SetServerEnabledAsync("fs-a", true)).ReturnsAsync(true);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-a");
        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-b");

        var toggled = await _installationManager.ToggleServerEnabledAsync("filesystem", "claude");
        var installations = (await _installationManager.GetInstallationsByAgentIdAsync("claude")).ToList();

        Assert.False(toggled);
        Assert.All(installations, installation => Assert.True(installation.IsEnabled));
        _mockConnector.Verify(c => c.SetServerEnabledAsync("fs-a", true), Times.Once);
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_UsesConfiguredAliasWhenTrackingMissing()
    {
        var agent = new Agent
        {
            Id = "claude",
            Type = AgentType.Claude,
            ConfiguredServers =
            [
                new ConfiguredAgentServer { ConfiguredServerKey = "my-fs", ServerId = "filesystem", IsEnabled = true }
            ]
        };

        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.RemoveServerFromAgentAsync("my-fs")).ReturnsAsync(true);

        var removed = await _installationManager.RemoveServerFromAgentAsync("filesystem", "claude");

        Assert.True(removed);
        _mockConnector.Verify(c => c.RemoveServerFromAgentAsync("my-fs"), Times.Once);
    }

    [Fact]
    public async Task ToggleServerEnabledAsync_UsesConfiguredAliasWhenTrackingMissing()
    {
        var agent = new Agent
        {
            Id = "claude",
            Type = AgentType.Claude,
            ConfiguredServers =
            [
                new ConfiguredAgentServer { ConfiguredServerKey = "my-fs", ServerId = "filesystem", IsEnabled = false }
            ]
        };

        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);
        _mockConnector.Setup(c => c.SetServerEnabledAsync("my-fs", true)).ReturnsAsync(true);

        var toggled = await _installationManager.ToggleServerEnabledAsync("filesystem", "claude");
        var installations = (await _installationManager.GetInstallationsByAgentIdAsync("claude")).ToList();

        Assert.True(toggled);
        Assert.Single(installations);
        Assert.Equal("my-fs", installations[0].ConfiguredServerKey);
        Assert.True(installations[0].IsEnabled);
    }

    [Fact]
    public async Task AddServerToAgentAsync_ReconcilesUntrackedAliasesWhenOneAliasIsAlreadyTracked()
    {
        var agent = new Agent
        {
            Id = "claude",
            Type = AgentType.Claude,
            ConfiguredServers =
            [
                new ConfiguredAgentServer { ConfiguredServerKey = "fs-a", ServerId = "filesystem", IsEnabled = true },
                new ConfiguredAgentServer { ConfiguredServerKey = "fs-b", ServerId = "filesystem", IsEnabled = false }
            ]
        };

        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);

        await _installationManager.TrackConfiguredServerAsync("filesystem", "claude", isEnabled: true, configuredServerKey: "fs-a");

        var installation = await _installationManager.AddServerToAgentAsync("filesystem", "claude");
        var trackedInstallations = (await _installationManager.GetInstallationsByAgentIdAsync("claude")).ToList();

        Assert.Equal("fs-a", installation.ConfiguredServerKey);
        Assert.Equal(2, trackedInstallations.Count);
        Assert.Contains(trackedInstallations, tracked => tracked.ConfiguredServerKey == "fs-b" && !tracked.IsEnabled);
    }

    [Fact]
    public async Task UntrackServerAsync_RemovesTrackedInstallationWithoutCallingConnector()
    {
        var agent = new Agent { Id = "claude", Type = AgentType.Claude };
        _mockAgentManager.Setup(m => m.GetAgentByIdAsync("claude")).ReturnsAsync(agent);

        var installation = await _installationManager.TrackConfiguredServerAsync("server1", "claude", isEnabled: true, configuredServerKey: "alias");

        var removed = await _installationManager.UntrackServerAsync(installation.Id);
        var installations = await _installationManager.GetAllInstallationsAsync();

        Assert.True(removed);
        Assert.Empty(installations);
        _mockConnector.Verify(c => c.RemoveServerFromAgentAsync(It.IsAny<string>()), Times.Never);
    }
}
