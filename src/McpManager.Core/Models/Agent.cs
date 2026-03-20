namespace McpManager.Core.Models;

/// <summary>
/// Represents an AI agent (Claude, Copilot, etc.) that can use MCP servers.
/// </summary>
public class Agent
{
    private List<ConfiguredAgentServer> _configuredServers = [];

    /// <summary>
    /// Unique identifier for the agent.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the agent (e.g., "Claude Desktop", "GitHub Copilot").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type/category of the agent.
    /// </summary>
    public AgentType Type { get; set; }

    /// <summary>
    /// Whether the agent is currently detected/installed on the system.
    /// </summary>
    public bool IsDetected { get; set; }

    /// <summary>
    /// Path to the agent's configuration file.
    /// </summary>
    public string ConfigPath { get; set; } = string.Empty;

    /// <summary>
    /// Optional read-only live runtime catalog for agents that expose one.
    /// </summary>
    public AgentRuntimeCatalog? RuntimeCatalog { get; set; }

    /// <summary>
    /// MCP servers currently configured for this agent, including enabled state.
    /// </summary>
    public List<ConfiguredAgentServer> ConfiguredServers
    {
        get => _configuredServers;
        set => _configuredServers = value ?? [];
    }

    /// <summary>
    /// List of MCP server IDs currently configured for this agent.
    /// </summary>
    public List<string> ConfiguredServerIds
    {
        get => _configuredServers
            .Select(server => string.IsNullOrWhiteSpace(server.ConfiguredServerKey) ? server.ServerId : server.ConfiguredServerKey)
            .ToList();
        set => _configuredServers = value?
            .Select(serverId => new ConfiguredAgentServer
            {
                ConfiguredServerKey = serverId,
                ServerId = serverId,
                IsEnabled = true
            })
            .ToList() ?? [];
    }
}

/// <summary>
/// Represents a configured MCP server entry from an agent configuration file.
/// </summary>
public class ConfiguredAgentServer
{
    public string ConfiguredServerKey { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, string> RawConfig { get; set; } = new();
}

/// <summary>
/// Represents a read-only runtime catalog exposed by a live agent backend.
/// </summary>
public class AgentRuntimeCatalog
{
    public string AgentId { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public List<AgentRuntimeGroup> Groups { get; set; } = [];
}

/// <summary>
/// Represents a group of runtime tools.
/// </summary>
public class AgentRuntimeGroup
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? PluginId { get; set; }
    public List<AgentRuntimeTool> Tools { get; set; } = [];
}

/// <summary>
/// Represents a single live runtime tool entry.
/// </summary>
public class AgentRuntimeTool
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? PluginId { get; set; }
    public bool Optional { get; set; }
}

/// <summary>
/// Defines the types of AI agents supported by the system.
/// </summary>
public enum AgentType
{
    Claude,
    GitHubCopilot,
    OpenAICodex,
    ClaudeCode,
    OpenClaw,
    Other
}
