using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;

namespace McpManager.Tests.Integration;

/// <summary>
/// Integration tests demonstrating the full configuration workflow
/// </summary>
public class ConfigurationWorkflowTests
{
    [Fact]
    public async Task ConfigurationWorkflow_GlobalUpdatePropagation_WorksCorrectly()
    {
        // Arrange - Set up the services
        var mockRepository = new Mock<IServerRepository>();
        var installedServers = new List<McpServer>();
        
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(() => installedServers);
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => installedServers.FirstOrDefault(s => s.Id == id));
        mockRepository.Setup(r => r.AddAsync(It.IsAny<McpServer>()))
            .Callback<McpServer>(s => installedServers.Add(s))
            .ReturnsAsync(true);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<McpServer>()))
            .Callback<McpServer>(s => {
                var existing = installedServers.FirstOrDefault(x => x.Id == s.Id);
                if (existing != null) {
                    var index = installedServers.IndexOf(existing);
                    installedServers[index] = s;
                }
            })
            .ReturnsAsync(true);
        mockRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => installedServers.Any(s => s.Id == id));
            
        var serverManager = new ServerManager(mockRepository.Object);
        var mockAgentManager = new Mock<IAgentManager>();
        var mockClaudeConnector = new Mock<IAgentConnector>();
        var mockCopilotConnector = new Mock<IAgentConnector>();
        
        mockClaudeConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        mockCopilotConnector.Setup(c => c.AgentType).Returns(AgentType.GitHubCopilot);
        
        var connectors = new List<IAgentConnector> { mockClaudeConnector.Object, mockCopilotConnector.Object };
        var installationManager = new InstallationManager(serverManager, mockAgentManager.Object, connectors, []);
        var configService = new ConfigurationService(installationManager);

        // Create a server with initial global configuration
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Configuration = new Dictionary<string, string>
            {
                ["apiKey"] = "initial_key",
                ["endpoint"] = "https://api.example.com"
            }
        };
        await serverManager.InstallServerAsync(server);

        // Create two agents
        var agent1 = new Agent { Id = "agent1", Name = "Agent 1", Type = AgentType.Claude };
        var agent2 = new Agent { Id = "agent2", Name = "Agent 2", Type = AgentType.GitHubCopilot };
        
        mockAgentManager.Setup(m => m.GetAgentByIdAsync("agent1")).ReturnsAsync(agent1);
        mockAgentManager.Setup(m => m.GetAgentByIdAsync("agent2")).ReturnsAsync(agent2);

        // Add server to agent1 with global config (matching)
        var installation1 = await installationManager.AddServerToAgentAsync(
            "test-server", 
            "agent1", 
            new Dictionary<string, string>(server.Configuration));

        // Add server to agent2 with custom config (different)
        var installation2 = await installationManager.AddServerToAgentAsync(
            "test-server",
            "agent2",
            new Dictionary<string, string>
            {
                ["apiKey"] = "custom_key",
                ["endpoint"] = "https://custom.example.com"
            });

        // Verify initial state
        Assert.True(configService.DoesAgentConfigMatchGlobal(server, installation1));
        Assert.False(configService.DoesAgentConfigMatchGlobal(server, installation2));

        // Act - Update the global configuration
        var oldGlobalConfig = new Dictionary<string, string>(server.Configuration);
        var newGlobalConfig = new Dictionary<string, string>
        {
            ["apiKey"] = "updated_key",
            ["endpoint"] = "https://api.example.com"
        };

        await serverManager.UpdateServerConfigurationAsync("test-server", newGlobalConfig);

        // Propagate the update
        var updatedInstallations = await configService.PropagateConfigurationUpdateAsync(
            "test-server",
            oldGlobalConfig,
            newGlobalConfig);

        // Assert - Verify propagation behavior
        // Agent1 should be updated (it matched the old global config)
        Assert.Contains(installation1.Id, updatedInstallations);
        
        // Agent2 should NOT be updated (it had custom config)
        Assert.DoesNotContain(installation2.Id, updatedInstallations);

        // Verify the actual configurations
        var updatedInstallation1 = (await installationManager.GetInstallationsByAgentIdAsync("agent1")).First();
        var updatedInstallation2 = (await installationManager.GetInstallationsByAgentIdAsync("agent2")).First();

        // Agent1 config should now have the updated key
        Assert.Equal("updated_key", updatedInstallation1.AgentSpecificConfig["apiKey"]);
        
        // Agent2 config should still have the custom key
        Assert.Equal("custom_key", updatedInstallation2.AgentSpecificConfig["apiKey"]);
        Assert.Equal("https://custom.example.com", updatedInstallation2.AgentSpecificConfig["endpoint"]);
    }

    [Fact]
    public async Task ConfigurationWorkflow_ResetToGlobal_WorksCorrectly()
    {
        // Arrange
        var mockRepository = new Mock<IServerRepository>();
        var installedServers = new List<McpServer>();
        
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(() => installedServers);
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => installedServers.FirstOrDefault(s => s.Id == id));
        mockRepository.Setup(r => r.AddAsync(It.IsAny<McpServer>()))
            .Callback<McpServer>(s => installedServers.Add(s))
            .ReturnsAsync(true);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<McpServer>()))
            .Callback<McpServer>(s => {
                var existing = installedServers.FirstOrDefault(x => x.Id == s.Id);
                if (existing != null) {
                    var index = installedServers.IndexOf(existing);
                    installedServers[index] = s;
                }
            })
            .ReturnsAsync(true);
        mockRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => installedServers.Any(s => s.Id == id));
            
        var serverManager = new ServerManager(mockRepository.Object);
        var mockAgentManager = new Mock<IAgentManager>();
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        
        var connectors = new List<IAgentConnector> { mockConnector.Object };
        var installationManager = new InstallationManager(serverManager, mockAgentManager.Object, connectors, []);
        var configurationService = new ConfigurationService(installationManager);

        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Configuration = new Dictionary<string, string>
            {
                ["setting1"] = "global_value1",
                ["setting2"] = "global_value2"
            }
        };
        await serverManager.InstallServerAsync(server);

        var agent = new Agent { Id = "agent1", Name = "Agent 1", Type = AgentType.Claude };
        mockAgentManager.Setup(m => m.GetAgentByIdAsync("agent1")).ReturnsAsync(agent);

        // Add server with custom config
        var installation = await installationManager.AddServerToAgentAsync(
            "test-server",
            "agent1",
            new Dictionary<string, string>
            {
                ["setting1"] = "custom_value1",
                ["setting2"] = "custom_value2"
            });

        // Verify it's different from global
        Assert.False(configurationService.DoesAgentConfigMatchGlobal(server, installation));

        // Act - Reset to global configuration
        await installationManager.UpdateInstallationConfigAsync(
            installation.Id,
            new Dictionary<string, string>(server.Configuration));

        // Assert
        var updatedInstallation = (await installationManager.GetInstallationsByAgentIdAsync("agent1")).First();
        Assert.True(configurationService.DoesAgentConfigMatchGlobal(server, updatedInstallation));
        Assert.Equal("global_value1", updatedInstallation.AgentSpecificConfig["setting1"]);
        Assert.Equal("global_value2", updatedInstallation.AgentSpecificConfig["setting2"]);
    }

    [Fact]
    public void ConfigurationWorkflow_JsonSerialization_RoundTripWorks()
    {
        // Arrange
        var mockInstallationManager = new Mock<IInstallationManager>();
        var configurationService = new ConfigurationService(mockInstallationManager.Object);

        var originalConfig = new Dictionary<string, string>
        {
            ["database"] = "mydb",
            ["host"] = "localhost",
            ["port"] = "5432",
            ["ssl"] = "true"
        };

        // Act - Serialize to JSON
        var json = configurationService.SerializeConfiguration(originalConfig);

        // Act - Deserialize back
        var deserializedConfig = configurationService.DeserializeConfiguration(json);

        // Assert
        Assert.NotNull(deserializedConfig);
        Assert.Equal(originalConfig.Count, deserializedConfig.Count);
        
        foreach (var kvp in originalConfig)
        {
            Assert.True(deserializedConfig.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, deserializedConfig[kvp.Key]);
        }
    }

    [Fact]
    public async Task ConfigurationWorkflow_MultipleAgents_PropagationSelectivelyUpdates()
    {
        // Arrange
        var mockRepository = new Mock<IServerRepository>();
        var installedServers = new List<McpServer>();
        
        mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(() => installedServers);
        mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => installedServers.FirstOrDefault(s => s.Id == id));
        mockRepository.Setup(r => r.AddAsync(It.IsAny<McpServer>()))
            .Callback<McpServer>(s => installedServers.Add(s))
            .ReturnsAsync(true);
        mockRepository.Setup(r => r.UpdateAsync(It.IsAny<McpServer>()))
            .Callback<McpServer>(s => {
                var existing = installedServers.FirstOrDefault(x => x.Id == s.Id);
                if (existing != null) {
                    var index = installedServers.IndexOf(existing);
                    installedServers[index] = s;
                }
            })
            .ReturnsAsync(true);
        mockRepository.Setup(r => r.ExistsAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => installedServers.Any(s => s.Id == id));
            
        var serverManager = new ServerManager(mockRepository.Object);
        var mockAgentManager = new Mock<IAgentManager>();
        var mockConnector = new Mock<IAgentConnector>();
        mockConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        
        var connectors = new List<IAgentConnector> { mockConnector.Object };
        var installationManager = new InstallationManager(serverManager, mockAgentManager.Object, connectors, []);
        var configurationService = new ConfigurationService(installationManager);

        var server = new McpServer
        {
            Id = "shared-server",
            Name = "Shared Server",
            Configuration = new Dictionary<string, string>
            {
                ["version"] = "1.0",
                ["mode"] = "production"
            }
        };
        await serverManager.InstallServerAsync(server);

        // Create 3 agents
        var agent1 = new Agent { Id = "agent1", Name = "Agent 1", Type = AgentType.Claude };
        var agent2 = new Agent { Id = "agent2", Name = "Agent 2", Type = AgentType.Claude };
        var agent3 = new Agent { Id = "agent3", Name = "Agent 3", Type = AgentType.Claude };
        
        mockAgentManager.Setup(m => m.GetAgentByIdAsync("agent1")).ReturnsAsync(agent1);
        mockAgentManager.Setup(m => m.GetAgentByIdAsync("agent2")).ReturnsAsync(agent2);
        mockAgentManager.Setup(m => m.GetAgentByIdAsync("agent3")).ReturnsAsync(agent3);

        // Agent1 and Agent2 use global config (matching)
        await installationManager.AddServerToAgentAsync("shared-server", "agent1", 
            new Dictionary<string, string>(server.Configuration));
        await installationManager.AddServerToAgentAsync("shared-server", "agent2",
            new Dictionary<string, string>(server.Configuration));

        // Agent3 uses custom config
        await installationManager.AddServerToAgentAsync("shared-server", "agent3",
            new Dictionary<string, string>
            {
                ["version"] = "1.0",
                ["mode"] = "development" // Different!
            });

        // Act - Update global config
        var oldConfig = new Dictionary<string, string>(server.Configuration);
        var newConfig = new Dictionary<string, string>
        {
            ["version"] = "2.0", // Updated
            ["mode"] = "production"
        };

        await serverManager.UpdateServerConfigurationAsync("shared-server", newConfig);
        var updated = await configurationService.PropagateConfigurationUpdateAsync(
            "shared-server", oldConfig, newConfig);

        // Assert
        var updatedList = updated.ToList();
        Assert.Equal(2, updatedList.Count); // Only agent1 and agent2

        // Verify configurations
        var agent1Installation = (await installationManager.GetInstallationsByAgentIdAsync("agent1")).First();
        var agent2Installation = (await installationManager.GetInstallationsByAgentIdAsync("agent2")).First();
        var agent3Installation = (await installationManager.GetInstallationsByAgentIdAsync("agent3")).First();

        Assert.Equal("2.0", agent1Installation.AgentSpecificConfig["version"]);
        Assert.Equal("2.0", agent2Installation.AgentSpecificConfig["version"]);
        Assert.Equal("1.0", agent3Installation.AgentSpecificConfig["version"]); // Not updated
        Assert.Equal("development", agent3Installation.AgentSpecificConfig["mode"]); // Still custom
    }
}
