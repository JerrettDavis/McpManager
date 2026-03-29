using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace McpManager.Infrastructure.Import;

/// <summary>
/// Detects and imports MCP server configurations from installed AI agents.
/// Scans known config locations for Claude Desktop, Claude Code, Copilot,
/// OpenClaw, Codex, and Cursor, then allows selective import into a target agent.
/// </summary>
public class ConfigImporter : IConfigImporter
{
    private readonly IEnumerable<IAgentConnector> _connectors;

    public ConfigImporter(IEnumerable<IAgentConnector> connectors)
    {
        _connectors = connectors;
    }

    public async Task<IEnumerable<ImportSource>> DetectSourcesAsync()
    {
        var sources = new List<ImportSource>();

        foreach (var connector in _connectors)
        {
            var source = new ImportSource
            {
                AgentName = GetAgentDisplayName(connector.AgentType),
            };

            try
            {
                var isInstalled = await connector.IsAgentInstalledAsync();
                source.Detected = isInstalled;
                source.ConfigPath = await connector.GetConfigurationPathAsync();

                if (isInstalled)
                {
                    var configuredServers = await connector.GetConfiguredServersAsync();
                    source.Servers = configuredServers.Select(cs => ToImportableServer(cs)).ToList();
                }
            }
            catch
            {
                // If detection fails for one agent, continue with others.
                source.Detected = false;
            }

            sources.Add(source);
        }

        // Also scan Cursor config, which has no dedicated connector.
        var cursorSource = await DetectCursorSourceAsync();
        if (cursorSource != null)
        {
            sources.Add(cursorSource);
        }

        return sources;
    }

    public async Task<ImportResult> ImportAsync(IEnumerable<ImportableServer> servers, string targetAgent)
    {
        var serverList = servers.Where(s => s.Selected && !s.AlreadyManaged).ToList();
        var result = new ImportResult
        {
            TotalDetected = serverList.Count
        };

        var targetConnector = _connectors.FirstOrDefault(c =>
            string.Equals(GetAgentDisplayName(c.AgentType), targetAgent, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.AgentType.ToString(), targetAgent, StringComparison.OrdinalIgnoreCase));

        if (targetConnector == null)
        {
            result.Failed = serverList.Count;
            result.Details = serverList.Select(s => new ImportedServerResult
            {
                ServerName = s.Name,
                Success = false,
                Error = $"Target agent '{targetAgent}' not found."
            }).ToList();
            return result;
        }

        foreach (var server in serverList)
        {
            var detail = new ImportedServerResult
            {
                ServerName = server.Name,
                SourceAgent = string.Empty
            };

            try
            {
                var config = BuildConfigDictionary(server);
                var success = await targetConnector.AddServerToAgentAsync(server.Name, config);

                if (success)
                {
                    detail.Success = true;
                    result.Imported++;
                }
                else
                {
                    detail.Success = false;
                    detail.Error = "AddServerToAgentAsync returned false.";
                    result.Failed++;
                }
            }
            catch (Exception ex)
            {
                detail.Success = false;
                detail.Error = ex.Message;
                result.Failed++;
            }

            result.Details.Add(detail);
        }

        result.Skipped = result.TotalDetected - result.Imported - result.Failed;
        return result;
    }

    private static ImportableServer ToImportableServer(ConfiguredAgentServer configuredServer)
    {
        var rawConfig = configuredServer.RawConfig ?? new Dictionary<string, string>();

        var command = rawConfig.GetValueOrDefault("command", string.Empty);
        var argsRaw = rawConfig.GetValueOrDefault("args", string.Empty);
        var envRaw = rawConfig.GetValueOrDefault("env", string.Empty);

        var args = ParseJsonStringList(argsRaw);
        var env = ParseJsonStringDictionary(envRaw);

        return new ImportableServer
        {
            Name = configuredServer.ServerId,
            Command = command,
            Args = args,
            Env = env,
            AlreadyManaged = false,
            Selected = true
        };
    }

    private static Dictionary<string, string> BuildConfigDictionary(ImportableServer server)
    {
        var config = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(server.Command))
        {
            config["command"] = server.Command;
        }

        if (server.Args.Count > 0)
        {
            config["args"] = JsonSerializer.Serialize(server.Args);
        }

        if (server.Env.Count > 0)
        {
            config["env"] = JsonSerializer.Serialize(server.Env);
        }

        return config;
    }

    private static List<string> ParseJsonStringList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static Dictionary<string, string> ParseJsonStringDictionary(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(raw) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static async Task<ImportSource?> DetectCursorSourceAsync()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cursorConfigPath = Path.Combine(home, ".cursor", "mcp.json");

        if (!File.Exists(cursorConfigPath))
        {
            return null;
        }

        var source = new ImportSource
        {
            AgentName = "Cursor",
            ConfigPath = cursorConfigPath,
            Detected = true
        };

        try
        {
            var json = await File.ReadAllTextAsync(cursorConfigPath);
            var root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) as JsonObject;

            var mcpServers = root?["mcpServers"] as JsonObject;
            if (mcpServers == null)
            {
                return source;
            }

            foreach (var (name, serverNode) in mcpServers)
            {
                if (serverNode is not JsonObject serverObj)
                {
                    continue;
                }

                var command = serverObj["command"]?.GetValue<string>() ?? string.Empty;
                var args = new List<string>();
                if (serverObj["args"] is JsonArray argsArray)
                {
                    args = argsArray
                        .Select(a => a?.GetValue<string>() ?? string.Empty)
                        .Where(a => !string.IsNullOrEmpty(a))
                        .ToList();
                }

                var env = new Dictionary<string, string>();
                if (serverObj["env"] is JsonObject envObj)
                {
                    foreach (var (envKey, envValue) in envObj)
                    {
                        env[envKey] = envValue?.GetValue<string>() ?? string.Empty;
                    }
                }

                source.Servers.Add(new ImportableServer
                {
                    Name = name,
                    Command = command,
                    Args = args,
                    Env = env,
                    AlreadyManaged = false,
                    Selected = true
                });
            }
        }
        catch
        {
            // Malformed JSON — return detected but empty.
        }

        return source;
    }

    private static string GetAgentDisplayName(AgentType agentType)
    {
        return agentType switch
        {
            AgentType.Claude => "Claude Desktop",
            AgentType.ClaudeCode => "Claude Code",
            AgentType.GitHubCopilot => "GitHub Copilot",
            AgentType.OpenClaw => "OpenClaw",
            AgentType.OpenAICodex => "OpenAI Codex",
            _ => agentType.ToString()
        };
    }
}
