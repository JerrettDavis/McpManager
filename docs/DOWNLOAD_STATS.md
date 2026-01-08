# Download Statistics System

## Overview
McpManager automatically fetches and caches download statistics from package registries (NPM, PyPI) to provide accurate popularity metrics for MCP servers.

## Architecture

### DownloadStatsService
**Location**: `src/McpManager.Infrastructure/Services/DownloadStatsService.cs`

Responsible for fetching download counts from external package registries:

#### Supported Registries
- **NPM**: Fetches weekly download counts from `api.npmjs.org`
- **PyPI**: Stub implementation (PyPI deprecated public download stats API)

#### Key Methods
- `GetNpmDownloadsAsync(packageName)` - Fetches NPM weekly downloads
- `GetPyPiDownloadsAsync(packageName)` - PyPI stub (returns null)
- `UpdateDownloadCountsAsync()` - Batch updates all stale servers

#### Package Name Extraction
Automatically extracts package names from install commands:
```bash
npm install @modelcontextprotocol/server-filesystem
  → @modelcontextprotocol/server-filesystem

npx -y @modelcontextprotocol/server-git
  → @modelcontextprotocol/server-git

pip install mcp-server-sqlite
  → mcp-server-sqlite
```

### DownloadStatsWorker
**Location**: `src/McpManager.Infrastructure/BackgroundWorkers/DownloadStatsWorker.cs`

Background service that periodically updates download statistics:

- **Runs**: Once per day
- **Initial Delay**: 5 minutes (allows registry cache to populate first)
- **Update Criteria**: Only updates servers with:
  - Zero download count
  - Download data older than 3 days

### Database Schema

#### CachedRegistryServerEntity
Added field:
```csharp
public DateTime? DownloadsLastUpdated { get; set; }
```

Tracks when download statistics were last refreshed for intelligent cache invalidation.

## Update Flow

1. **Background Worker Starts**
   - Waits 5 minutes for registries to populate
   - Queries database for all cached servers

2. **Filter Servers**
   - Select servers needing updates:
     - `DownloadCount == 0` (never fetched)
     - `LastUpdated > 3 days ago` (stale data)

3. **Fetch Downloads**
   - Extract package name from install command
   - Determine package type (NPM/PyPI)
   - Query appropriate registry API
   - Update database cache

4. **Repeat Daily**
   - Sleep for 24 hours
   - Repeat process

## Configuration

### Update Frequency
```csharp
private readonly TimeSpan _refreshInterval = TimeSpan.FromDays(1);
```

### Stale Threshold
```csharp
private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(3);
```

### HTTP Timeouts
```csharp
client.Timeout = TimeSpan.FromSeconds(10);
```

## API Endpoints Used

### NPM Registry
**Endpoint**: `https://api.npmjs.org/downloads/point/last-week/{package}`

**Response**:
```json
{
  "downloads": 12345,
  "package": "@modelcontextprotocol/server-filesystem"
}
```

**Rate Limits**: None documented, but we cache for 3+ days

### PyPI Registry
**Endpoint**: `https://pypi.org/pypi/{package}/json`

**Note**: Download statistics not available in API since 2023. Alternative solutions:
- pypistats.org API (requires API key)
- Google BigQuery (complex setup)
- For now, returns null

## Monitoring

### Logs
```
Download Stats Worker starting
Starting download statistics update
Updating download counts for 42 servers
Updated mcp-server-git: 15234 downloads
Download statistics update completed successfully
```

### Error Handling
- Failures logged as warnings, don't stop worker
- Individual server failures don't affect batch
- Network timeouts handled gracefully

## Performance

### Efficiency
- Only updates stale data (not all servers every time)
- Runs off-peak (once per day)
- Parallel fetching with Task.WhenAll
- 10-second HTTP timeout prevents hanging

### Load Impact
- ~100 servers = ~100 API calls per day
- NPM API has no rate limits
- Cached locally for 3+ days minimum
- Background processing doesn't block UI

## Future Enhancements

### PyPI Download Stats
Options to implement:
1. **pypistats.org API**
   ```python
   GET https://pypistats.org/api/packages/{package}/recent
   ```
   - Requires authentication
   - Rate limited

2. **Google BigQuery**
   - Official PyPI download data
   - Requires GCP account
   - Complex setup

3. **Estimate from GitHub Stars**
   - Fallback for packages without stats
   - Use GitHub API to estimate popularity

### Additional Metrics
- **GitHub Stars**: Repository popularity
- **Last Commit Date**: Activity indicator
- **Open Issues**: Maintenance health
- **Contributors**: Community size

### Caching Improvements
- Separate refresh intervals by popularity
- Hot packages: Update weekly
- Cold packages: Update monthly
- Redis cache for high-traffic scenarios

## Troubleshooting

### Downloads Not Updating
1. Check worker is running:
   ```
   grep "Download Stats Worker" logs
   ```

2. Verify network connectivity:
   ```bash
   curl https://api.npmjs.org/downloads/point/last-week/express
   ```

3. Check database for DownloadsLastUpdated field

### Incorrect Package Names
- Review ExtractPackageName logic
- Check install command format in database
- Add logging for extraction failures

### Rate Limiting
- If NPM starts rate limiting, increase cache duration
- Implement exponential backoff
- Add circuit breaker pattern

## Testing

### Manual Testing
```bash
# Run application
dotnet run --project src/McpManager.Web

# Check logs after 5 minutes
tail -f logs/app.log | grep "Download Stats"

# Query database
sqlite3 ~/.local/share/McpManager/mcpmanager.db
SELECT Name, DownloadCount, DownloadsLastUpdated 
FROM CachedRegistryServers 
WHERE DownloadCount > 0;
```

### Unit Testing
Test package name extraction:
```csharp
ExtractPackageName("npm install @mcp/server")
  → "@mcp/server"

ExtractPackageName("npx -y mcp-server-git")
  → "mcp-server-git"
```

## Related Documentation
- [Database Browser](DATABASE_BROWSER.md)
- [Background Workers](../src/McpManager.Infrastructure/BackgroundWorkers/)
- [Registry Cache Repository](../src/McpManager.Infrastructure/Persistence/Repositories/RegistryCacheRepository.cs)
