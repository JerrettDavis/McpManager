# MCP Manager

[![CI](https://github.com/JerrettDavis/McpManager/actions/workflows/ci.yml/badge.svg)](https://github.com/JerrettDavis/McpManager/actions/workflows/ci.yml)
[![CodeQL](https://github.com/JerrettDavis/McpManager/actions/workflows/codeql.yml/badge.svg)](https://github.com/JerrettDavis/McpManager/actions/workflows/codeql.yml)
[![codecov](https://codecov.io/gh/JerrettDavis/McpManager/branch/main/graph/badge.svg)](https://codecov.io/gh/JerrettDavis/McpManager)
![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-512BD4?style=flat&logo=blazor)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Release](https://img.shields.io/github/v/release/JerrettDavis/McpManager)](https://github.com/JerrettDavis/McpManager/releases)
[![Docker](https://img.shields.io/badge/docker-ghcr.io-blue?logo=docker)](https://github.com/JerrettDavis/McpManager/pkgs/container/mcpmanager)

MCP Manager is a dashboard for discovering, installing, and managing Model Context Protocol servers across multiple AI agents.

## Features

- Browse and search MCP servers from registries
- One-click server installation and management
- Multi-agent support (Claude Desktop, GitHub Copilot, etc.)
- Unified dashboard for all servers and agents
- Bulk agent management with checkbox selection
- Plugin-based architecture for extensibility
- Docker support for containerized deployment
- Comprehensive unit test coverage

## Architecture

MCP Manager follows Clean Architecture with clear separation of concerns:

### Project Structure

```
McpManager/
├── src/
│   ├── McpManager.Core/           # Domain models and interfaces
│   ├── McpManager.Application/    # Business logic and services
│   ├── McpManager.Infrastructure/ # Agent connectors and registries
│   ├── McpManager.Web/            # Blazor Server web application
│   └── McpManager.Desktop/        # Desktop app using Photino.Blazor
├── tests/
│   └── McpManager.Tests/          # Unit and integration tests
└── docs/                          # Documentation
```

### Key Design Patterns

- **Dependency Inversion**: All dependencies flow inward through interfaces
- **Single Responsibility**: Each service handles one concern
- **Open/Closed**: Extensible via `IAgentConnector` interface
- **DRY (Don't Repeat Yourself)**: Shared logic in base classes and services

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for building from source)
- One or more AI agents installed (Claude Desktop, GitHub Copilot, etc.)

### Installation Options

#### Windows SmartScreen Warning

Windows may show a security warning when running the desktop app because it's not code-signed. Code signing certificates cost $300-500/year, which isn't practical for this open source project.

**To run the app:**
1. Click "More info"
2. Click "Run anyway"

**Why it's safe:**
- 100% open source - audit the code yourself
- Built by GitHub Actions - reproducible builds
- SHA256 hashes in release notes
- No telemetry or network calls

See [SmartScreen Warning Guide](docs/SMARTSCREEN_WARNING.md) for details.

---

#### Option 1: Desktop App (Recommended)

The desktop app runs as a standalone application with no browser required!

**Windows:**
1. Download the latest `mcpmanager-desktop-win-x64.zip` from [Releases](https://github.com/JerrettDavis/McpManager/releases)
2. Extract the archive
3. Run `McpManager.Desktop.exe`
4. The app opens in its own window!

**Linux:**
1. Download the latest `mcpmanager-desktop-linux-x64.tar.gz` from [Releases](https://github.com/JerrettDavis/McpManager/releases)
2. Extract: `tar -xzf mcpmanager-desktop-linux-x64.tar.gz`
3. Make executable: `chmod +x McpManager.Desktop`
4. Run: `./McpManager.Desktop`
5. The app opens in its own window!

#### Option 2: Server-Hosted

If you prefer the traditional server model where you open a browser:

**Windows:**
1. Download the latest `mcpmanager-server-win-x64.zip` from [Releases](https://github.com/JerrettDavis/McpManager/releases)
2. Extract the archive
3. Run `McpManager.Web.exe`
4. Navigate to http://localhost:5000

**Linux:**
1. Download the latest `mcpmanager-server-linux-x64.tar.gz` from [Releases](https://github.com/JerrettDavis/McpManager/releases)
2. Extract: `tar -xzf mcpmanager-server-linux-x64.tar.gz`
3. Make executable: `chmod +x McpManager.Web`
4. Run: `./McpManager.Web`
5. Navigate to http://localhost:5000

#### Option 3: Docker

```bash
# Pull and run the latest image
docker pull ghcr.io/jerrettdavis/mcpmanager:latest
docker run -p 8080:8080 ghcr.io/jerrettdavis/mcpmanager:latest

# Navigate to http://localhost:8080
```

#### Option 4: Build from Source

```bash
git clone https://github.com/JerrettDavis/McpManager.git
cd McpManager
dotnet restore
dotnet run --project src/McpManager.Web
# Navigate to https://localhost:5001
```

## Usage

### Dashboard

The main dashboard shows installed servers, detected agents, and active installations.

### Browse Servers

Navigate to Browse Servers to search and install MCP servers from registries.

### Manage Agents

1. Select an agent to view configured servers
2. Use checkboxes to enable/disable servers
3. Use bulk operations to add/remove servers from multiple agents
4. Click Remove to uninstall a server from an agent

## Testing

```bash
dotnet test
dotnet test /p:CollectCoverage=true  # with coverage
```

## Supported Agents

- Claude Desktop
- GitHub Copilot (VS Code)
- Custom agents via `IAgentConnector` interface

## Development

### Adding a New Agent Connector

1. Create a new class implementing `IAgentConnector`:

```csharp
public class MyAgentConnector : IAgentConnector
{
    public AgentType AgentType => AgentType.Other;
    
    public Task<bool> IsAgentInstalledAsync() { /* ... */ }
    public Task<string> GetConfigurationPathAsync() { /* ... */ }
    // Implement other interface methods
}
```

2. Register in `Program.cs`:

```csharp
builder.Services.AddSingleton<IAgentConnector, MyAgentConnector>();
```

### Adding a New Registry

Implement `IServerRegistry` to connect to npm, GitHub, or custom registries:

```csharp
public class NpmRegistry : IServerRegistry
{
    public string Name => "npm MCP Registry";
    public Task<IEnumerable<ServerSearchResult>> SearchAsync(string query) { /* ... */ }
    // Implement other interface methods
}
```

## Building for Production

### Build Executable

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

### Build Docker Image

```bash
docker build -t mcp-manager:latest .
docker tag mcp-manager:latest your-registry/mcp-manager:latest
docker push your-registry/mcp-manager:latest
```

## Contributing

Contributions welcome. Fork the repository, create a feature branch, and submit a pull request.

## License

MIT License - see [LICENSE](LICENSE) file.

## Acknowledgments

Built with Blazor and Bootstrap. Implements the [Model Context Protocol](https://modelcontextprotocol.io/).

