# MCP Manager

![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-512BD4?style=flat&logo=blazor)
![License](https://img.shields.io/badge/license-MIT-green)

> **Manage your Model Context Protocol servers across all AI agents in one place!**

MCP Manager is a modern, extensible dashboard application for discovering, installing, and managing MCP (Model Context Protocol) servers across multiple AI agents like Claude Desktop, GitHub Copilot, and more.

## ✨ Features

- 🔍 **Browse & Search** - Discover MCP servers from registries
- 📦 **Install & Manage** - One-click installation and management
- 🤖 **Multi-Agent Support** - Works with Claude, Copilot, and more
- 🎛️ **Unified Dashboard** - Manage all servers and agents from one place
- ⚡ **Simple UI** - Intuitive checkboxes, buttons, and actions
- 🔧 **Extensible** - Plugin-based architecture for new agents
- 🐳 **Containerizable** - Docker support for flexible deployment
- ✅ **Well-Tested** - Comprehensive unit test coverage

## 🏗️ Architecture

MCP Manager follows **SOLID principles** and **Clean Architecture**:

### Project Structure

```
McpManager/
├── src/
│   ├── McpManager.Core/           # Domain models and interfaces
│   ├── McpManager.Application/    # Business logic and services
│   ├── McpManager.Infrastructure/ # Agent connectors and registries
│   └── McpManager.Web/            # Blazor Server web application
├── tests/
│   └── McpManager.Tests/          # Unit and integration tests
└── docs/                          # Documentation
```

### Key Design Patterns

- **Dependency Inversion**: All dependencies flow inward through interfaces
- **Single Responsibility**: Each service handles one concern
- **Open/Closed**: Extensible via `IAgentConnector` interface
- **DRY (Don't Repeat Yourself)**: Shared logic in base classes and services

## 🚀 Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- One or more AI agents installed (Claude Desktop, GitHub Copilot, etc.)

### Run Locally

```bash
# Clone the repository
git clone https://github.com/JerrettDavis/McpManager.git
cd McpManager

# Restore dependencies
dotnet restore

# Run the application
dotnet run --project src/McpManager.Web

# Navigate to https://localhost:5001
```

### Run with Docker

```bash
# Build the image
docker build -t mcp-manager .

# Run the container
docker run -p 8080:8080 mcp-manager

# Navigate to http://localhost:8080
```

## 📖 Usage

### Dashboard

The main dashboard provides an overview of:
- Number of installed MCP servers
- Detected AI agents on your system
- Active server installations across agents

### Browse Servers

1. Navigate to **Browse Servers**
2. Search for servers by name, description, or tags
3. Click **Install** on any server you want to add

### Manage Agents

1. Navigate to **Agents**
2. Select an agent to view its configured servers
3. Use checkboxes to **Enable/Disable** servers
4. Click **Remove** to uninstall a server from an agent
5. Click **Add to Agent** to configure a new server

### Agent-Specific Configuration

Each agent page shows:
- All installed MCP servers
- Current enable/disable status
- Quick actions for configuration

Simply toggle checkboxes to enable or disable servers, or click remove buttons to uninstall.

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

Current test coverage: **22 passing tests** covering core services.

## 🔌 Supported Agents

Currently supported AI agents:

- ✅ **Claude Desktop** - Full support for configuration management
- ✅ **GitHub Copilot** - VS Code integration support
- 🔜 **OpenAI Codex** - Coming soon
- 🔜 **Custom Agents** - Extensible via `IAgentConnector` interface

## 🛠️ Development

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

## 📦 Building for Production

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

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
- Inspired by the [Model Context Protocol](https://modelcontextprotocol.io/)
- UI powered by [Bootstrap 5](https://getbootstrap.com/)

---

**Made with ❤️ for the AI development community**

