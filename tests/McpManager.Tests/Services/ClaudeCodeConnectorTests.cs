using McpManager.Core.Models;
using McpManager.Infrastructure.Connectors;

namespace McpManager.Tests.Services;

public class ClaudeCodeConnectorTests : IDisposable
{
    private readonly string _testHome;

    public ClaudeCodeConnectorTests()
    {
        _testHome = Path.Combine(Path.GetTempPath(), $"mcpmanager-claudecode-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testHome);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_AggregatesUserAndAllProjectServers()
    {
        var configPath = Path.Combine(_testHome, ".claude.json");
        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcpServers": {
                                                  "github": {
                                                    "type": "http",
                                                    "url": "https://api.githubcopilot.com/mcp/"
                                                  }
                                                },
                                                "projects": {
                                                  "C:\\git\\JD.Efcpt.Build": {
                                                    "mcpServers": {
                                                      "patternkit_docs": {
                                                        "type": "http",
                                                        "url": "https://patternkit.local/mcp",
                                                        "disabled": true
                                                      }
                                                    }
                                                  },
                                                  "C:/git/JD.Efcpt.Build": {
                                                    "mcpServers": {
                                                      "jd_efcpt_build": {
                                                        "type": "stdio",
                                                        "command": "npx",
                                                        "args": ["-y", "jd-efcpt-build"]
                                                      },
                                                      "patternkit": {
                                                        "type": "stdio",
                                                        "command": "npx",
                                                        "args": ["-y", "patternkit"]
                                                      },
                                                      "tinybdd": {
                                                        "type": "stdio",
                                                        "command": "npx",
                                                        "args": ["-y", "tinybdd"]
                                                      }
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var connector = CreateConnector();

        var configuredServers = (await connector.GetConfiguredServersAsync())
            .OrderBy(server => server.ConfiguredServerKey)
            .ToList();

        Assert.Equal(5, configuredServers.Count);
        Assert.Contains(configuredServers, server => server.ServerId == "github" && server.RawConfig["url"] == "https://api.githubcopilot.com/mcp/");
        Assert.Contains(configuredServers, server => server.ServerId == "patternkit_docs" && !server.IsEnabled &&
            server.ConfiguredServerKey.StartsWith("project:C:/git/JD.Efcpt.Build::", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(configuredServers, server => server.ConfiguredServerKey == "project:C:/git/JD.Efcpt.Build::jd_efcpt_build" && server.ServerId == "jd_efcpt_build");
        Assert.Contains(configuredServers, server => server.ConfiguredServerKey == "project:C:/git/JD.Efcpt.Build::patternkit" && server.ServerId == "patternkit");
        Assert.Contains(configuredServers, server => server.ConfiguredServerKey == "project:C:/git/JD.Efcpt.Build::tinybdd" && server.ServerId == "tinybdd");
    }

    [Fact]
    public async Task GetConfigurationPathAsync_PrefersUserConfig()
    {
        var configPath = Path.Combine(_testHome, ".claude.json");
        await File.WriteAllTextAsync(configPath, "{}");

        var connector = CreateConnector();

        var resolvedPath = await connector.GetConfigurationPathAsync();

        Assert.Equal(configPath, resolvedPath);
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_RemovesProjectScopedServer()
    {
        var configPath = Path.Combine(_testHome, ".claude.json");
        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "projects": {
                                                  "C:/git/JD.Efcpt.Build": {
                                                    "mcpServers": {
                                                      "tinybdd": {
                                                        "type": "stdio",
                                                        "command": "npx"
                                                      }
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var connector = CreateConnector();

        var removed = await connector.RemoveServerFromAgentAsync("project:C:\\git\\JD.Efcpt.Build::tinybdd");
        var updatedJson = await File.ReadAllTextAsync(configPath);

        Assert.True(removed);
        Assert.DoesNotContain("tinybdd", updatedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SetServerEnabledAsync_UpdatesProjectScopedServer()
    {
        var configPath = Path.Combine(_testHome, ".claude.json");
        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "projects": {
                                                  "C:/git/JD.Efcpt.Build": {
                                                    "mcpServers": {
                                                      "tinybdd": {
                                                        "type": "stdio",
                                                        "command": "npx"
                                                      }
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var connector = CreateConnector();

        var updated = await connector.SetServerEnabledAsync("project:C:\\git\\JD.Efcpt.Build::tinybdd", false);
        var updatedJson = await File.ReadAllTextAsync(configPath);

        Assert.True(updated);
        Assert.Contains(@"""disabled"": true", updatedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_PrefersUserConfigOverLegacySettingsForSameServerId()
    {
        var settingsDirectory = Path.Combine(_testHome, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        await File.WriteAllTextAsync(Path.Combine(settingsDirectory, "settings.json"), """
                                                                                     {
                                                                                       "mcpServers": {
                                                                                         "github": {
                                                                                           "command": "legacy-command",
                                                                                           "args": ["legacy-arg"]
                                                                                         }
                                                                                       }
                                                                                     }
                                                                                     """);
        await File.WriteAllTextAsync(Path.Combine(_testHome, ".claude.json"), """
                                                                    {
                                                                      "mcpServers": {
                                                                        "github": {
                                                                          "type": "http",
                                                                          "url": "https://api.githubcopilot.com/mcp/"
                                                                        }
                                                                      }
                                                                    }
                                                                    """);

        var connector = CreateConnector();

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("github", configuredServer.ServerId);
        Assert.Equal("https://api.githubcopilot.com/mcp/", configuredServer.RawConfig["url"]);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_ReadsLegacySettingsMcpServers()
    {
        var settingsDirectory = Path.Combine(_testHome, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        await File.WriteAllTextAsync(Path.Combine(settingsDirectory, "settings.json"), """
                                                                                     {
                                                                                       "mcpServers": {
                                                                                         "legacy-server": {
                                                                                           "command": "npx",
                                                                                           "args": ["-y", "legacy-server"],
                                                                                           "disabled": true
                                                                                         }
                                                                                       }
                                                                                     }
                                                                                     """);

        var connector = CreateConnector();

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("legacy-server", configuredServer.ServerId);
        Assert.False(configuredServer.IsEnabled);
        Assert.Equal("npx", configuredServer.RawConfig["command"]);
    }

    [Fact]
    public async Task SetServerEnabledAsync_FallsBackToLegacySettingsWhenUserConfigIsInvalid()
    {
        await File.WriteAllTextAsync(Path.Combine(_testHome, ".claude.json"), "{ invalid json");

        var settingsDirectory = Path.Combine(_testHome, ".claude");
        Directory.CreateDirectory(settingsDirectory);
        var settingsPath = Path.Combine(settingsDirectory, "settings.json");
        await File.WriteAllTextAsync(settingsPath, """
                                                    {
                                                      "mcpServers": {
                                                        "legacy-server": {
                                                          "command": "npx",
                                                          "args": ["-y", "legacy-server"]
                                                        }
                                                      }
                                                    }
                                                    """);

        var connector = CreateConnector();

        var updated = await connector.SetServerEnabledAsync("legacy-server", false);
        var updatedJson = await File.ReadAllTextAsync(settingsPath);

        Assert.True(updated);
        Assert.Contains(@"""disabled"": true", updatedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddServerToAgentAsync_PreservesJsonArgsArrayWhenWritingUserConfig()
    {
        await File.WriteAllTextAsync(Path.Combine(_testHome, ".claude.json"), "{}");
        var connector = CreateConnector();

        await connector.AddServerToAgentAsync("tinybdd", new Dictionary<string, string>
        {
            ["command"] = "npx",
            ["args"] = """["-y","tinybdd"]"""
        });

        var updatedJson = await File.ReadAllTextAsync(Path.Combine(_testHome, ".claude.json"));

        Assert.Contains(@"""args"": [", updatedJson, StringComparison.Ordinal);
        Assert.Contains(@"""-y""", updatedJson, StringComparison.Ordinal);
        Assert.Contains(@"""tinybdd""", updatedJson, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentType_IsClaudeCode()
    {
        var connector = CreateConnector();

        Assert.Equal(AgentType.ClaudeCode, connector.AgentType);
    }

    private ClaudeCodeConnector CreateConnector()
    {
        return new ClaudeCodeConnector(
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
