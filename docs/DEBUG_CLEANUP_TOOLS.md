# Debug Page Cleanup Tools

The Debug page (`/debug`) now includes automated cleanup tools to help resolve common sync issues and duplicates.

## Available Tools

### 1. Detect Duplicates

Finds servers with the same name but different IDs. Groups servers by name and shows which are tracked in installation records.

Use after upgrades or when duplicate servers appear in the UI.

Example output:
```
Found 2 duplicate group(s):
- Github: 2 servers
  - mcp_12js8Hu2bw (Not Tracked)
  - github (In Records)
```

### 2. Remove Duplicates

Removes duplicate servers automatically. Keeps tracked servers, removes untracked ones. If all tracked or untracked, keeps the first and removes the rest.

Use after confirming duplicates exist or when duplicate servers appear.

Example output:
```
Cleanup complete: Kept 2 server(s), removed 2 duplicate(s)
```

### 3. Sync Installation Records

Creates missing installation records for servers configured in agent config files. Matches by ID, falls back to name matching.

Use when servers show "Add to Agent" but are already configured, or when Debug page shows untracked servers.

Example output:
```
Created 3 missing installation record(s)
```

Or if everything is in sync:
```
All installation records are in sync ✓
```

### 4. Remove Orphaned Records

**Purpose**: Remove installation records that point to servers that no longer exist.

**What it does**:
1. Finds all installation records
2. Checks if the referenced server exists
3. Removes records for non-existent servers
4. Shows how many orphans were removed

**When to use**:
- After manually uninstalling servers
- When cleaning up old data
- When installation records reference servers that don't exist

**Example output**:
```
Removed 1 orphaned installation record(s)
```

Or if there are no orphans:
```
No orphaned records found ✓
```

## Typical Cleanup Workflow

If you're experiencing sync issues or duplicates after upgrading, follow this workflow:

### Step 1: Identify the Problem
1. Go to `/debug`
2. Click "🔍 Scan for Duplicates"
3. Review the duplicate groups shown
4. Check the "Sync Status" for each agent

### Step 2: Remove Duplicates (if found)
1. Click "🗑️ Remove Duplicates"
2. Review the success message
3. Verify on the "All Installed Servers" section that duplicates are gone

### Step 3: Sync Installation Records
1. Click "🔄 Sync Records"
2. This ensures all configured servers are properly tracked
3. Check that "Sync Status" now shows "✓ All servers tracked"

### Step 4: Clean Up Orphans
1. Click "🧹 Clean Orphans"
2. This removes any stale installation records
3. Verify the "Installation Records" table is clean

### Step 5: Verify
1. Click "🔄 Refresh" to reload all data
2. Navigate to your agent page (e.g., `/agents/claudecode`)
3. Verify servers show correctly as "✓ Enabled"

## Understanding the Status Messages

### Success (Green)
- ✓ Operation completed successfully
- Data is in sync
- No issues found

### Warning (Yellow)
- ⚠ Found issues that need attention
- Duplicates detected
- Servers not tracked

### Info (Blue)
- Operation in progress
- Informational messages

### Danger (Red)
- ✗ Operation failed
- Error occurred during cleanup

## Safety Notes

1. **All cleanup operations are reversible**:
   - Removed servers can be reinstalled
   - Missing installation records will be recreated by the background worker
   - The config files are never modified by cleanup tools

2. **Cleanup tools don't modify config files**:
   - They only work with the in-memory installation records
   - Your agent's config files (like `~/.claude.json`) remain unchanged

3. **Background worker respects cleanup**:
   - After cleanup, the background worker will detect the changes
   - It won't recreate duplicates (as of v0.1.4+)
   - It will recreate installation records if needed

## Troubleshooting

### "Duplicates keep coming back"
- Make sure you're running v0.1.4 or later
- The background worker had a bug in earlier versions that created duplicates
- Update to the latest version and run cleanup again

### "Installation records disappear after cleanup"
- This is normal - orphaned records are removed
- The background worker will recreate valid records
- Use "Sync Installation Records" to recreate them immediately

### "Server shows as 'Add to Agent' after cleanup"
- Click "Sync Installation Records" to recreate the missing record
- Or wait for the background worker to run (every 5 minutes)
- Navigate to a different page and back to refresh

### "Can't find the duplicate I'm looking for"
- Click "🔄 Refresh" to reload data
- Duplicates only show if they have the exact same name (case-insensitive)
- Check "All Installed Servers" section for all servers

## Version History

- **v0.1.5**: Added cleanup tools to Debug page
- **v0.1.4**: Fixed duplicate creation bug in background worker
- **v0.1.3**: Added UI-side sync and Debug page

## Related Documentation

- [Cleanup Duplicates Guide](CLEANUP_DUPLICATES.md) - Manual cleanup steps
- [Server ID Matching Fix](SERVER_ID_MATCHING_FIX.md) - Background on the ID mismatch issue
- [Debugging Sync Issues](DEBUGGING_SYNC_ISSUES.md) - General troubleshooting
