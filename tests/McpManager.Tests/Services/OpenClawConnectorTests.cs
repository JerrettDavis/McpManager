using McpManager.Core.Models;
using McpManager.Infrastructure.Connectors;
using System.Text.Json;
using Xunit;

namespace McpManager.Tests.Services;

public class OpenClawConnectorTests : IDisposable
{
    private readonly string _testRoot;
    private readonly Dictionary<string, string?> _environment;

    public OpenClawConnectorTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"mcpmanager-openclaw-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRoot);
        _environment = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["OPENCLAW_STATE_DIR"] = _testRoot
        };
    }

    [Fact]
    public void AgentType_IsOpenClaw()
    {
        var connector = CreateConnector();

        Assert.Equal(AgentType.OpenClaw, connector.AgentType);
    }

    [Fact]
    public async Task GetConfigurationPathAsync_UsesConfiguredStateDir()
    {
        var connector = CreateConnector();

        var configPath = await connector.GetConfigurationPathAsync();

        Assert.Equal(Path.Combine(_testRoot, "openclaw.json"), configPath);
    }

    [Fact]
    public async Task GetConfiguredServerIdsAsync_ReadsNestedMcpServers()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        var config = """
                     {
                       "mcp": {
                         "servers": {
                           "filesystem": {
                             "command": "npx"
                           },
                           "github": {
                             "url": "https://example.com/mcp"
                           }
                         }
                       }
                     }
                     """;

        await File.WriteAllTextAsync(configPath, config);

        var serverIds = (await connector.GetConfiguredServerIdsAsync()).ToList();

        Assert.Equal(2, serverIds.Count);
        Assert.Contains("filesystem", serverIds);
        Assert.Contains("github", serverIds);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_ReadsDisabledState()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "filesystem": { "command": "npx", "disabled": true },
                                                    "github": { "url": "https://example.com/mcp" }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServers = (await connector.GetConfiguredServersAsync()).ToList();

        Assert.Equal(2, configuredServers.Count);
        Assert.False(configuredServers.Single(server => server.ServerId == "filesystem").IsEnabled);
        Assert.True(configuredServers.Single(server => server.ServerId == "github").IsEnabled);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_ToleratesStringDisabledValues()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "filesystem": { "command": "npx", "disabled": "true" },
                                                    "github": { "url": "https://example.com/mcp", "disabled": "not-a-bool" }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServers = (await connector.GetConfiguredServersAsync()).ToList();

        Assert.Equal(2, configuredServers.Count);
        Assert.False(configuredServers.Single(server => server.ServerId == "filesystem").IsEnabled);
        Assert.True(configuredServers.Single(server => server.ServerId == "github").IsEnabled);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromAlias()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "my-fs": {
                                                      "command": "npx",
                                                      "args": ["-y", "@modelcontextprotocol/server-filesystem", "C:\\repos"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("my-fs", configuredServer.ConfiguredServerKey);
        Assert.Equal("filesystem", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetRuntimeCatalogAsync_ParsesLiveToolsCatalog()
    {
        var connector = CreateConnector(processRunner: (_, _, _) => Task.FromResult((
            0,
            """
            {
              "agentId": "jdai-default",
              "groups": [
                {
                  "id": "fs",
                  "label": "Files",
                  "source": "core",
                  "tools": [
                    {
                      "id": "read",
                      "label": "read",
                      "description": "Read file contents",
                      "source": "core"
                    }
                  ]
                },
                {
                  "id": "plugin:memory-core",
                  "label": "memory-core",
                  "source": "plugin",
                  "pluginId": "memory-core",
                  "tools": [
                    {
                      "id": "memory_search",
                      "label": "memory_search",
                      "description": "Semantic search",
                      "source": "plugin",
                      "pluginId": "memory-core",
                      "optional": true
                    }
                  ]
                }
              ]
            }
            """,
            string.Empty)));

        var runtimeCatalog = await connector.GetRuntimeCatalogAsync();

        Assert.NotNull(runtimeCatalog);
        Assert.Null(runtimeCatalog!.ErrorMessage);
        Assert.Equal("jdai-default", runtimeCatalog.AgentId);
        Assert.Equal(2, runtimeCatalog.Groups.Count);
        Assert.Equal("Files", runtimeCatalog.Groups.Single(group => group.Id == "fs").Label);

        var pluginTool = runtimeCatalog.Groups
            .Single(group => group.PluginId == "memory-core")
            .Tools
            .Single();
        Assert.Equal("memory_search", pluginTool.Id);
        Assert.True(pluginTool.Optional);
    }

    [Fact]
    public async Task GetRuntimeCatalogAsync_ReturnsErrorWhenCommandFails()
    {
        var connector = CreateConnector(processRunner: (_, _, _) => Task.FromResult((
            1,
            string.Empty,
            "gateway timeout after 3000ms")));

        var runtimeCatalog = await connector.GetRuntimeCatalogAsync();

        Assert.NotNull(runtimeCatalog);
        Assert.Contains("gateway timeout", runtimeCatalog!.ErrorMessage);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromNpmExec()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "fs-from-npm": {
                                                      "command": "npm",
                                                      "args": ["exec", "-y", "@modelcontextprotocol/server-filesystem", "C:\\repos"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("fs-from-npm", configuredServer.ConfiguredServerKey);
        Assert.Equal("filesystem", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromPnpmDlx()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "fs-from-pnpm": {
                                                      "command": "pnpm",
                                                      "args": ["dlx", "@modelcontextprotocol/server-filesystem", "C:\\repos"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("fs-from-pnpm", configuredServer.ConfiguredServerKey);
        Assert.Equal("filesystem", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromNpmExecPackageFlag()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "fs-from-package-flag": {
                                                      "command": "npm",
                                                      "args": ["exec", "--package=@modelcontextprotocol/server-filesystem", "--", "filesystem-server", "C:\\repos"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("fs-from-package-flag", configuredServer.ConfiguredServerKey);
        Assert.Equal("filesystem", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_FallsBackToConfiguredKeyForUnsupportedNpmCommand()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "npm-run-alias": {
                                                      "command": "npm",
                                                      "args": ["run", "my-mcp"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("npm-run-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("npm-run-alias", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_FallsBackToConfiguredKeyForUnsupportedPnpmCommand()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "pnpm-add-alias": {
                                                      "command": "pnpm",
                                                      "args": ["add", "@scope/pkg"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("pnpm-add-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("pnpm-add-alias", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_FallsBackToConfiguredKeyForPnpmExec()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "pnpm-exec-alias": {
                                                      "command": "pnpm",
                                                      "args": ["exec", "tsx", "server.ts"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("pnpm-exec-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("pnpm-exec-alias", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromUvxWithPythonFlag()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "uvx-alias": {
                                                      "command": "uvx",
                                                      "args": ["--python", "3.12", "@acme/mcp-server-github"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("uvx-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("github", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromPipxRun()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "pipx-alias": {
                                                      "command": "pipx",
                                                      "args": ["run", "@acme/mcp-server-github"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("pipx-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("github", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromPipxRunWithPythonFlag()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "pipx-python-alias": {
                                                      "command": "pipx",
                                                      "args": ["run", "--python", "3.12", "@acme/mcp-server-github"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("pipx-python-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("github", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_DerivesCanonicalServerIdFromNpxPackageFlag()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "npx-package-alias": {
                                                      "command": "npx",
                                                      "args": ["--package=@modelcontextprotocol/server-filesystem", "--", "filesystem-server", "C:\\repos"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("npx-package-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("filesystem", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServersAsync_StripsMcpServerPrefixFromPackageName()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "github-alias": {
                                                      "command": "npx",
                                                      "args": ["-y", "@acme/mcp-server-github"]
                                                    }
                                                  }
                                                }
                                              }
                                              """);

        var configuredServer = Assert.Single(await connector.GetConfiguredServersAsync());

        Assert.Equal("github-alias", configuredServer.ConfiguredServerKey);
        Assert.Equal("github", configuredServer.ServerId);
    }

    [Fact]
    public async Task GetConfiguredServerIdsAsync_ReturnsEmptyWhenMcpSectionIsNotAnObject()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": true
                                              }
                                              """);

        var serverIds = await connector.GetConfiguredServerIdsAsync();

        Assert.Empty(serverIds);
    }

    [Fact]
    public async Task AddServerToAgentAsync_WritesOpenClawMcpShape()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        var config = new Dictionary<string, string>
        {
            ["command"] = "npx",
            ["args"] = "[\"-y\",\"@modelcontextprotocol/server-filesystem\",\"C:\\\\repos\"]",
            ["env"] = "{\"API_KEY\":\"secret\"}"
        };

        var result = await connector.AddServerToAgentAsync("filesystem", config);

        Assert.True(result);
        Assert.True(File.Exists(configPath));

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var serverConfig = document.RootElement
            .GetProperty("mcp")
            .GetProperty("servers")
            .GetProperty("filesystem");

        Assert.Equal("npx", serverConfig.GetProperty("command").GetString());
        Assert.Equal("API_KEY", serverConfig.GetProperty("env").EnumerateObject().Single().Name);
        Assert.Equal("secret", serverConfig.GetProperty("env").GetProperty("API_KEY").GetString());
        Assert.Equal("-y", serverConfig.GetProperty("args")[0].GetString());
    }

    [Fact]
    public async Task AddServerToAgentAsync_ThrowsWhenConfigIsMissing()
    {
        var connector = CreateConnector();

        await Assert.ThrowsAsync<InvalidOperationException>(() => connector.AddServerToAgentAsync("filesystem"));
    }

    [Fact]
    public async Task AddServerToAgentAsync_ParsesQuotedArgsWithoutSplittingPaths()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        var config = new Dictionary<string, string>
        {
            ["command"] = "npx",
            ["args"] = "\"C:\\Program Files\\Repos\" --mode read-only"
        };

        await connector.AddServerToAgentAsync("filesystem", config);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var args = document.RootElement
            .GetProperty("mcp")
            .GetProperty("servers")
            .GetProperty("filesystem")
            .GetProperty("args");

        Assert.Equal("C:\\Program Files\\Repos", args[0].GetString());
        Assert.Equal("--mode", args[1].GetString());
        Assert.Equal("read-only", args[2].GetString());
    }

    [Fact]
    public async Task AddServerToAgentAsync_PreservesEmptyQuotedArgs()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        var config = new Dictionary<string, string>
        {
            ["command"] = "npx",
            ["args"] = "\"\" --flag"
        };

        await connector.AddServerToAgentAsync("filesystem", config);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var args = document.RootElement
            .GetProperty("mcp")
            .GetProperty("servers")
            .GetProperty("filesystem")
            .GetProperty("args");

        Assert.Equal(string.Empty, args[0].GetString());
        Assert.Equal("--flag", args[1].GetString());
    }

    [Fact]
    public async Task AddServerToAgentAsync_PreservesQuotedPathEndingWithBackslash()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        var config = new Dictionary<string, string>
        {
            ["command"] = "npx",
            ["args"] = "\"C:\\Repos\\\" --mode read-only"
        };

        await connector.AddServerToAgentAsync("filesystem", config);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var args = document.RootElement
            .GetProperty("mcp")
            .GetProperty("servers")
            .GetProperty("filesystem")
            .GetProperty("args");

        Assert.Equal("C:\\Repos\\", args[0].GetString());
        Assert.Equal("--mode", args[1].GetString());
        Assert.Equal("read-only", args[2].GetString());
    }

    [Fact]
    public async Task AddServerToAgentAsync_UsesScopedConfigKeyForSecondaryConfig()
    {
        var connector = CreateConnector();
        var primaryConfigPath = Path.Combine(_testRoot, "openclaw.json");
        var profileDirectory = Path.Combine(_testRoot, ".openclaw-dev");
        Directory.CreateDirectory(profileDirectory);
        var profileConfigPath = Path.Combine(profileDirectory, "openclaw.json");

        await File.WriteAllTextAsync(primaryConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "filesystem": { "command": "npx" }
                                                         }
                                                       }
                                                     }
                                                     """);

        var added = await connector.AddServerToAgentAsync(
            $"config:{profileConfigPath.Replace('\\', '/')}::github",
            new Dictionary<string, string>
            {
                ["url"] = "https://example.com/mcp"
            });

        Assert.True(added);

        using var primaryDocument = JsonDocument.Parse(await File.ReadAllTextAsync(primaryConfigPath));
        Assert.False(primaryDocument.RootElement.GetProperty("mcp").GetProperty("servers").TryGetProperty("github", out _));

        using var profileDocument = JsonDocument.Parse(await File.ReadAllTextAsync(profileConfigPath));
        var scopedServer = profileDocument.RootElement.GetProperty("mcp").GetProperty("servers").GetProperty("github");
        Assert.Equal("https://example.com/mcp", scopedServer.GetProperty("url").GetString());
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_RemovesOnlyRequestedServer()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "filesystem": { "command": "npx" },
                                                    "github": { "url": "https://example.com/mcp" }
                                                  }
                                                }
                                              }
                                              """);

        var removed = await connector.RemoveServerFromAgentAsync("filesystem");

        Assert.True(removed);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var servers = document.RootElement.GetProperty("mcp").GetProperty("servers");
        Assert.False(servers.TryGetProperty("filesystem", out _));
        Assert.True(servers.TryGetProperty("github", out _));
    }

    [Fact]
    public async Task SetServerEnabledAsync_SetsDisabledFlagWhenDisabled()
    {
        var connector = CreateConnector();
        var configPath = Path.Combine(_testRoot, "openclaw.json");

        await File.WriteAllTextAsync(configPath, """
                                              {
                                                "mcp": {
                                                  "servers": {
                                                    "filesystem": { "command": "npx" }
                                                  }
                                                }
                                              }
                                              """);

        var disabled = await connector.SetServerEnabledAsync("filesystem", enabled: false);
        var reenabled = await connector.SetServerEnabledAsync("filesystem", enabled: true);

        Assert.True(disabled);
        Assert.True(reenabled);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(configPath));
        var server = document.RootElement.GetProperty("mcp").GetProperty("servers").GetProperty("filesystem");
        Assert.False(server.TryGetProperty("disabled", out _));
    }

    [Fact]
    public async Task GetConfiguredServersAsync_AggregatesServersAcrossMultipleConfigFiles()
    {
        var connector = CreateConnector();
        var primaryConfigPath = Path.Combine(_testRoot, "openclaw.json");
        var legacyDirectory = Path.Combine(_testRoot, ".clawdbot");
        Directory.CreateDirectory(legacyDirectory);
        var legacyConfigPath = Path.Combine(legacyDirectory, "openclaw.json");

        await File.WriteAllTextAsync(primaryConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "filesystem": { "command": "npx" }
                                                         }
                                                       }
                                                     }
                                                     """);

        await File.WriteAllTextAsync(legacyConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "github-alias": {
                                                             "command": "npx",
                                                             "args": ["-y", "@acme/mcp-server-github"]
                                                           }
                                                         }
                                                       }
                                                     }
                                                     """);

        var configuredServers = (await connector.GetConfiguredServersAsync()).ToList();

        Assert.Equal(2, configuredServers.Count);

        var primaryServer = configuredServers.Single(server => server.ServerId == "filesystem");
        Assert.Equal("filesystem", primaryServer.ConfiguredServerKey);

        var scopedServer = configuredServers.Single(server => server.ServerId == "github");
        Assert.Equal($"config:{legacyConfigPath.Replace('\\', '/')}::github-alias", scopedServer.ConfiguredServerKey);
    }

    [Fact]
    public async Task RemoveServerFromAgentAsync_UsesScopedConfigKeyForSecondaryConfig()
    {
        var connector = CreateConnector();
        var primaryConfigPath = Path.Combine(_testRoot, "openclaw.json");
        var legacyDirectory = Path.Combine(_testRoot, ".clawdbot");
        Directory.CreateDirectory(legacyDirectory);
        var legacyConfigPath = Path.Combine(legacyDirectory, "openclaw.json");

        await File.WriteAllTextAsync(primaryConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "filesystem": { "command": "npx" }
                                                         }
                                                       }
                                                     }
                                                     """);

        await File.WriteAllTextAsync(legacyConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "github": { "url": "https://example.com/mcp" }
                                                         }
                                                       }
                                                     }
                                                     """);

        var removed = await connector.RemoveServerFromAgentAsync($"config:{legacyConfigPath.Replace('\\', '/')}::github");

        Assert.True(removed);

        using var primaryDocument = JsonDocument.Parse(await File.ReadAllTextAsync(primaryConfigPath));
        Assert.True(primaryDocument.RootElement.GetProperty("mcp").GetProperty("servers").TryGetProperty("filesystem", out _));

        using var legacyDocument = JsonDocument.Parse(await File.ReadAllTextAsync(legacyConfigPath));
        Assert.False(legacyDocument.RootElement.TryGetProperty("mcp", out _));
    }

    [Fact]
    public async Task SetServerEnabledAsync_UsesScopedConfigKeyForSecondaryConfig()
    {
        var connector = CreateConnector();
        var primaryConfigPath = Path.Combine(_testRoot, "openclaw.json");
        var profileDirectory = Path.Combine(_testRoot, ".openclaw-dev");
        Directory.CreateDirectory(profileDirectory);
        var profileConfigPath = Path.Combine(profileDirectory, "openclaw.json");

        await File.WriteAllTextAsync(primaryConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "filesystem": { "command": "npx" }
                                                         }
                                                       }
                                                     }
                                                     """);

        await File.WriteAllTextAsync(profileConfigPath, """
                                                     {
                                                       "mcp": {
                                                         "servers": {
                                                           "github": { "url": "https://example.com/mcp" }
                                                         }
                                                       }
                                                     }
                                                     """);

        var scopedKey = $"config:{profileConfigPath.Replace('\\', '/')}::github";

        Assert.True(await connector.SetServerEnabledAsync(scopedKey, enabled: false));
        Assert.True(await connector.SetServerEnabledAsync(scopedKey, enabled: true));

        using var primaryDocument = JsonDocument.Parse(await File.ReadAllTextAsync(primaryConfigPath));
        Assert.False(primaryDocument.RootElement.GetProperty("mcp").GetProperty("servers").GetProperty("filesystem").TryGetProperty("disabled", out _));

        using var profileDocument = JsonDocument.Parse(await File.ReadAllTextAsync(profileConfigPath));
        Assert.False(profileDocument.RootElement.GetProperty("mcp").GetProperty("servers").GetProperty("github").TryGetProperty("disabled", out _));
    }

    [Fact]
    public async Task IsAgentInstalledAsync_ReturnsTrueWhenStateDirectoryExists()
    {
        var connector = CreateConnector();

        var installed = await connector.IsAgentInstalledAsync();

        Assert.True(installed);
    }

    private OpenClawConnector CreateConnector(
        Func<string, string, CancellationToken, Task<(int ExitCode, string StandardOutput, string StandardError)>>? processRunner = null)
    {
        return new OpenClawConnector(
            key => _environment.TryGetValue(key, out var value) ? value : null,
            () => _testRoot,
            processRunner: processRunner);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
