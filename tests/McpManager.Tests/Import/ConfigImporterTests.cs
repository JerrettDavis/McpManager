using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using McpManager.Infrastructure.Import;
using Moq;

namespace McpManager.Tests.Import;

public class ConfigImporterTests : IDisposable
{
    private readonly string _testHome;

    public ConfigImporterTests()
    {
        _testHome = Path.Combine(Path.GetTempPath(), $"mcpmanager-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testHome);
    }

    [Fact]
    public async Task DetectSources_FindsClaudeConfigInTempDir()
    {
        var claudeConnector = CreateMockConnector(
            AgentType.Claude,
            isInstalled: true,
            configPath: Path.Combine(_testHome, "claude_desktop_config.json"),
            servers:
            [
                new ConfiguredAgentServer
                {
                    ConfiguredServerKey = "test-server",
                    ServerId = "test-server",
                    IsEnabled = true,
                    RawConfig = new Dictionary<string, string>
                    {
                        ["command"] = "npx",
                        ["args"] = """["-y","@test/mcp-server"]"""
                    }
                }
            ]);

        var importer = new ConfigImporter([claudeConnector.Object]);

        var sources = (await importer.DetectSourcesAsync()).ToList();

        Assert.Single(sources);
        Assert.True(sources[0].Detected);
        Assert.Equal("Claude Desktop", sources[0].AgentName);
        Assert.Single(sources[0].Servers);
        Assert.Equal("test-server", sources[0].Servers[0].Name);
        Assert.Equal("npx", sources[0].Servers[0].Command);
    }

    [Fact]
    public async Task DetectSources_ReturnsEmptyWhenNoConfigsExist()
    {
        var claudeConnector = CreateMockConnector(AgentType.Claude, isInstalled: false);
        var copilotConnector = CreateMockConnector(AgentType.GitHubCopilot, isInstalled: false);

        var importer = new ConfigImporter([claudeConnector.Object, copilotConnector.Object]);

        var sources = (await importer.DetectSourcesAsync()).ToList();

        Assert.Equal(2, sources.Count);
        Assert.All(sources, s => Assert.False(s.Detected));
        Assert.All(sources, s => Assert.Empty(s.Servers));
    }

    [Fact]
    public async Task DetectSources_ParsesClaudeDesktopConfigFormatCorrectly()
    {
        var servers = new List<ConfiguredAgentServer>
        {
            new()
            {
                ConfiguredServerKey = "filesystem",
                ServerId = "filesystem",
                IsEnabled = true,
                RawConfig = new Dictionary<string, string>
                {
                    ["command"] = "npx",
                    ["args"] = """["-y","@modelcontextprotocol/server-filesystem","/home/user"]""",
                    ["env"] = """{"NODE_ENV":"production"}"""
                }
            },
            new()
            {
                ConfiguredServerKey = "github",
                ServerId = "github",
                IsEnabled = true,
                RawConfig = new Dictionary<string, string>
                {
                    ["command"] = "npx",
                    ["args"] = """["-y","@modelcontextprotocol/server-github"]""",
                    ["env"] = """{"GITHUB_TOKEN":"ghp_xxx"}"""
                }
            }
        };

        var connector = CreateMockConnector(
            AgentType.Claude,
            isInstalled: true,
            configPath: Path.Combine(_testHome, "config.json"),
            servers: servers);

        var importer = new ConfigImporter([connector.Object]);
        var sources = (await importer.DetectSourcesAsync()).ToList();

        Assert.Single(sources);
        Assert.Equal(2, sources[0].Servers.Count);

        var fsServer = sources[0].Servers.First(s => s.Name == "filesystem");
        Assert.Equal("npx", fsServer.Command);
        Assert.Contains("-y", fsServer.Args);
        Assert.Contains("@modelcontextprotocol/server-filesystem", fsServer.Args);
        Assert.Equal("production", fsServer.Env["NODE_ENV"]);

        var ghServer = sources[0].Servers.First(s => s.Name == "github");
        Assert.Equal("ghp_xxx", ghServer.Env["GITHUB_TOKEN"]);
    }

    [Fact]
    public async Task DetectSources_ParsesClaudeCodeConfigFormatCorrectly()
    {
        var servers = new List<ConfiguredAgentServer>
        {
            new()
            {
                ConfiguredServerKey = "context7",
                ServerId = "context7",
                IsEnabled = true,
                RawConfig = new Dictionary<string, string>
                {
                    ["type"] = "stdio",
                    ["command"] = "npx",
                    ["args"] = """["-y","@upstash/context7-mcp"]"""
                }
            }
        };

        var connector = CreateMockConnector(
            AgentType.ClaudeCode,
            isInstalled: true,
            configPath: Path.Combine(_testHome, ".claude.json"),
            servers: servers);

        var importer = new ConfigImporter([connector.Object]);
        var sources = (await importer.DetectSourcesAsync()).ToList();

        Assert.Single(sources);
        Assert.Equal("Claude Code", sources[0].AgentName);
        Assert.Single(sources[0].Servers);
        Assert.Equal("context7", sources[0].Servers[0].Name);
    }

    [Fact]
    public async Task DetectSources_HandlesMalformedConnectorGracefully()
    {
        var faultyConnector = new Mock<IAgentConnector>();
        faultyConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        faultyConnector.Setup(c => c.IsAgentInstalledAsync()).ThrowsAsync(new IOException("disk error"));

        var healthyConnector = CreateMockConnector(AgentType.GitHubCopilot, isInstalled: true);

        var importer = new ConfigImporter([faultyConnector.Object, healthyConnector.Object]);

        var sources = (await importer.DetectSourcesAsync()).ToList();

        Assert.Equal(2, sources.Count);
        Assert.False(sources[0].Detected);
        Assert.True(sources[1].Detected);
    }

    [Fact]
    public async Task ImportAsync_ImportsSelectedServersToTargetAgent()
    {
        var targetConnector = new Mock<IAgentConnector>();
        targetConnector.Setup(c => c.AgentType).Returns(AgentType.ClaudeCode);
        targetConnector
            .Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        var importer = new ConfigImporter([targetConnector.Object]);

        var servers = new List<ImportableServer>
        {
            new()
            {
                Name = "test-server",
                Command = "npx",
                Args = ["-y", "@test/mcp-server"],
                Env = new Dictionary<string, string> { ["API_KEY"] = "abc" },
                Selected = true,
                AlreadyManaged = false
            },
            new()
            {
                Name = "skipped-server",
                Command = "node",
                Args = ["server.js"],
                Selected = false,
                AlreadyManaged = false
            }
        };

        var result = await importer.ImportAsync(servers, "Claude Code");

        Assert.Equal(1, result.TotalDetected);
        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.Single(result.Details);
        Assert.True(result.Details[0].Success);
        Assert.Equal("test-server", result.Details[0].ServerName);

        targetConnector.Verify(
            c => c.AddServerToAgentAsync("test-server", It.IsAny<Dictionary<string, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ImportAsync_SkipsAlreadyManagedServers()
    {
        var targetConnector = new Mock<IAgentConnector>();
        targetConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        targetConnector
            .Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(true);

        var importer = new ConfigImporter([targetConnector.Object]);

        var servers = new List<ImportableServer>
        {
            new()
            {
                Name = "managed-server",
                Command = "npx",
                Args = ["-y", "@test/server"],
                Selected = true,
                AlreadyManaged = true
            }
        };

        var result = await importer.ImportAsync(servers, "Claude Desktop");

        Assert.Equal(0, result.TotalDetected);
        Assert.Equal(0, result.Imported);
        targetConnector.Verify(
            c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportAsync_ReportsFailureWhenTargetAgentNotFound()
    {
        var importer = new ConfigImporter([]);

        var servers = new List<ImportableServer>
        {
            new()
            {
                Name = "test-server",
                Command = "npx",
                Selected = true,
                AlreadyManaged = false
            }
        };

        var result = await importer.ImportAsync(servers, "NonExistentAgent");

        Assert.Equal(1, result.TotalDetected);
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Contains("not found", result.Details[0].Error);
    }

    [Fact]
    public async Task ImportAsync_HandlesAddServerFailureGracefully()
    {
        var targetConnector = new Mock<IAgentConnector>();
        targetConnector.Setup(c => c.AgentType).Returns(AgentType.Claude);
        targetConnector
            .Setup(c => c.AddServerToAgentAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .ThrowsAsync(new InvalidOperationException("write failed"));

        var importer = new ConfigImporter([targetConnector.Object]);

        var servers = new List<ImportableServer>
        {
            new()
            {
                Name = "failing-server",
                Command = "npx",
                Selected = true,
                AlreadyManaged = false
            }
        };

        var result = await importer.ImportAsync(servers, "Claude Desktop");

        Assert.Equal(1, result.TotalDetected);
        Assert.Equal(0, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.False(result.Details[0].Success);
        Assert.Contains("write failed", result.Details[0].Error);
    }

    private static Mock<IAgentConnector> CreateMockConnector(
        AgentType agentType,
        bool isInstalled,
        string? configPath = null,
        IEnumerable<ConfiguredAgentServer>? servers = null)
    {
        var mock = new Mock<IAgentConnector>();
        mock.Setup(c => c.AgentType).Returns(agentType);
        mock.Setup(c => c.IsAgentInstalledAsync()).ReturnsAsync(isInstalled);
        mock.Setup(c => c.GetConfigurationPathAsync()).ReturnsAsync(configPath ?? string.Empty);
        mock.Setup(c => c.GetConfiguredServersAsync()).ReturnsAsync(servers ?? []);
        return mock;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testHome))
            {
                Directory.Delete(_testHome, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
