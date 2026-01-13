# MCP Manager Architecture

## Overview

MCP Manager uses Clean Architecture with separation of concerns across multiple layers. Each layer can be tested independently, and the design supports extension without modifying existing code. Runs as desktop app, web server, or container.

## Architecture Layers

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│       (McpManager.Web - Blazor)        │
├─────────────────────────────────────────┤
│       Application Layer                 │
│   (McpManager.Application - Services)   │
├─────────────────────────────────────────┤
│      Infrastructure Layer               │
│ (McpManager.Infrastructure - Adapters)  │
├─────────────────────────────────────────┤
│          Core/Domain Layer              │
│   (McpManager.Core - Models/Interfaces) │
└─────────────────────────────────────────┘
```

## Core Layer (McpManager.Core)

The innermost layer containing:

### Domain Models
- `McpServer`: Represents an MCP server
- `Agent`: Represents an AI agent (Claude, Copilot, etc.)
- `ServerInstallation`: Links servers to agents
- `ServerHealthStatus`: Health monitoring data

### Interfaces
- `IServerManager`: Server CRUD operations
- `IAgentManager`: Agent detection and management
- `IAgentConnector`: Agent-specific operations (extensibility point)
- `IInstallationManager`: Server-agent relationship management
- `IServerRegistry`: Server discovery and search
- `IServerMonitor`: Health monitoring

**Key Principles:**
- No dependencies on other layers
- Pure domain logic
- Framework-agnostic

## Application Layer (McpManager.Application)

Business logic and orchestration:

### Services
- `ServerManager`: Implements server management logic
- `AgentManager`: Orchestrates agent detection across connectors
- `InstallationManager`: Manages server-agent relationships
- `ServerMonitor`: Implements health monitoring

**Key Principles:**
- Depends only on Core layer
- Contains business rules
- No framework-specific code

## Infrastructure Layer (McpManager.Infrastructure)

External concerns and adapters:

### Agent Connectors
- `ClaudeConnector`: Claude Desktop integration
- `CopilotConnector`: GitHub Copilot integration
- Extensible for new agents

### Registries
- `MockServerRegistry`: Demo registry (replace with npm, GitHub, etc.)

**Key Principles:**
- Implements Core interfaces
- Handles external dependencies
- File I/O, network calls, etc.

## Presentation Layer (McpManager.Web)

User interface built with Blazor Server:

### Pages
- `Home.razor`: Dashboard overview
- `BrowseServers.razor`: Search and install servers
- `InstalledServers.razor`: Manage installed servers
- `Agents.razor`: View detected agents
- `AgentDetails.razor`: Agent-specific server management

**Key Principles:**
- Depends on Application and Core layers
- Handles user interaction
- Responsive, intuitive UI

## SOLID Principles in Action

### Single Responsibility Principle
Each service has one reason to change:
- `ServerManager` only manages servers
- `AgentManager` only manages agents
- `InstallationManager` only manages relationships

### Open/Closed Principle
The system is open for extension, closed for modification:
- Add new agents by implementing `IAgentConnector`
- Add new registries by implementing `IServerRegistry`
- No need to modify existing code

### Liskov Substitution Principle
All implementations can be substituted:
- Any `IAgentConnector` works the same way
- Any `IServerRegistry` provides consistent search

### Interface Segregation Principle
Interfaces are focused and specific:
- `IServerManager` only has server operations
- `IAgentConnector` only has agent-specific operations
- No client is forced to depend on unused methods

### Dependency Inversion Principle
High-level modules don't depend on low-level modules:
- Application depends on Core abstractions
- Infrastructure implements Core interfaces
- Web depends on Application services

## Data Flow

### Example: Installing a Server for an Agent

```
User clicks "Add to Agent"
         ↓
AgentDetails.razor (Web Layer)
         ↓
InstallationManager (Application Layer)
         ↓
IAgentConnector (Core Interface)
         ↓
ClaudeConnector (Infrastructure Layer)
         ↓
Write to agent config file
```

## Extension Points

### Adding a New Agent

1. Create connector in Infrastructure:
```csharp
public class NewAgentConnector : IAgentConnector
{
    // Implement interface methods
}
```

2. Register in Program.cs:
```csharp
builder.Services.AddSingleton<IAgentConnector, NewAgentConnector>();
```

### Adding a New Registry

1. Create registry in Infrastructure:
```csharp
public class NpmRegistry : IServerRegistry
{
    // Implement interface methods
}
```

2. Register in Program.cs:
```csharp
builder.Services.AddSingleton<IServerRegistry, NpmRegistry>();
```

## Testing Strategy

### Unit Tests
- Services tested in isolation using mocks
- Domain logic tested without dependencies

### Integration Tests
- Infrastructure adapters tested with real I/O
- Database and file operations verified

### E2E Tests
- Complete user workflows tested
- UI interactions verified

## Deployment Scenarios

### Desktop Executable
```bash
dotnet publish -c Release -r win-x64 --self-contained
```
- Self-contained executable
- Runs locally on user's machine
- Direct access to agent configs

### Web Server
```bash
dotnet run --project src/McpManager.Web
```
- Hosted web application
- Remote access via browser
- Centralized management

### Docker Container
```bash
docker build -t mcp-manager .
docker run -p 8080:8080 mcp-manager
```
- Containerized deployment
- Easy scaling
- Cloud-ready architecture

## Security Considerations

1. **File Access**: Agent configurations contain sensitive data
2. **Authentication**: Web deployment should add auth
3. **Validation**: Input validation on all user inputs
4. **Isolation**: Container deployment provides process isolation

## Performance

- In-memory state for fast operations
- Async/await for non-blocking I/O
- Lazy loading
- Registry results could be cached

## Future Enhancements

1. **Persistence**: Add database for state management
2. **Authentication**: OAuth/OIDC for web deployment
3. **Real Registries**: Connect to npm, GitHub registries
4. **Monitoring**: Enhanced server health monitoring
5. **Notifications**: Alert on server failures
6. **Backup/Restore**: Configuration backup features
