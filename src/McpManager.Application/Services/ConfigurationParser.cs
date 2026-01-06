using McpManager.Core.Models;
using System.Text.Json;

namespace McpManager.Application.Services;

/// <summary>
/// Service for parsing MCP server configurations from various agent-specific formats.
/// Supports Claude, Copilot, and Codex configuration formats.
/// </summary>
public class ConfigurationParser
{
    /// <summary>
    /// Attempts to parse a configuration string and convert it to a McpServer.
    /// Auto-detects the format (Claude, Copilot, or Codex).
    /// </summary>
    public (bool success, McpServer? server, string error) ParseConfiguration(string configText, string? serverId = null)
    {
        if (string.IsNullOrWhiteSpace(configText))
        {
            return (false, null, "Configuration text cannot be empty");
        }

        try
        {
            // Try to parse as JSON
            var jsonDoc = JsonDocument.Parse(configText);
            var root = jsonDoc.RootElement;

            // Detect if it's a full config (with mcpServers key) or a single server config
            if (root.TryGetProperty("mcpServers", out var mcpServersElement))
            {
                return ParseFullConfiguration(mcpServersElement);
            }
            else if (root.TryGetProperty("command", out _))
            {
                // Single server configuration (Codex-style)
                return ParseCodexServerConfig(root, serverId);
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                // Could be Claude/Copilot style (simple key-value)
                return ParseSimpleServerConfig(root, serverId);
            }

            return (false, null, "Unrecognized configuration format");
        }
        catch (JsonException ex)
        {
            return (false, null, $"Invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, null, $"Error parsing configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse a full configuration with multiple servers.
    /// Returns the first server found.
    /// </summary>
    private (bool success, McpServer? server, string error) ParseFullConfiguration(JsonElement mcpServers)
    {
        if (mcpServers.ValueKind != JsonValueKind.Object)
        {
            return (false, null, "mcpServers must be an object");
        }

        var enumerator = mcpServers.EnumerateObject();
        if (!enumerator.MoveNext())
        {
            return (false, null, "No servers found in configuration");
        }

        var firstServer = enumerator.Current;
        var serverId = firstServer.Name;
        var serverConfig = firstServer.Value;

        if (serverConfig.TryGetProperty("command", out _))
        {
            return ParseCodexServerConfig(serverConfig, serverId);
        }
        else
        {
            return ParseSimpleServerConfig(serverConfig, serverId);
        }
    }

    /// <summary>
    /// Parse Codex-style configuration with command, args, env.
    /// </summary>
    private (bool success, McpServer? server, string error) ParseCodexServerConfig(JsonElement config, string? serverId)
    {
        try
        {
            var server = new McpServer
            {
                Id = serverId ?? GenerateServerId(),
                Name = serverId ?? "Custom MCP Server",
                Description = "Custom MCP Server",
                Version = "1.0.0",
                Author = "Custom",
                Configuration = new Dictionary<string, string>()
            };

            if (config.TryGetProperty("command", out var command))
            {
                server.Configuration["command"] = command.GetString() ?? "";
            }

            if (config.TryGetProperty("args", out var args))
            {
                if (args.ValueKind == JsonValueKind.Array)
                {
                    var argsList = new List<string>();
                    foreach (var arg in args.EnumerateArray())
                    {
                        argsList.Add(arg.GetString() ?? "");
                    }
                    server.Configuration["args"] = string.Join(" ", argsList);
                }
                else
                {
                    server.Configuration["args"] = args.GetString() ?? "";
                }
            }

            if (config.TryGetProperty("env", out var env))
            {
                server.Configuration["env"] = env.GetRawText();
            }

            if (config.TryGetProperty("enabled", out var enabled))
            {
                if (enabled.ValueKind == JsonValueKind.True || enabled.ValueKind == JsonValueKind.False)
                {
                    server.Configuration["enabled"] = enabled.GetBoolean().ToString().ToLower();
                }
                else if (enabled.ValueKind == JsonValueKind.String)
                {
                    server.Configuration["enabled"] = enabled.GetString() ?? "";
                }
            }

            // Copy any other properties as well
            foreach (var property in config.EnumerateObject())
            {
                if (!server.Configuration.ContainsKey(property.Name))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        server.Configuration[property.Name] = property.Value.GetString() ?? "";
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Array || 
                             property.Value.ValueKind == JsonValueKind.Object)
                    {
                        server.Configuration[property.Name] = property.Value.GetRawText();
                    }
                    else
                    {
                        server.Configuration[property.Name] = property.Value.ToString();
                    }
                }
            }

            return (true, server, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error parsing Codex configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse simple key-value style configuration (Claude/Copilot).
    /// </summary>
    private (bool success, McpServer? server, string error) ParseSimpleServerConfig(JsonElement config, string? serverId)
    {
        try
        {
            var server = new McpServer
            {
                Id = serverId ?? GenerateServerId(),
                Name = serverId ?? "Custom MCP Server",
                Description = "Custom MCP Server",
                Version = "1.0.0",
                Author = "Custom",
                Configuration = new Dictionary<string, string>()
            };

            foreach (var property in config.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    server.Configuration[property.Name] = property.Value.GetString() ?? "";
                }
                else if (property.Value.ValueKind == JsonValueKind.Array || 
                         property.Value.ValueKind == JsonValueKind.Object)
                {
                    server.Configuration[property.Name] = property.Value.GetRawText();
                }
                else
                {
                    server.Configuration[property.Name] = property.Value.ToString();
                }
            }

            return (true, server, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error parsing simple configuration: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a server from manual form input.
    /// </summary>
    public McpServer CreateServerFromManualInput(
        string id,
        string name,
        string description,
        string command,
        string args,
        Dictionary<string, string>? envVars = null,
        string? version = null,
        string? author = null)
    {
        var server = new McpServer
        {
            Id = string.IsNullOrWhiteSpace(id) ? GenerateServerId() : id,
            Name = name,
            Description = description,
            Version = version ?? "1.0.0",
            Author = author ?? "Custom",
            Configuration = new Dictionary<string, string>
            {
                ["command"] = command,
                ["args"] = args
            }
        };

        if (envVars != null && envVars.Any())
        {
            server.Configuration["env"] = JsonSerializer.Serialize(envVars);
        }

        return server;
    }

    private static string GenerateServerId()
    {
        return $"custom-{Guid.NewGuid():N}".Substring(0, 20);
    }
}
