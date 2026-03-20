using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;

namespace McpManager.Tests.Services;

public class AgentManagerTests
{
    [Fact]
    public async Task DetectInstalledAgentsAsync_ReturnsEmptyWhenNoAgentsInstalled()
    {
        // Arrange
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(false);
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync([]);

        var agentManager = new AgentManager([mockConnector.Object]);

        // Act
        var agents = await agentManager.DetectInstalledAgentsAsync();

        // Assert
        Assert.Empty(agents);
    }

    [Fact]
    public async Task DetectInstalledAgentsAsync_ReturnsDetectedAgents()
    {
        // Arrange
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        mockConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/to/config");
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync(new List<ConfiguredAgentServer>
        {
            new() { ConfiguredServerKey = "server1", ServerId = "server1", IsEnabled = true },
            new() { ConfiguredServerKey = "server2", ServerId = "server2", IsEnabled = true }
        });

        var agentManager = new AgentManager([mockConnector.Object]);

        // Act
        var agents = await agentManager.DetectInstalledAgentsAsync();

        // Assert
        Assert.Single(agents);
        var agent = agents.First();
        Assert.Equal("claude", agent.Id);
        Assert.Equal("Claude Desktop", agent.Name);
        Assert.Equal(AgentType.Claude, agent.Type);
        Assert.True(agent.IsDetected);
        Assert.Equal("/path/to/config", agent.ConfigPath);
        Assert.Equal(2, agent.ConfiguredServerIds.Count);
    }

    [Fact]
    public async Task DetectInstalledAgentsAsync_HandlesMultipleConnectors()
    {
        // Arrange
        var claudeConnector = new Mock<IAgentConnector>();
        claudeConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        claudeConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        claudeConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/claude");
        claudeConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync([]);

        var copilotConnector = new Mock<IAgentConnector>();
        copilotConnector.Setup(c => c.AgentType).Returns(AgentType.GitHubCopilot);
        copilotConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        copilotConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/copilot");
        copilotConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync([]);

        var agentManager = new AgentManager([claudeConnector.Object, copilotConnector.Object]);

        // Act
        var agents = await agentManager.DetectInstalledAgentsAsync();

        // Assert
        Assert.Equal(2, agents.Count());
        Assert.Contains(agents, a => a.Type == AgentType.Claude);
        Assert.Contains(agents, a => a.Type == AgentType.GitHubCopilot);
    }

    [Fact]
    public async Task GetAgentByIdAsync_ReturnsCorrectAgent()
    {
        // Arrange
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        mockConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/to/config");
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync([]);

        var agentManager = new AgentManager([mockConnector.Object]);

        // Act
        var agent = await agentManager.GetAgentByIdAsync("claude");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal("claude", agent.Id);
        Assert.Equal(AgentType.Claude, agent.Type);
    }

    [Fact]
    public async Task GetAgentByIdAsync_ReturnsNullForNonExistentAgent()
    {
        // Arrange
        var agentManager = new AgentManager([]);

        // Act
        var agent = await agentManager.GetAgentByIdAsync("non-existent");

        // Assert
        Assert.Null(agent);
    }

    [Fact]
    public async Task GetAgentServerIdsAsync_ReturnsConfiguredServerIds()
    {
        // Arrange
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        mockConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path");
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync(new List<ConfiguredAgentServer>
        {
            new() { ConfiguredServerKey = "server1", ServerId = "server1", IsEnabled = true },
            new() { ConfiguredServerKey = "server2", ServerId = "server2", IsEnabled = true }
        });

        var agentManager = new AgentManager([mockConnector.Object]);

        // Act
        var serverIds = await agentManager.GetAgentServerIdsAsync("claude");

        // Assert
        Assert.Equal(2, serverIds.Count());
        Assert.Contains("server1", serverIds);
        Assert.Contains("server2", serverIds);
    }

    [Fact]
    public async Task GetAgentServerIdsAsync_ReturnsConfiguredKeysWhenAvailable()
    {
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.ClaudeCode);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        mockConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path");
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync(new List<ConfiguredAgentServer>
        {
            new() { ConfiguredServerKey = "project:/repo-a::tinybdd", ServerId = "tinybdd", IsEnabled = true },
            new() { ConfiguredServerKey = "project:/repo-b::tinybdd", ServerId = "tinybdd", IsEnabled = true }
        });

        var agentManager = new AgentManager([mockConnector.Object]);

        var serverIds = (await agentManager.GetAgentServerIdsAsync("claudecode")).ToList();

        Assert.Equal(2, serverIds.Count);
        Assert.Contains("project:/repo-a::tinybdd", serverIds);
        Assert.Contains("project:/repo-b::tinybdd", serverIds);
    }

    [Fact]
    public async Task DetectInstalledAgentsAsync_UsesOpenClawDisplayName()
    {
        // Arrange
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.OpenClaw);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        mockConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/to/openclaw.json");
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync(new List<ConfiguredAgentServer>
        {
            new() { ConfiguredServerKey = "filesystem", ServerId = "filesystem", IsEnabled = false }
        });

        var agentManager = new AgentManager([mockConnector.Object]);

        // Act
        var agents = await agentManager.DetectInstalledAgentsAsync();

        // Assert
        var agent = Assert.Single(agents);
        Assert.Equal("openclaw", agent.Id);
        Assert.Equal("OpenClaw", agent.Name);
        Assert.Equal(AgentType.OpenClaw, agent.Type);
        Assert.Equal("/path/to/openclaw.json", agent.ConfigPath);
        Assert.Single(agent.ConfiguredServerIds);
        Assert.False(agent.ConfiguredServers.Single().IsEnabled);
    }

    [Fact]
    public async Task DetectInstalledAgentsAsync_UsesClaudeCodeDisplayName()
    {
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.ClaudeCode);
        mockConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        mockConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/to/.claude.json");
        mockConnector.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync([]);

        var agentManager = new AgentManager([mockConnector.Object]);

        var agent = Assert.Single(await agentManager.DetectInstalledAgentsAsync());

        Assert.Equal("claudecode", agent.Id);
        Assert.Equal("Claude Code", agent.Name);
    }
}
