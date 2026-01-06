using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;
using Xunit;

namespace McpManager.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly Mock<IInstallationManager> _mockInstallationManager;
    private readonly ConfigurationService _configurationService;

    public ConfigurationServiceTests()
    {
        _mockInstallationManager = new Mock<IInstallationManager>();
        _configurationService = new ConfigurationService(_mockInstallationManager.Object);
    }

    [Fact]
    public void AreConfigurationsEqual_BothNull_ReturnsTrue()
    {
        // Act
        var result = _configurationService.AreConfigurationsEqual(null!, null!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreConfigurationsEqual_OneNull_ReturnsFalse()
    {
        // Arrange
        var config = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var result1 = _configurationService.AreConfigurationsEqual(config, null!);
        var result2 = _configurationService.AreConfigurationsEqual(null!, config);

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public void AreConfigurationsEqual_EmptyDictionaries_ReturnsTrue()
    {
        // Arrange
        var config1 = new Dictionary<string, string>();
        var config2 = new Dictionary<string, string>();

        // Act
        var result = _configurationService.AreConfigurationsEqual(config1, config2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreConfigurationsEqual_IdenticalConfigs_ReturnsTrue()
    {
        // Arrange
        var config1 = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };
        var config2 = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var result = _configurationService.AreConfigurationsEqual(config1, config2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreConfigurationsEqual_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var config1 = new Dictionary<string, string> { ["key"] = "value1" };
        var config2 = new Dictionary<string, string> { ["key"] = "value2" };

        // Act
        var result = _configurationService.AreConfigurationsEqual(config1, config2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreConfigurationsEqual_DifferentKeys_ReturnsFalse()
    {
        // Arrange
        var config1 = new Dictionary<string, string> { ["key1"] = "value" };
        var config2 = new Dictionary<string, string> { ["key2"] = "value" };

        // Act
        var result = _configurationService.AreConfigurationsEqual(config1, config2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreConfigurationsEqual_DifferentCounts_ReturnsFalse()
    {
        // Arrange
        var config1 = new Dictionary<string, string> { ["key1"] = "value1" };
        var config2 = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var result = _configurationService.AreConfigurationsEqual(config1, config2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetEffectiveConfiguration_NoInstallation_ReturnsGlobalConfig()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Configuration = new Dictionary<string, string>
            {
                ["global_key"] = "global_value"
            }
        };

        // Act
        var result = _configurationService.GetEffectiveConfiguration(server, null);

        // Assert
        Assert.Single(result);
        Assert.Equal("global_value", result["global_key"]);
    }

    [Fact]
    public void GetEffectiveConfiguration_EmptyAgentConfig_ReturnsGlobalConfig()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Configuration = new Dictionary<string, string>
            {
                ["global_key"] = "global_value"
            }
        };
        var installation = new ServerInstallation
        {
            ServerId = "test-server",
            AgentId = "agent1",
            AgentSpecificConfig = new Dictionary<string, string>()
        };

        // Act
        var result = _configurationService.GetEffectiveConfiguration(server, installation);

        // Assert
        Assert.Single(result);
        Assert.Equal("global_value", result["global_key"]);
    }

    [Fact]
    public void GetEffectiveConfiguration_WithAgentConfig_ReturnsAgentConfig()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Configuration = new Dictionary<string, string>
            {
                ["global_key"] = "global_value"
            }
        };
        var installation = new ServerInstallation
        {
            ServerId = "test-server",
            AgentId = "agent1",
            AgentSpecificConfig = new Dictionary<string, string>
            {
                ["agent_key"] = "agent_value"
            }
        };

        // Act
        var result = _configurationService.GetEffectiveConfiguration(server, installation);

        // Assert
        Assert.Single(result);
        Assert.Equal("agent_value", result["agent_key"]);
    }

    [Fact]
    public void DoesAgentConfigMatchGlobal_MatchingConfigs_ReturnsTrue()
    {
        // Arrange
        var config = new Dictionary<string, string> { ["key"] = "value" };
        var server = new McpServer
        {
            Id = "test-server",
            Configuration = config
        };
        var installation = new ServerInstallation
        {
            ServerId = "test-server",
            AgentId = "agent1",
            AgentSpecificConfig = new Dictionary<string, string>(config)
        };

        // Act
        var result = _configurationService.DoesAgentConfigMatchGlobal(server, installation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void DoesAgentConfigMatchGlobal_DifferentConfigs_ReturnsFalse()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Configuration = new Dictionary<string, string> { ["key"] = "value1" }
        };
        var installation = new ServerInstallation
        {
            ServerId = "test-server",
            AgentId = "agent1",
            AgentSpecificConfig = new Dictionary<string, string> { ["key"] = "value2" }
        };

        // Act
        var result = _configurationService.DoesAgentConfigMatchGlobal(server, installation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task PropagateConfigurationUpdateAsync_MatchingConfig_UpdatesInstallation()
    {
        // Arrange
        var serverId = "test-server";
        var oldConfig = new Dictionary<string, string> { ["key"] = "old_value" };
        var newConfig = new Dictionary<string, string> { ["key"] = "new_value" };
        
        var installation = new ServerInstallation
        {
            Id = "install-1",
            ServerId = serverId,
            AgentId = "agent1",
            AgentSpecificConfig = new Dictionary<string, string>(oldConfig)
        };

        _mockInstallationManager
            .Setup(m => m.GetInstallationsByServerIdAsync(serverId))
            .ReturnsAsync(new[] { installation });

        _mockInstallationManager
            .Setup(m => m.UpdateInstallationConfigAsync(installation.Id, It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _configurationService.PropagateConfigurationUpdateAsync(serverId, oldConfig, newConfig);

        // Assert
        Assert.Single(result);
        Assert.Contains(installation.Id, result);
        _mockInstallationManager.Verify(
            m => m.UpdateInstallationConfigAsync(installation.Id, It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PropagateConfigurationUpdateAsync_NonMatchingConfig_DoesNotUpdate()
    {
        // Arrange
        var serverId = "test-server";
        var oldConfig = new Dictionary<string, string> { ["key"] = "old_value" };
        var newConfig = new Dictionary<string, string> { ["key"] = "new_value" };
        
        var installation = new ServerInstallation
        {
            Id = "install-1",
            ServerId = serverId,
            AgentId = "agent1",
            AgentSpecificConfig = new Dictionary<string, string> { ["key"] = "different_value" }
        };

        _mockInstallationManager
            .Setup(m => m.GetInstallationsByServerIdAsync(serverId))
            .ReturnsAsync(new[] { installation });

        // Act
        var result = await _configurationService.PropagateConfigurationUpdateAsync(serverId, oldConfig, newConfig);

        // Assert
        Assert.Empty(result);
        _mockInstallationManager.Verify(
            m => m.UpdateInstallationConfigAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public void ValidateConfiguration_ValidConfig_ReturnsValid()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var result = _configurationService.ValidateConfiguration(config);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateConfiguration_NullConfig_ReturnsInvalid()
    {
        // Act
        var result = _configurationService.ValidateConfiguration(null!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Configuration cannot be null", result.Errors);
    }

    [Fact]
    public void ValidateConfiguration_EmptyKey_ReturnsInvalid()
    {
        // Arrange
        var config = new Dictionary<string, string> { [""] = "value" };

        // Act
        var result = _configurationService.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("keys cannot be null or empty"));
    }

    [Fact]
    public void ValidateConfiguration_NullValue_ReturnsInvalid()
    {
        // Arrange
        var config = new Dictionary<string, string> { ["key"] = null! };

        // Act
        var result = _configurationService.ValidateConfiguration(config);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cannot be null"));
    }

    [Fact]
    public void SerializeConfiguration_ValidConfig_ReturnsJson()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        };

        // Act
        var result = _configurationService.SerializeConfiguration(config);

        // Assert
        Assert.Contains("key1", result);
        Assert.Contains("value1", result);
        Assert.Contains("key2", result);
        Assert.Contains("value2", result);
    }

    [Fact]
    public void SerializeConfiguration_EmptyConfig_ReturnsEmptyJson()
    {
        // Arrange
        var config = new Dictionary<string, string>();

        // Act
        var result = _configurationService.SerializeConfiguration(config);

        // Assert
        Assert.Equal("{}", result);
    }

    [Fact]
    public void DeserializeConfiguration_ValidJson_ReturnsConfig()
    {
        // Arrange
        var json = """{"key1":"value1","key2":"value2"}""";

        // Act
        var result = _configurationService.DeserializeConfiguration(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("value1", result["key1"]);
        Assert.Equal("value2", result["key2"]);
    }

    [Fact]
    public void DeserializeConfiguration_EmptyJson_ReturnsEmptyConfig()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _configurationService.DeserializeConfiguration(json);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void DeserializeConfiguration_InvalidJson_ReturnsNull()
    {
        // Arrange
        var json = "invalid json {";

        // Act
        var result = _configurationService.DeserializeConfiguration(json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializeConfiguration_EmptyString_ReturnsEmptyConfig()
    {
        // Act
        var result = _configurationService.DeserializeConfiguration("");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
