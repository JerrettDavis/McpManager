# Claude Code Configuration Support

This document describes how MCP Manager detects and manages MCP servers configured in Claude Code.

## Configuration File Locations

Claude Code stores MCP server configurations in two locations:

### 1. User-Level Configuration (`~/.claude.json`)

The primary configuration file that contains:
- **User-level MCP servers**: Servers available across all projects
- **Project-level MCP servers**: Servers specific to individual projects

**Location**: `~/.claude.json` (in the user's home directory)

**Structure**:
```json
{
  "mcpServers": {
    "github": {
      "type": "http",
      "url": "https://api.githubcopilot.com/mcp/"
    },
    "custom-server": {
      "type": "stdio",
      "command": "npx",
      "args": ["-y", "custom-server"],
      "env": {
        "API_KEY": "secret"
      }
    }
  },
  "projects": {
    "C:/git/MyProject": {
      "mcpServers": {
        "project-specific-server": {
          "type": "stdio",
          "command": "node",
          "args": ["server.js"]
        }
      }
    }
  }
}
```

### 2. Legacy Settings File (`~/.claude/settings.json`)

An older configuration format that may still be used by some Claude Code installations.

**Location**: `~/.claude/settings.json`

**Structure**:
```json
{
  "mcpServers": {
    "server-name": {
      "command": "npx",
      "args": ["-y", "server-name"],
      "env": {},
      "disabled": false
    }
  }
}
```

## Server Types

Claude Code supports two types of MCP servers:

### HTTP Servers
```json
{
  "type": "http",
  "url": "https://api.example.com/mcp/"
}
```

### Stdio Servers
```json
{
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "package-name"],
  "env": {
    "KEY": "value"
  },
  "disabled": false
}
```

## Auto-Detection

MCP Manager automatically detects Claude Code MCP servers by:

1. **Checking for user-level configuration** (`~/.claude.json`)
   - Reads the `mcpServers` section for user-level servers
   - Reads the `projects.<current-directory>.mcpServers` section for project-specific servers

2. **Checking legacy configuration** (`~/.claude/settings.json`)
   - Reads the `mcpServers` section

3. **Merging configurations**
   - All detected servers are combined and displayed in MCP Manager
   - Duplicate server IDs from different sources are handled gracefully

## File Watching

MCP Manager includes a **Configuration Watcher** service that monitors configuration files for changes:

- **Monitored files**:
  - `~/.claude.json` (user-level config)
  - `~/.claude/settings.json` (legacy config)

- **Automatic updates**:
  - When a configuration file changes externally (e.g., edited in a text editor)
  - The Configuration Watcher detects the change
  - A notification event is raised
  - The UI can refresh to show the updated server list

- **Debouncing**:
  - File changes are debounced by 100ms to avoid multiple events from rapid edits

## Implementation Details

### ClaudeCodeConnector

**Location**: `src/McpManager.Infrastructure/Connectors/ClaudeCodeConnector.cs`

Key methods:
- `GetConfiguredServerIdsAsync()`: Reads servers from all configuration sources
- `AddServerToAgentAsync()`: Adds a server to the user config
- `RemoveServerFromAgentAsync()`: Removes a server from the config
- `SetServerEnabledAsync()`: Enables or disables a server

### ConfigurationWatcher

**Location**: `src/McpManager.Application/Services/ConfigurationWatcher.cs`

Monitors configuration files using `FileSystemWatcher` and raises events when changes occur.

### ConfigurationWatcherWorker

**Location**: `src/McpManager.Infrastructure/BackgroundWorkers/ConfigurationWatcherWorker.cs`

Background service that starts the configuration watcher when the application starts.

## Usage in UI

When viewing the Claude Code agent in MCP Manager:

1. The UI displays all detected MCP servers (from both user and project configs)
2. You can enable/disable servers (updates the config file)
3. You can add new servers (adds to `~/.claude.json`)
4. You can remove servers (removes from the config file)
5. External changes are automatically detected and reflected in the UI

## Testing

Test coverage is provided in:
- `tests/McpManager.Tests/Services/ClaudeCodeConnectorTests.cs`

Tests verify:
- Correct parsing of user-level MCP servers
- Correct parsing of project-level MCP servers
- Config file structure validation
- Agent type identification

## Future Enhancements

Potential improvements:
- Support for additional Claude Code configuration options
- Better conflict resolution for duplicate server IDs
- Project-specific server management UI
- Import/export configuration functionality
