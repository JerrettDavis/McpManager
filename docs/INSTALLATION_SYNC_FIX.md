# Installation Sync Fix

## Issue

Servers configured in agent configuration files (like `~/.claude.json`) were detected and auto-installed by the `AgentServerSyncWorker`, but the UI didn't recognize they were already configured. Instead of showing configuration options, it prompted users to "Add to Agent" even though the server was already in the agent's config file.

## Root Cause

The application uses an **in-memory collection** (`ICollection<ServerInstallation>`) to track which servers are configured for which agents. This collection is registered as a singleton but starts empty on each app launch:

```csharp
services.AddSingleton<ICollection<ServerInstallation>, List<ServerInstallation>>(_ => []);
```

### The Problem Flow

1. **App starts** → In-memory installations collection is empty
2. **UI loads** → Checks installations collection, finds nothing
3. **AgentServerSyncWorker runs** (5 seconds later) → Populates installations
4. **User sees "Add to Agent"** → Because UI loaded before worker ran
5. **App restarts** → All installations lost, cycle repeats

### Why This Happened

The `AgentServerSyncWorker` creates installation records in the background, but:
- There's a 5-second delay before first run
- The UI might load during this window
- Installation records aren't persisted to the database
- Every restart clears the in-memory collection

## The Fix

### 1. Made `InstallationManager.AddServerToAgentAsync` Idempotent

**Location**: `src/McpManager.Application/Services/InstallationManager.cs`

```csharp
public async Task<ServerInstallation> AddServerToAgentAsync(string serverId, string agentId, ...)
{
    // Check if installation already exists
    var existingInstallation = installations.FirstOrDefault(i => i.ServerId == serverId && i.AgentId == agentId);
    if (existingInstallation != null)
    {
        return existingInstallation; // Already tracked
    }

    // Check if server is already in agent's config
    var isAlreadyConfigured = agent.ConfiguredServerIds.Contains(serverId);

    if (!isAlreadyConfigured)
    {
        // Only add to config file if not already there
        await connector.AddServerToAgentAsync(serverId, config);
    }

    // Always create installation record to track the relationship
    installations.Add(installation);
    return installation;
}
```

**Benefits**:
- Safe to call even if server already exists in agent config
- Won't overwrite existing configuration
- Creates tracking record if missing

### 2. Added UI-Side Sync on Page Load

**Location**: `src/McpManager.Web/Components/Pages/AgentDetails.razor`

When the Agent Details page loads, it now:

1. Reads the agent's **actual configuration file** (`agent.ConfiguredServerIds`)
2. Compares with in-memory installations
3. For any servers in the config but not tracked:
   - Ensures the server exists in MCP Manager (auto-installs if needed)
   - Calls `AddServerToAgentAsync` to create the installation record
4. Refreshes the UI with updated installations

```csharp
// Sync with actual agent configuration
foreach (var serverId in agent.ConfiguredServerIds)
{
    var existingInstallation = installationsList.FirstOrDefault(i => i.ServerId == serverId);
    if (existingInstallation == null)
    {
        // Server is in agent's config but not tracked yet
        await InstallationManager.AddServerToAgentAsync(serverId, AgentId);
    }
}
```

## Expected Behavior (Post-Fix)

### First Launch
1. App starts with empty installations collection
2. User navigates to "Claude Code" agent page
3. **UI immediately syncs** with agent's config file
4. Finds "github" server in `~/.claude.json`
5. Creates installation record
6. Shows server as **"✓ Enabled"** with Configure/Disable/Remove buttons

### Subsequent Loads
1. Installations collection may still be empty (not persisted)
2. UI syncs again on each page load
3. Always shows correct state from agent's config file

### Background Worker
- Still runs every 5 minutes as a safety net
- Catches any new servers added externally
- Complements UI-side sync

## Benefits

✅ **Instant synchronization** - No 5-second wait for background worker
✅ **Correct UI state** - Always reflects agent's actual configuration
✅ **No duplicate config** - Won't overwrite existing server settings
✅ **Idempotent operations** - Safe to sync multiple times
✅ **Auto-install missing servers** - Servers in agent config but not in MCP Manager get installed
✅ **Preserves custom config** - Existing server configurations aren't overwritten

## Testing

To verify the fix:

1. **Add a server externally**:
   ```json
   // Edit ~/.claude.json
   {
     "mcpServers": {
       "test-server": {
         "type": "stdio",
         "command": "npx",
         "args": ["-y", "test-server"]
       }
     }
   }
   ```

2. **Launch the app** (or refresh if already running)

3. **Navigate to**: Agents → Claude Code

4. **Verify**:
   - "test-server" shows as **"✓ Enabled"**
   - Shows Configure/Disable/Remove buttons (not "Add to Agent")
   - Clicking Configure shows the existing configuration

## Future Improvements

Potential enhancements:
- **Persist installations to database** - Survive app restarts
- **Real-time sync** - Use file watcher events to update UI immediately
- **Bi-directional sync** - Detect when servers are removed from config files
- **Configuration comparison** - Show diff between MCP Manager config and agent config
