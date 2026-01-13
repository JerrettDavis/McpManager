# Debugging Sync Issues

If servers from your agent configuration files (like `~/.claude.json`) aren't showing up correctly in the UI, follow these steps to diagnose the issue.

## Step 1: Check the Debug Page

1. **Build and run the app**:
   ```bash
   dotnet build
   dotnet run --project src/McpManager.Web
   # Or for Desktop:
   dotnet run --project src/McpManager.Desktop
   ```

2. **Navigate to the Debug page**:
   - Click "Debug Info" in the left sidebar
   - Or go to: `http://localhost:5000/debug`

3. **Check the following**:
   - ✅ Is your agent detected? (e.g., "Claude Code")
   - ✅ Does the config path exist?
   - ✅ Are server IDs listed under "Configured Server IDs (from config file)"?
   - ✅ Are there installation records?
   - ⚠️ Look for "server(s) not tracked" warnings

4. **Expand "Show Config File Contents"**:
   - Verify your `~/.claude.json` file is being read correctly
   - Check that the `mcpServers` section has your servers

## Step 2: Check Console Logs

When the app starts, the console will show detailed logs like:

```
[ClaudeCodeConnector] Checking user config at: C:\Users\jd\.claude.json
[ClaudeCodeConnector] File exists: True
[ClaudeCodeConnector] Config file size: 12345 bytes
[ClaudeCodeConnector] Found 1 user-level MCP servers
[ClaudeCodeConnector] Adding server: github
```

**Look for:**
- ✅ "File exists: True" - Your config file is found
- ✅ "Found X user-level MCP servers" - Servers are detected
- ❌ "File exists: False" - Config file not found (wrong path?)
- ❌ "Error reading user config" - JSON parsing error

## Step 3: Check for Sync Errors

On the Agent Details page (e.g., `/agents/claudecode`):
- Look for a yellow warning banner at the top
- If present, it will show sync errors like:
  - "Error syncing server 'github': ..."

## Common Issues and Solutions

### Issue 1: Config File Not Found

**Symptoms:**
- Console shows: `File exists: False`
- Debug page shows: Config File Exists = ✗ No

**Solutions:**
1. Verify the file exists at `~/.claude.json` (not `~/.claude/settings.json`)
2. On Windows, this should be: `C:\Users\YourUsername\.claude.json`
3. Check file permissions - the app needs read access

### Issue 2: Servers Detected But Not Tracked

**Symptoms:**
- Debug page shows servers under "Configured Server IDs"
- But "Installation Records" is empty or missing servers
- Console shows servers being added
- UI still shows "Add to Agent" button

**Solutions:**
1. Check for sync errors on the Agent Details page
2. Look for exceptions in the console
3. Try clicking the "🔄 Refresh" button on the Agent Details page
4. Restart the app (installations are in-memory, not persisted)

### Issue 3: JSON Parsing Errors

**Symptoms:**
- Console shows: `Error reading user config: ...`
- Debug page shows: Config file exists but no servers detected

**Solutions:**
1. Validate your JSON at https://jsonlint.com
2. Common issues:
   - Missing commas between properties
   - Trailing commas (not allowed in JSON)
   - Incorrect quotes (use double quotes, not single)
3. Example valid structure:
   ```json
   {
     "mcpServers": {
       "github": {
         "type": "http",
         "url": "https://api.githubcopilot.com/mcp/"
       }
     }
   }
   ```

### Issue 4: Case Sensitivity

**Symptoms:**
- Servers show as "Not Configured" even though they're in the file

**Possible causes:**
1. Server ID mismatch (e.g., "GitHub" vs "github")
2. The connector uses the exact ID from the config file
3. MCP Manager might have installed it with a different ID

**Solutions:**
1. Check the Debug page to see both:
   - "Configured Server IDs (from config file)"
   - "All Installed Servers"
2. Ensure IDs match exactly (case-sensitive)

### Issue 5: Installation Records Lost on Restart

**Symptoms:**
- Servers show correctly after app start
- After restart, they need to be synced again
- "Active Installations" count is 0 on dashboard after restart

**Explanation:**
- Installation records are stored **in-memory only** (not persisted to database)
- This is by design for the current version
- The UI syncs automatically on page load to rebuild the relationships

**Solutions:**
- This is normal behavior
- The sync happens automatically when you visit agent pages
- Future version will persist installations to the database

## Step 4: Manual Sync

If automatic sync isn't working:

1. Go to the Agent Details page (e.g., `/agents/claudecode`)
2. Click the "🔄 Refresh" button in the Quick Actions panel
3. This triggers a fresh sync with the config file

## Step 5: Force Re-sync

If nothing works:

1. **Stop the app**
2. **Verify your config file**:
   ```bash
   # On Windows:
   notepad %USERPROFILE%\.claude.json

   # On Linux/Mac:
   cat ~/.claude.json
   ```
3. **Restart the app**
4. **Wait 5 seconds** for background sync to complete
5. **Navigate to** `/agents/claudecode`
6. **Check the Debug page**

## Reporting Issues

If you're still having problems, please provide:

1. **Debug page screenshot** - Shows detection status
2. **Console logs** - Copy relevant `[ClaudeCodeConnector]` lines
3. **Config file** - Sanitize sensitive data first
4. **Operating system** - Windows/Linux/Mac
5. **App version** - Shown in bottom-left of sidebar

## Example Debug Session

**Good state:**
```
Debug page shows:
  ✓ Config File Exists: Yes
  ✓ Configured Server IDs: github
  ✓ Installation Records: github (Enabled)
  ✓ Sync Status: All servers tracked

Console shows:
  [ClaudeCodeConnector] File exists: True
  [ClaudeCodeConnector] Found 1 user-level MCP servers
  [ClaudeCodeConnector] Adding server: github

UI shows:
  ✓ Enabled button with Configure/Disable/Remove options
```

**Problem state:**
```
Debug page shows:
  ✓ Config File Exists: Yes
  ✓ Configured Server IDs: github
  ✗ Installation Records: (empty)
  ⚠ Sync Status: 1 server(s) not tracked - github

Agent Details page shows:
  ⚠ Sync Warning: Error syncing server 'github': ...

UI shows:
  ➕ Add to Agent button (wrong!)
```

## Next Steps

Based on what you find:
- File not found → Check path and permissions
- Parsing error → Validate JSON
- Sync error → Check error message details
- Everything looks good but still wrong → File an issue with debug info
