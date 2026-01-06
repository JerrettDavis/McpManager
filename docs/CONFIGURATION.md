# MCP Server Configuration

## Overview

MCP Manager now supports **rich configuration** for MCP servers at both **global** and **agent-specific** levels. This allows you to:

- Define default configurations at the server level
- Override configurations per agent when needed
- Automatically propagate global configuration updates to agents using default settings
- Safely maintain custom agent configurations without accidental overwrites

## Key Concepts

### Global Configuration

Each MCP server has a **global configuration** that serves as the default for all agents using that server. This configuration is defined on the server itself and can be edited from the Server Configuration page.

### Agent-Specific Configuration

Each agent can have its own **agent-specific configuration** that overrides the global settings. This allows customization per agent without affecting other agents.

### Configuration Status

An agent's configuration can be in one of two states:

- **Matches Global**: The agent's configuration is identical to the global server configuration
- **Custom Configuration**: The agent has a customized configuration that differs from global

### Safe Propagation

When you update a global server configuration, MCP Manager follows these rules:

1. **If an agent's config matches the old global config** → It will be **automatically updated** to the new global config
2. **If an agent's config differs from the old global config** → It will **not be modified** (custom configs are preserved)

This ensures that:
- Agents using default settings stay in sync
- Custom configurations are never accidentally overwritten
- Configuration changes are predictable and safe

## User Guide

### Viewing Server Configuration

1. Navigate to **Servers** → **Installed Servers**
2. Click **⚙️ Configure** on any server
3. You'll see:
   - The global configuration editor
   - A list of agents using this server
   - Each agent's configuration status (matches/differs)

### Editing Global Configuration

On the Server Configuration page:

1. Use either the **Form Editor** or **JSON Editor** tab
2. In Form Editor:
   - Click **➕ Add Configuration Item** to add new settings
   - Enter key-value pairs
   - Click **🗑️** to remove items
3. In JSON Editor:
   - Edit the JSON directly
   - The form view updates automatically
4. Click **💾 Save Configuration**

The system will show you how many agent installations were automatically updated.

### Editing Agent-Specific Configuration

From the Agent Details page:

1. Navigate to **Agents** → select an agent
2. Click **⚙️** next to any configured server
3. You'll see:
   - The agent-specific configuration editor
   - A badge showing if it matches global
   - Comparison view if configurations differ
4. Edit the configuration using Form or JSON editor
5. Click **💾 Save Agent Configuration**

### Resetting to Global Configuration

If an agent has a custom configuration and you want to revert to global:

1. Open the agent-specific configuration page
2. Click **🔄 Reset to Global Configuration**
3. The agent will inherit the server's global configuration

## Configuration Editor Features

### Dual Edit Modes

The configuration editor supports two synchronized edit modes:

#### Form Editor
- User-friendly interface for key-value pairs
- Add, edit, and remove configuration items
- Real-time validation

#### JSON Editor
- Direct JSON editing for power users
- Syntax validation
- Formatted output with indentation

### Live Synchronization

- Switching between Form and JSON views automatically syncs data
- Changes in one view immediately reflect in the other
- Validation errors appear in both views

### Validation

The editor validates:
- JSON syntax (in JSON mode)
- Non-empty keys
- Non-null values
- Proper dictionary structure

## Configuration Examples

### Example 1: Global-Only Configuration

**Scenario**: You have a database MCP server that all agents should connect to with the same credentials.

```json
{
  "host": "db.example.com",
  "port": "5432",
  "database": "shared_db"
}
```

All agents will use these settings. When you update the host, all agents are automatically updated.

### Example 2: Agent Override

**Scenario**: Claude Desktop needs to use a different database than GitHub Copilot.

**Global Configuration**:
```json
{
  "host": "db.example.com",
  "database": "shared_db"
}
```

**Claude Desktop Override**:
```json
{
  "host": "db.example.com",
  "database": "claude_db"
}
```

Now Claude has a custom configuration. When you update the global `host`, Claude's custom config is preserved.

### Example 3: Propagation Scenario

**Initial State**:
- Global config: `{"apiKey": "old_key", "region": "us-east-1"}`
- Agent A config: `{"apiKey": "old_key", "region": "us-east-1"}` (matches)
- Agent B config: `{"apiKey": "custom_key", "region": "us-west-2"}` (differs)

**After updating global config to**: `{"apiKey": "new_key", "region": "us-east-1"}`

**Result**:
- Agent A config: `{"apiKey": "new_key", "region": "us-east-1"}` ✓ Updated
- Agent B config: `{"apiKey": "custom_key", "region": "us-west-2"}` ✓ Preserved

## Architecture

### Services

#### ConfigurationService (`IConfigurationService`)

Handles all configuration-related operations:

- **Configuration Comparison**: Determines if two configurations are equal
- **Effective Configuration**: Resolves agent-specific vs global configs
- **Safe Propagation**: Updates matching agent configs when global changes
- **Validation**: Ensures configuration data is well-formed
- **Serialization**: Converts between dictionaries and JSON

### Components

#### ConfigurationEditor

Reusable Blazor component for editing configurations:
- Supports both form and JSON editing
- Live synchronization between modes
- Validation feedback
- Read-only mode support

#### ConfigurationStatusBadge

Visual indicator showing if an agent's config matches global:
- ✓ Matches Global (green)
- ⚠️ Custom Configuration (yellow)

### Pages

#### ServerDetails (`/servers/{ServerId}`)

Manage global server configuration:
- Edit global configuration
- View all agents using the server
- See which agents have custom configs
- Navigate to agent-specific configuration

#### AgentServerConfiguration (`/agents/{AgentId}/servers/{ServerId}`)

Manage agent-specific server configuration:
- Edit configuration for a specific agent
- View differences from global
- Reset to global configuration
- Toggle server enabled/disabled

## API Reference

### IConfigurationService

```csharp
public interface IConfigurationService
{
    // Compare two configurations
    bool AreConfigurationsEqual(
        Dictionary<string, string> config1, 
        Dictionary<string, string> config2);

    // Get effective configuration (agent-specific or global)
    Dictionary<string, string> GetEffectiveConfiguration(
        McpServer server, 
        ServerInstallation? installation);

    // Check if agent config matches global
    bool DoesAgentConfigMatchGlobal(
        McpServer server, 
        ServerInstallation installation);

    // Propagate global config update to matching agents
    Task<IEnumerable<string>> PropagateConfigurationUpdateAsync(
        string serverId,
        Dictionary<string, string> oldGlobalConfig,
        Dictionary<string, string> newGlobalConfig);

    // Validate configuration
    ConfigurationValidationResult ValidateConfiguration(
        Dictionary<string, string> config);

    // Serialize/deserialize
    string SerializeConfiguration(Dictionary<string, string> config);
    Dictionary<string, string>? DeserializeConfiguration(string json);
}
```

## Best Practices

### When to Use Global Configuration

Use global configuration when:
- All agents should share the same settings
- You want to manage configuration in one place
- Settings change infrequently

### When to Use Agent-Specific Configuration

Use agent-specific configuration when:
- Different agents need different settings (e.g., different API keys)
- You're testing changes on one agent before rolling out globally
- An agent has unique requirements

### Configuration Management Tips

1. **Start with Global**: Define sensible defaults at the server level
2. **Override Sparingly**: Only create agent-specific configs when truly needed
3. **Document Changes**: Use descriptive keys and consistent naming
4. **Test Changes**: Try changes on one agent before updating globally
5. **Review Status**: Periodically check which agents have custom configs

## Troubleshooting

### Configuration Not Saving

**Problem**: Configuration changes don't persist

**Solutions**:
- Check for validation errors in the UI
- Ensure JSON is properly formatted
- Verify keys are not empty
- Check that values are not null

### Agent Not Updating After Global Change

**Problem**: Agent didn't update when global config changed

**Explanation**: The agent has a custom configuration that differs from the old global config. This is expected behavior - custom configs are preserved.

**Solution**: 
1. Navigate to the agent-specific configuration page
2. Click "Reset to Global Configuration" if you want to sync it

### Configuration Differences Not Showing

**Problem**: Badge shows "Custom Configuration" but configs look the same

**Solution**: Check for:
- Extra whitespace in values
- Key ordering differences (order doesn't matter functionally)
- Hidden characters in JSON

## Testing

The configuration system includes comprehensive unit tests covering:

- Configuration equality checking (7 tests)
- Effective configuration resolution (3 tests)
- Global vs agent matching (2 tests)
- Safe propagation rules (2 tests)
- Validation (4 tests)
- Serialization/deserialization (4 tests)

All 24 configuration tests pass, ensuring reliable operation.

## Future Enhancements

Potential future improvements:

- **Configuration Templates**: Predefined configuration templates for common scenarios
- **Environment Variables**: Support for environment variable interpolation
- **Configuration History**: Track changes over time
- **Bulk Operations**: Update multiple agents at once
- **Import/Export**: Share configurations between installations
- **Schema Validation**: Validate configurations against server-specific schemas
