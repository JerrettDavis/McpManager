# Desktop App Background Workers

## Issue

The Desktop app was not auto-installing MCP servers detected in agents because the background workers were not being started.

## Root Cause

**Web App** (`src/McpManager.Web/Program.cs`):
- Uses ASP.NET Core's built-in hosted service infrastructure
- Background workers are registered with `AddHostedService<T>()`
- ASP.NET Core automatically starts and manages their lifecycle

**Desktop App** (`src/McpManager.Desktop/Program.cs`):
- Uses Photino.Blazor instead of ASP.NET Core
- Photino does **not** have `IHostedService` support
- Background workers were registered but never started

## The Fix

Updated `src/McpManager.Desktop/Program.cs` to manually start background workers:

```csharp
// Register background workers as singletons
appBuilder.Services.AddSingleton<RegistryRefreshWorker>();
appBuilder.Services.AddSingleton<AgentServerSyncWorker>();
appBuilder.Services.AddSingleton<DownloadStatsWorker>();
appBuilder.Services.AddSingleton<ConfigurationWatcherWorker>();

// Start them manually after app is built
var cancellationTokenSource = new CancellationTokenSource();
StartBackgroundWorkers(app.Services, cancellationTokenSource.Token);
```

## Background Workers

### 1. **AgentServerSyncWorker** (Most Important for Your Issue)
   - **Purpose**: Auto-installs servers detected in agent configurations
   - **Interval**: Every 5 minutes, plus initial sync after 5 seconds
   - **What it does**:
     1. Detects installed agents (Claude Desktop, Claude Code, GitHub Copilot, etc.)
     2. Reads their configuration files to find configured MCP servers
     3. Searches registries for matching servers
     4. Auto-installs any servers not already in MCP Manager
     5. Creates agent-server relationships

### 2. **RegistryRefreshWorker**
   - **Purpose**: Refreshes cached registry data
   - **Interval**: Every 60 minutes
   - **What it does**:
     1. Fetches latest server lists from all registries
     2. Updates the database cache
     3. Ensures browse/search results are up-to-date

### 3. **DownloadStatsWorker**
   - **Purpose**: Updates package download statistics
   - **Interval**: Every 120 minutes
   - **What it does**:
     1. Fetches download counts from npm, PyPI, etc.
     2. Updates popularity metrics for servers

### 4. **ConfigurationWatcherWorker**
   - **Purpose**: Monitors agent config files for external changes
   - **What it does**:
     1. Watches `~/.claude.json` and other agent config files
     2. Raises events when files change
     3. Enables real-time UI updates when configs are edited externally

## Lifecycle Management

**Web App**: Handled by ASP.NET Core
- Workers start automatically with the application
- Workers stop gracefully on shutdown

**Desktop App**: Manual management (post-fix)
- Workers are started manually after app builds
- Cancellation token is canceled on ProcessExit
- Workers stop gracefully on application exit

## Expected Behavior (Post-Fix)

When you start the Desktop app:
1. After 5 seconds, `AgentServerSyncWorker` runs its initial sync
2. It detects your `~/.claude.json` file and finds the "github" server
3. It searches registries for a matching server definition
4. If found in a registry, it installs the full server metadata
5. If not found, it creates a minimal entry marked as "auto-discovered"
6. It creates the agent-server relationship in the database
7. The server appears in your installed servers list
8. Every 5 minutes, it checks again for new servers

## Verification

To verify the fix is working:

1. **Build and run the Desktop app**:
   ```bash
   dotnet build src/McpManager.Desktop
   dotnet run --project src/McpManager.Desktop
   ```

2. **Check logs** (console output):
   - You should see: "Agent Server Sync Worker starting"
   - After 5 seconds: "Syncing servers from X detected agent(s)"
   - Look for: "Auto-installed server 'github' from agent claudecode"

3. **Check the UI**:
   - Navigate to "Installed Servers"
   - You should see any servers from your `~/.claude.json` file
   - They should be linked to the "Claude Code" agent

## Troubleshooting

If servers still aren't auto-installing:

1. **Check if Claude Code is detected**:
   - Go to the "Agents" page
   - Verify "Claude Code" shows as detected
   - Check that it shows the correct config path

2. **Check logs for errors**:
   - Look for "Agent sync worker error" messages
   - Check for "Failed to sync agent servers" errors

3. **Verify config file exists**:
   - Ensure `~/.claude.json` exists
   - Verify it has valid JSON
   - Check the `mcpServers` section has entries

4. **Manual sync**:
   - The sync runs every 5 minutes
   - Restart the app to trigger an immediate sync
   - Or wait 5 seconds + 5 minutes for the next automatic sync
