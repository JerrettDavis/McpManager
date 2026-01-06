# API Documentation

## Core Interfaces

### IServerManager

Manages MCP server lifecycle and configuration.

**Methods:**

```csharp
Task<IEnumerable<McpServer>> GetInstalledServersAsync()
```
Returns all locally installed MCP servers.

```csharp
Task<McpServer?> GetServerByIdAsync(string serverId)
```
Gets a specific server by ID, returns null if not found.

```csharp
Task<bool> InstallServerAsync(McpServer server)
```
Installs an MCP server locally. Returns false if server already exists.

```csharp
Task<bool> UninstallServerAsync(string serverId)
```
Uninstalls a server. Returns false if server doesn't exist.

```csharp
Task<bool> UpdateServerConfigurationAsync(string serverId, Dictionary<string, string> configuration)
```
Updates server configuration. Returns false if server doesn't exist.

### IAgentManager

Manages AI agent detection and configuration.

**Methods:**

```csharp
Task<IEnumerable<Agent>> DetectInstalledAgentsAsync()
```
Detects all installed AI agents on the system. Checks Claude Desktop, GitHub Copilot, etc.

```csharp
Task<Agent?> GetAgentByIdAsync(string agentId)
```
Gets a specific agent by ID. Returns null if not found.

```csharp
Task<IEnumerable<string>> GetAgentServerIdsAsync(string agentId)
```
Returns list of server IDs configured for the specified agent.

### IAgentConnector

Interface for agent-specific implementations. Implement this to add support for new agents.

**Properties:**

```csharp
AgentType AgentType { get; }
```
The type of agent this connector supports.

**Methods:**

```csharp
Task<bool> IsAgentInstalledAsync()
```
Detects if this agent is installed on the system.

```csharp
Task<string> GetConfigurationPathAsync()
```
Returns the path to the agent's configuration file.

```csharp
Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
```
Reads and returns server IDs from the agent's configuration.

```csharp
Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config = null)
```
Adds an MCP server to the agent's configuration.

```csharp
Task<bool> RemoveServerFromAgentAsync(string serverId)
```
Removes an MCP server from the agent's configuration.

```csharp
Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
```
Enables or disables a server for this agent.

### IInstallationManager

Manages relationships between servers and agents.

**Methods:**

```csharp
Task<IEnumerable<ServerInstallation>> GetAllInstallationsAsync()
```
Returns all server installations across all agents.

```csharp
Task<IEnumerable<ServerInstallation>> GetInstallationsByServerIdAsync(string serverId)
```
Returns all agent installations for a specific server.

```csharp
Task<IEnumerable<ServerInstallation>> GetInstallationsByAgentIdAsync(string agentId)
```
Returns all server installations for a specific agent.

```csharp
Task<ServerInstallation> AddServerToAgentAsync(string serverId, string agentId, Dictionary<string, string>? config = null)
```
Adds a server to an agent with optional configuration.

```csharp
Task<bool> RemoveServerFromAgentAsync(string serverId, string agentId)
```
Removes a server from an agent.

```csharp
Task<bool> ToggleServerEnabledAsync(string serverId, string agentId)
```
Toggles the enabled state of a server for an agent.

```csharp
Task<bool> UpdateInstallationConfigAsync(string installationId, Dictionary<string, string> config)
```
Updates the configuration for a specific installation.

### IServerRegistry

Interface for MCP server registries (npm, GitHub, custom).

**Properties:**

```csharp
string Name { get; }
```
The display name of this registry.

**Methods:**

```csharp
Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
```
Searches for servers matching the query.

```csharp
Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
```
Returns all available servers from this registry.

```csharp
Task<McpServer?> GetServerDetailsAsync(string serverId)
```
Gets detailed information about a specific server.

### IServerMonitor

Monitors server health and status.

**Methods:**

```csharp
Task<bool> IsServerRunningAsync(string serverId)
```
Checks if a server is currently running.

```csharp
Task<ServerHealthStatus> GetServerHealthAsync(string serverId)
```
Gets health status including metrics and error messages.

```csharp
Task StartMonitoringAsync(string serverId)
```
Starts monitoring a server.

```csharp
Task StopMonitoringAsync(string serverId)
```
Stops monitoring a server.

## Domain Models

### McpServer

```csharp
public class McpServer
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string RepositoryUrl { get; set; }
    public string InstallCommand { get; set; }
    public List<string> Tags { get; set; }
    public bool IsInstalled { get; set; }
    public DateTime? InstalledAt { get; set; }
    public Dictionary<string, string> Configuration { get; set; }
}
```

### Agent

```csharp
public class Agent
{
    public string Id { get; set; }
    public string Name { get; set; }
    public AgentType Type { get; set; }
    public bool IsDetected { get; set; }
    public string ConfigPath { get; set; }
    public List<string> ConfiguredServerIds { get; set; }
}
```

### ServerInstallation

```csharp
public class ServerInstallation
{
    public string Id { get; set; }
    public string ServerId { get; set; }
    public string AgentId { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime InstalledAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<string, string> AgentSpecificConfig { get; set; }
}
```

### ServerHealthStatus

```csharp
public class ServerHealthStatus
{
    public string ServerId { get; set; }
    public bool IsRunning { get; set; }
    public bool IsHealthy { get; set; }
    public DateTime LastChecked { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, string> Metrics { get; set; }
}
```

## Usage Examples

### Example 1: Detecting Agents

```csharp
var agentManager = serviceProvider.GetRequiredService<IAgentManager>();
var agents = await agentManager.DetectInstalledAgentsAsync();

foreach (var agent in agents)
{
    Console.WriteLine($"Found: {agent.Name} at {agent.ConfigPath}");
}
```

### Example 2: Installing a Server

```csharp
var serverManager = serviceProvider.GetRequiredService<IServerManager>();
var server = new McpServer
{
    Id = "my-server",
    Name = "My Server",
    Version = "1.0.0"
};

await serverManager.InstallServerAsync(server);
```

### Example 3: Adding Server to Agent

```csharp
var installationManager = serviceProvider.GetRequiredService<IInstallationManager>();
await installationManager.AddServerToAgentAsync("my-server", "claude");
```

### Example 4: Searching for Servers

```csharp
var registry = serviceProvider.GetRequiredService<IServerRegistry>();
var results = await registry.SearchAsync("database");

foreach (var result in results)
{
    Console.WriteLine($"{result.Server.Name} - {result.DownloadCount} downloads");
}
```

### Example 5: Creating a Custom Agent Connector

```csharp
public class MyAgentConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.Other;

    public async Task<bool> IsAgentInstalledAsync()
    {
        // Check if your agent is installed
        return File.Exists("/path/to/agent/config");
    }

    public async Task<string> GetConfigurationPathAsync()
    {
        return "/path/to/agent/config";
    }

    public async Task<IEnumerable<string>> GetConfiguredServerIdsAsync()
    {
        // Read and parse your agent's config
        var config = await File.ReadAllTextAsync("/path/to/agent/config");
        // Parse and return server IDs
        return new List<string>();
    }

    public async Task<bool> AddServerToAgentAsync(string serverId, Dictionary<string, string>? config)
    {
        // Add server to your agent's config
        return true;
    }

    public async Task<bool> RemoveServerFromAgentAsync(string serverId)
    {
        // Remove server from your agent's config
        return true;
    }

    public async Task<bool> SetServerEnabledAsync(string serverId, bool enabled)
    {
        // Enable/disable server in your agent's config
        return true;
    }
}

// Register in Program.cs
builder.Services.AddSingleton<IAgentConnector, MyAgentConnector>();
```

## Error Handling

All async methods may throw exceptions:

- `InvalidOperationException`: When an operation cannot be completed (e.g., agent not found)
- `IOException`: When file operations fail
- `JsonException`: When configuration parsing fails

Always wrap calls in try-catch blocks:

```csharp
try
{
    await installationManager.AddServerToAgentAsync("server-id", "agent-id");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Configuration

### Registering Services

In `Program.cs`:

```csharp
// Core services
builder.Services.AddSingleton<IServerManager, ServerManager>();
builder.Services.AddSingleton<IAgentManager, AgentManager>();
builder.Services.AddSingleton<IInstallationManager, InstallationManager>();
builder.Services.AddSingleton<IServerMonitor, ServerMonitor>();

// Agent connectors
builder.Services.AddSingleton<IAgentConnector, ClaudeConnector>();
builder.Services.AddSingleton<IAgentConnector, CopilotConnector>();

// Registries
builder.Services.AddSingleton<IServerRegistry, MockServerRegistry>();
```

### Dependency Injection

All services are registered as singletons for in-memory state. For production, consider:

- Using scoped services with a database
- Adding caching layers
- Implementing repository patterns

## Testing

Services are designed for testability with interfaces. Mock implementations using Moq:

```csharp
var mockServerManager = new Mock<IServerManager>();
mockServerManager.Setup(m => m.GetInstalledServersAsync())
    .ReturnsAsync(new List<McpServer>());
```

See the test project for complete examples.
