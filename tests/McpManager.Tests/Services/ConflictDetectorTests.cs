using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;

namespace McpManager.Tests.Services;

public class ConflictDetectorTests
{
    private readonly Mock<IAgentManager> _mockAgentManager;
    private readonly Mock<IInstallationManager> _mockInstallationManager;
    private readonly ConflictDetector _detector;

    public ConflictDetectorTests()
    {
        _mockAgentManager = new Mock<IAgentManager>();
        _mockInstallationManager = new Mock<IInstallationManager>();
        _detector = new ConflictDetector(_mockAgentManager.Object, _mockInstallationManager.Object);
    }

    [Fact]
    public async Task DetectAllConflictsAsync_ReturnsEmpty_WhenNoAgentsDetected()
    {
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync())
            .ReturnsAsync(Array.Empty<Agent>());

        var conflicts = await _detector.DetectAllConflictsAsync();

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task DetectAllConflictsAsync_ReturnsEmpty_WhenSingleAgent()
    {
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem@1.0.0"]""" }
                }]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflicts = await _detector.DetectAllConflictsAsync();

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task DetectAllConflictsAsync_DetectsVersionMismatch_WhenSameServerDifferentArgs()
    {
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem@1.0.0"]""" }
                }]
            },
            new Agent
            {
                Id = "copilot", Name = "Copilot",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem@2.0.0"]""" }
                }]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflicts = await _detector.DetectAllConflictsAsync();

        Assert.Single(conflicts);
        Assert.Equal("filesystem", conflicts[0].ServerId);
        Assert.Equal(ConflictType.VersionMismatch, conflicts[0].Type);
        Assert.Equal(2, conflicts[0].Entries.Count);
    }

    [Fact]
    public async Task DetectAllConflictsAsync_ReturnsEmpty_WhenSameServerSameConfig()
    {
        var sharedConfig = new Dictionary<string, string>
        {
            ["command"] = "npx",
            ["args"] = """["-y","@modelcontextprotocol/server-filesystem@1.0.0"]"""
        };
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new(sharedConfig)
                }]
            },
            new Agent
            {
                Id = "copilot", Name = "Copilot",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new(sharedConfig)
                }]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflicts = await _detector.DetectAllConflictsAsync();

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task DetectAllConflictsAsync_DetectsConfigMismatch_WhenSameCommandDifferentEnv()
    {
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "postgres", ConfiguredServerKey = "postgres",
                    RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-postgres"]""", ["env"] = """{"DB_HOST":"localhost"}""" }
                }]
            },
            new Agent
            {
                Id = "copilot", Name = "Copilot",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "postgres", ConfiguredServerKey = "postgres",
                    RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-postgres"]""", ["env"] = """{"DB_HOST":"production.db"}""" }
                }]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflicts = await _detector.DetectAllConflictsAsync();

        Assert.Single(conflicts);
        Assert.Equal(ConflictType.ConfigMismatch, conflicts[0].Type);
    }

    [Fact]
    public async Task DetectAllConflictsAsync_DetectsDuplicate_WhenSameServerMultipleKeysInOneAgent()
    {
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers =
                [
                    new ConfiguredAgentServer
                    {
                        ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                        RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem","/home"]""" }
                    },
                    new ConfiguredAgentServer
                    {
                        ServerId = "filesystem", ConfiguredServerKey = "fs-work",
                        RawConfig = new() { ["command"] = "npx", ["args"] = """["-y","@modelcontextprotocol/server-filesystem","/work"]""" }
                    }
                ]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflicts = await _detector.DetectAllConflictsAsync();

        Assert.Single(conflicts);
        Assert.Equal(ConflictType.Duplicate, conflicts[0].Type);
        Assert.Equal(2, conflicts[0].Entries.Count);
    }

    [Fact]
    public async Task DetectConflictForServerAsync_ReturnsNull_WhenNoConflict()
    {
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new() { ["command"] = "npx" }
                }]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflict = await _detector.DetectConflictForServerAsync("filesystem");

        Assert.Null(conflict);
    }

    [Fact]
    public async Task DetectConflictForServerAsync_ReturnsConflict_WhenMismatchExists()
    {
        var agents = new[]
        {
            new Agent
            {
                Id = "claude", Name = "Claude",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new() { ["command"] = "npx", ["args"] = "v1" }
                }]
            },
            new Agent
            {
                Id = "copilot", Name = "Copilot",
                ConfiguredServers = [new ConfiguredAgentServer
                {
                    ServerId = "filesystem", ConfiguredServerKey = "filesystem",
                    RawConfig = new() { ["command"] = "npx", ["args"] = "v2" }
                }]
            }
        };
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync()).ReturnsAsync(agents);

        var conflict = await _detector.DetectConflictForServerAsync("filesystem");

        Assert.NotNull(conflict);
        Assert.Equal("filesystem", conflict.ServerId);
    }

    [Fact]
    public async Task DetectConflictForServerAsync_ReturnsNull_WhenServerNotFound()
    {
        _mockAgentManager.Setup(m => m.DetectInstalledAgentsAsync())
            .ReturnsAsync(Array.Empty<Agent>());

        var conflict = await _detector.DetectConflictForServerAsync("nonexistent");

        Assert.Null(conflict);
    }
}
