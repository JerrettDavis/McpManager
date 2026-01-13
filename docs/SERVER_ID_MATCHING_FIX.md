# Server ID Matching Fix (v0.1.3)

## Issue

Servers configured in agent config files weren't recognized in the UI because of **server ID mismatches** between:
- The ID in the agent's config file (e.g., `github`)
- The ID assigned when installing from a registry (e.g., `mcp_12js8Hu2bw`)
- The ID in installation tracking records

This caused the UI to show "Add to Agent" even though the server was already configured.

## Root Cause

When the `AgentServerSyncWorker` auto-discovered and installed a server:
1. Read config file: Found server `github`
2. Searched registries: Found matching server with ID `mcp_12js8Hu2bw`
3. Installed server: Used registry ID `mcp_12js8Hu2bw`
4. Created installation record: Used config file ID `github`

**Result:** The UI couldn't match the installed server (`mcp_12js8Hu2bw`) with the installation record (`github`).

## The Fix

### 1. Background Worker - Override Registry IDs

**Location**: `src/McpManager.Infrastructure/BackgroundWorkers/AgentServerSyncWorker.cs`

When installing a server found in a registry, we now **override the registry's ID** with the config file's ID:

```csharp
if (serverFromRegistry != null)
{
    // Override registry ID to match config file ID
    serverFromRegistry.Id = serverId;  // Use config file's ID
    await serverManager.InstallServerAsync(serverFromRegistry);
}
```

**Benefit**: The config file remains the source of truth for server IDs.

### 2. UI Sync - Match by Name as Fallback

**Location**: `src/McpManager.Web/Components/Pages/AgentDetails.razor`

When syncing on page load, we now try multiple strategies to find the server:

```csharp
foreach (var configServerId in agent.ConfiguredServerIds)
{
    // 1. Try exact ID match
    var server = await ServerManager.GetServerByIdAsync(configServerId);

    if (server == null)
    {
        // 2. Try matching by name (case-insensitive)
        var matchByName = allServers.FirstOrDefault(s =>
            s.Name.Equals(configServerId, StringComparison.OrdinalIgnoreCase));

        if (matchByName != null)
        {
            actualServerId = matchByName.Id;  // Use the matched server's ID
        }
    }

    // Create installation with the actual server ID
    await InstallationManager.AddServerToAgentAsync(actualServerId, AgentId);
}
```

**Benefit**: Handles both new installations and existing servers with different IDs.

### 3. Debug Page

**Location**: `src/McpManager.Web/Components/Pages/Debug.razor`

Added comprehensive debugging page (`/debug`) that shows:
- Config file IDs vs. installed server IDs
- Installation record mismatches
- Sync status for each agent

## Migration Path

If you already have servers installed with mismatched IDs (like `mcp_12js8Hu2bw` when your config says `github`), here's how to fix it:

### Option 1: Let the UI Fix It (Recommended)

1. **Restart the app** with the new version
2. **Navigate to the Agent Details page** (e.g., `/agents/claudecode`)
3. The UI will automatically:
   - Find the server by name (`github` → `Github`)
   - Create the installation record with the correct ID
   - Show the server as configured

### Option 2: Manual Cleanup

1. **Go to Installed Servers** page
2. **Remove the server** with the wrong ID (e.g., `mcp_12js8Hu2bw`)
3. **Restart the app**
4. The background worker will reinstall it with the correct ID from your config

## Expected Behavior (Post-Fix)

### New Installations
1. Config file has: `github`
2. Registry has: Server with ID `abc123` and name "Github"
3. **Installed as**: `github` (config ID, not registry ID)
4. **Installation record**: `github` → `claudecode`
5. **UI shows**: ✓ Enabled with Configure/Disable/Remove

### Existing Mismatches
1. Config file has: `github`
2. Already installed: Server with ID `mcp_12js8Hu2bw` and name "Github"
3. **UI matches by name**: Finds `mcp_12js8Hu2bw`
4. **Installation record**: `mcp_12js8Hu2bw` → `claudecode` (uses actual ID)
5. **UI shows**: ✓ Enabled with Configure/Disable/Remove

## Verification

After updating, check the Debug page (`/debug`):

**Before fix:**
```
Configured Server IDs: github
All Installed Servers: Github (mcp_12js8Hu2bw)
Installation Records: github → claudecode
Sync Status: ⚠ Mismatch
```

**After fix:**
```
Configured Server IDs: github
All Installed Servers: Github (github)  ← ID now matches config
Installation Records: github → claudecode
Sync Status: ✓ All servers tracked
```

Or with name matching:
```
Configured Server IDs: github
All Installed Servers: Github (mcp_12js8Hu2bw)  ← Old ID kept
Installation Records: mcp_12js8Hu2bw → claudecode  ← Updated to match
Sync Status: ✓ All servers tracked
```

## Console Output

Look for these new log messages:

```
[AgentDetails] Config has 'github', matched to installed server 'mcp_12js8Hu2bw' by name
[AgentDetails] Created installation: 'mcp_12js8Hu2bw' -> 'claudecode'
```

Or for new installations:
```
[AgentServerSyncWorker] Found server in registry with ID 'abc123', overriding to use config ID 'github'
[AgentServerSyncWorker] Auto-installed server 'github' from agent ClaudeCode
```

## Future Improvements

Potential enhancements:
- **Alias system**: Map multiple IDs to the same server
- **ID migration tool**: Automatically rename existing servers
- **Duplicate detection**: Warn when multiple servers match the same config entry
- **Config file editing**: Update config files when server IDs change
