# Bulk Agent Management

You can now add or remove an MCP server from multiple agents simultaneously from the Server Details page.

## Feature Overview

When viewing a server's details page (`/servers/{serverId}`), you can manage which agents have access to that server all in one place, rather than visiting each agent's page individually.

## How to Use

### 1. Navigate to Server Details

From any of these locations:
- **Installed Servers** page → Click on a server name
- **Browse Servers** page → Install a server → Click its name
- Direct URL: `/servers/{serverId}`

### 2. View the "Manage Agents" Section

The server details page now shows all detected agents with:
- **Checkbox**: Select/deselect the agent
- **Agent Name**: The agent's display name and type
- **Current Status**:
  - `Not Added` - Server not configured for this agent
  - `✓ Enabled` - Server is configured and enabled
  - `⏸ Disabled` - Server is configured but disabled
- **New Status**: Shows what will happen when you apply changes
  - `→ Enabled` - Server will be added/enabled
  - `→ Not Added` - Server will be removed
  - `No change` - No action will be taken
- **Configuration**: Shows if agent's config matches the global config
- **Actions**: Link to configure the server for that specific agent

### 3. Select Agents

**Individual Selection**:
- Click checkboxes next to individual agents to add/remove them

**Bulk Selection**:
- Click **☑ Select All** to select all agents
- Click **☐ Deselect All** to deselect all agents
- Click the checkbox in the table header to toggle all agents

**Visual Feedback**:
- Rows with pending changes are highlighted in yellow
- The "New Status" column shows what will happen
- Apply Changes and Cancel buttons appear when changes are pending

### 4. Apply Changes

1. Click **💾 Apply Changes** to apply all selected changes
2. The system will:
   - Add the server to newly selected agents
   - Enable the server for agents where it was disabled
   - Remove the server from deselected agents
3. A success message shows what was changed

### 5. Cancel Changes

Click **✖️ Cancel** to revert all selection changes without applying them.

## Example Workflows

### Add Server to Multiple Agents

**Scenario**: You want to add the "github" server to all your agents

1. Go to `/servers/github`
2. Click **☑ Select All**
3. Review the "New Status" column - should show `→ Enabled` for all agents
4. Click **💾 Apply Changes**
5. Success message: "Success: 3 agent(s) added/enabled"

### Remove Server from Specific Agents

**Scenario**: You want to remove "filesystem" from 2 out of 3 agents

1. Go to `/servers/filesystem`
2. Currently all 3 agents are selected (have the server enabled)
3. Uncheck the 2 agents you want to remove
4. Review: Those 2 rows show `→ Not Added` in "New Status"
5. Click **💾 Apply Changes**
6. Success message: "Success: 2 agent(s) removed"

### Enable a Server Only for Development Agents

**Scenario**: You want to enable "memory" server only for dev agents

1. Go to `/servers/memory`
2. Click **☐ Deselect All** to start fresh
3. Check only the development agent(s)
4. Click **💾 Apply Changes**

## Status Indicators

### Current Status Column

| Badge | Meaning |
|-------|---------|
| `Not Added` | Server is not configured for this agent |
| `✓ Enabled` | Server is configured and active |
| `⏸ Disabled` | Server is configured but inactive |

### New Status Column

| Text | Meaning |
|------|---------|
| `→ Enabled` | Server will be added or enabled |
| `→ Not Added` | Server will be removed |
| `No change` | No action will be taken |

### Configuration Status

| Badge | Meaning |
|-------|---------|
| `Matching` | Agent's config matches the global config |
| `Different` | Agent has a custom configuration |
| `—` | Not applicable (server not configured) |

## Success Messages

After applying changes, you'll see messages like:

- `Success: 3 agent(s) added/enabled` - Added/enabled server for 3 agents
- `Success: 2 agent(s) removed` - Removed server from 2 agents
- `Success: 2 agent(s) added/enabled, 1 agent(s) removed` - Mixed operations
- `No changes to apply` - Selection matches current state
- `Partial success: ... Errors: ...` - Some operations failed

## Error Handling

If some operations fail:
- You'll see a warning message with details
- Successful operations are still applied
- Failed operations show specific error messages
- You can retry failed operations

Example error message:
```
Partial success: 2 agent(s) added/enabled.
Errors: ClaudeCode: Server already configured; Codex: Agent config file not writable
```

## Configuration Management

After adding a server to multiple agents:

1. **Global Configuration**: Edit the server's global configuration on this page
2. **Auto-Propagation**: Changes propagate to agents with matching configurations
3. **Custom Configuration**: Configure individual agents at `/agents/{agentId}/servers/{serverId}`

## Tips

1. **Check before applying**: Review the "New Status" column to see what will happen
2. **Use Select All carefully**: Make sure you want to add the server to ALL agents
3. **Configure after adding**: After bulk-adding, you may want to customize configs for specific agents
4. **Yellow highlights**: Shows which rows have pending changes
5. **Disabled state**: All controls are disabled while processing to prevent double-submission

## Comparison: Before vs. After

### Before (v0.1.4 and earlier)

To add the same server to 3 agents:
1. Go to Agent 1's page → Find server → Click "Add to Agent" → Click "Enable"
2. Go to Agent 2's page → Find server → Click "Add to Agent" → Click "Enable"
3. Go to Agent 3's page → Find server → Click "Add to Agent" → Click "Enable"

**Result**: 3 separate page visits, 6 clicks

### After (v0.1.5+)

To add the same server to 3 agents:
1. Go to server's page → Click "Select All" → Click "Apply Changes"

**Result**: 1 page visit, 2 clicks

## Related Features

- **Agent Details Page** (`/agents/{agentId}`): Manage all servers for a specific agent
- **Agent Server Configuration** (`/agents/{agentId}/servers/{serverId}`): Configure a specific server for a specific agent
- **Server Global Configuration**: Edit configuration that propagates to matching agents
- **Debug Page** (`/debug`): View and fix sync issues

## Version History

- **v0.1.5**: Added bulk agent management to Server Details page
