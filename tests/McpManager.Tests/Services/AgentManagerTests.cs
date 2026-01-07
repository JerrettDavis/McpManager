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
        mockConnector.Setup(c => c.GetConfiguredServerIdsAsync()).ReturnsAsync(new List<string> { "server1", "server2" });

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
        claudeConnector.Setup(c => c.GetConfiguredServerIdsAsync()).ReturnsAsync(new List<string>());

        var copilotConnector = new Mock<IAgentConnector>();
        copilotConnector.Setup(c => c.AgentType).Returns(AgentType.GitHubCopilot);
        copilotConnector.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(true);
        copilotConnector.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync("/path/copilot");
        copilotConnector.Setup(c => c.GetConfiguredServerIdsAsync()).ReturnsAsync(new List<string>());

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
        mockConnector.Setup(c => c.GetConfiguredServerIdsAsync()).ReturnsAsync(new List<string>());

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
        mockConnector.Setup(c => c.GetConfiguredServerIdsAsync()).ReturnsAsync(new List<string> { "server1", "server2" });

        var agentManager = new AgentManager([mockConnector.Object]);

        // Act
        var serverIds = await agentManager.GetAgentServerIdsAsync("claude");

        // Assert
        Assert.Equal(2, serverIds.Count());
        Assert.Contains("server1", serverIds);
        Assert.Contains("server2", serverIds);
    }
}
