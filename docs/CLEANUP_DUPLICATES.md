# Cleaning Up Duplicate Servers

If you upgraded to v0.1.3 and see duplicate servers (same name but different IDs), you can use the automated cleanup tools on the Debug page.

## Automated Cleanup (v0.1.5+)

**Recommended**: Use the automated cleanup tools on the Debug page (`/debug`):

1. **Go to the Debug page** (`/debug`)
2. **Click "🔍 Scan for Duplicates"** to identify duplicate servers
3. **Click "🗑️ Remove Duplicates"** to automatically remove them
4. **Click "🔄 Sync Records"** to ensure installation records are correct
5. **Click "🧹 Clean Orphans"** to remove any orphaned records
6. **Verify** on your agent page that servers show correctly

See [Debug Cleanup Tools](DEBUG_CLEANUP_TOOLS.md) for detailed documentation on the automated tools.

## Manual Cleanup (if needed)

If you prefer to manually clean up duplicates, follow these steps:

## Why Duplicates Exist

Before v0.1.3, when the app auto-discovered servers from your config files:
1. It found your config entry (e.g., `github`)
2. Searched registries and found a match (with ID like `mcp_12js8Hu2bw`)
3. Installed it with the registry's ID
4. Created installation tracking with the config ID

After v0.1.3, it now:
1. Uses the config file's ID for consistency
2. This created a second server with ID `github`
3. Result: Two servers with the same name but different IDs

## How to Identify Duplicates

1. **Go to the Debug page** (`/debug`)
2. **Look at "All Installed Servers"**
3. **Find servers with the same name but different IDs**:
   ```
   Github (mcp_12js8Hu2bw)  ← Old one
   Github (github)           ← New one
   ```

## How to Remove Duplicates

### Step 1: Identify Which to Keep

Check the **"Installation Records"** section on the Debug page:
- If it shows: `github → claudecode` - Keep the one with ID `github`
- If it shows: `mcp_12js8Hu2bw → claudecode` - Keep the one with ID `mcp_12js8Hu2bw`

**Rule**: Keep the server whose ID matches the installation record.

### Step 2: Remove the Duplicate

1. **Go to "Installed Servers"** page
2. **Find the duplicate server** (the one NOT in the installation records)
3. **Click the trash icon** 🗑️ to uninstall it
4. **Confirm the removal**

### Step 3: Verify

1. **Go back to the Debug page**
2. **Check "All Installed Servers"** - Should only show one now
3. **Navigate to your agent page** (e.g., `/agents/claudecode`)
4. **Verify the server shows correctly** - Should be "✓ Enabled"

## Example Cleanup

**Debug page shows:**
```
All Installed Servers:
  - Github (mcp_12js8Hu2bw)
  - Github (github)

Installation Records:
  - github → claudecode
```

**Action:**
- Remove: `Github (mcp_12js8Hu2bw)` ← This one is NOT in installation records
- Keep: `Github (github)` ← This one IS in installation records

**After cleanup:**
```
All Installed Servers:
  - Github (github)

Installation Records:
  - github → claudecode
```

## Alternative: Remove Both and Let It Reinstall

If you're unsure which to keep:

1. **Go to "Installed Servers"**
2. **Remove BOTH duplicates**
3. **Restart the app**
4. **Wait 5 seconds** for the background worker
5. The app will automatically reinstall from your config with the correct ID

## Prevention

After v0.1.3, duplicates won't be created anymore because:
- The background worker checks for existing servers by name before installing
- The UI sync uses existing servers instead of creating new ones
- Only one server per name will be created going forward

## Still Have Issues?

If you still see duplicates or problems after cleanup:

1. **Check the Debug page** for sync errors
2. **Check console logs** for error messages
3. **Restart the app** and try again
4. **Report the issue** with:
   - Debug page screenshot
   - Console logs
   - Your config file (sanitize sensitive data)
