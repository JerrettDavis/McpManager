using McpManager.Core.Models;
using McpManager.Infrastructure.Connectors;

namespace McpManager.Tests.Services;

public class CopilotConnectorTests : IDisposable
{
    private readonly string _testHome;

    public CopilotConnectorTests()
    {
        _testHome = Path.Combine(Path.GetTempPath(), $"mcpmanager-copilot-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testHome);
    }

    [Fact]
    public async Task GetConfigurationPathAsync_UsesCopilotConfigLocation()
    {
        var connector = CreateConnector();

        var configPath = await connector.GetConfigurationPathAsync();

        Assert.Equal(Path.Combine(_testHome, ".copilot", "mcp-config.json"), configPath);
    }

    [Fact]
    public async Task IsAgentInstalledAsync_ReturnsTrueWhenCopilotDirectoryExists()
    {
        Directory.CreateDirectory(Path.Combine(_testHome, ".copilot"));
        var connector = CreateConnector();

        var installed = await connector.IsAgentInstalledAsync();

        Assert.True(installed);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_ReadsConfiguredServersFromCopilotConfig()
    {
        var copilotDirectory = Path.Combine(_testHome, ".copilot");
        Directory.CreateDirectory(copilotDirectory);
        await File.WriteAllTextAsync(Path.Combine(copilotDirectory, "mcp-config.json"), """
                                                                                    {
                                                                                      "mcpServers": {
                                                                                        "context7": {
                                                                                          "type": "http",
                                                                                          "url": "https://mcp.context7.com/mcp"
                                                                                        },
                                                                                        "local-server": {
                                                                                          "command": "node",
                                                                                          "args": ["server.js"],
                                                                                          "enabled": false
                                                                                        },
                                                                                        "disabled-raw": false,
                                                                                        "array-raw": ["a", "b"]
                                                                                      }
                                                                                    }
                                                                                    """);

        var connector = CreateConnector();

        var configuredServers = (await connector.GetConfiguredServersAsync())
            .OrderBy(server => server.ServerId)
            .ToList();

        Assert.Equal(4, configuredServers.Count);
        Assert.Contains(configuredServers, server => server.ServerId == "context7" && server.IsEnabled);
        Assert.Contains(configuredServers, server => server.ServerId == "local-server" && !server.IsEnabled && server.RawConfig["args"] == """["server.js"]""");
        Assert.Contains(configuredServers, server => server.ServerId == "disabled-raw" && !server.IsEnabled && server.RawConfig["$raw"] == "false");
        Assert.Contains(configuredServers, server => server.ServerId == "array-raw" && server.IsEnabled && server.RawConfig["$raw"] == """["a", "b"]""");
    }

    [Fact]
    public async Task SetServerEnabledAsync_UpdatesBooleanOnlyEntries()
    {
        var copilotDirectory = Path.Combine(_testHome, ".copilot");
        Directory.CreateDirectory(copilotDirectory);
        var configPath = Path.Combine(copilotDirectory, "mcp-config.json");
        await File.WriteAllTextAsync(configPath, """
                                                                                    {
                                                                                      "mcpServers": {
                                                                                        "disabled-raw": false
                                                                                      }
                                                                                    }
                                                                                    """);

        var connector = CreateConnector();

        var updated = await connector.SetServerEnabledAsync("disabled-raw", true);
        var updatedJson = await File.ReadAllTextAsync(configPath);

        Assert.True(updated);
        Assert.Contains(@"""disabled-raw"": true", updatedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentType_IsGitHubCopilot()
    {
        var connector = CreateConnector();

        Assert.Equal(AgentType.GitHubCopilot, connector.AgentType);
    }

    private CopilotConnector CreateConnector()
    {
        return new CopilotConnector(
            homeDirectoryResolver: () => _testHome,
            fileExists: File.Exists,
            directoryExists: Directory.Exists,
            readAllTextAsync: path => File.ReadAllTextAsync(path),
            writeAllTextAsync: (path, content) => File.WriteAllTextAsync(path, content));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testHome))
        {
            Directory.Delete(_testHome, recursive: true);
        }
    }
}
