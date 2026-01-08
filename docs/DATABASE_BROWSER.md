# Database-Driven MCP Server Browser

## Overview
The MCP server browser now operates entirely from the local database cache, providing instant search results and live filtering without hitting external registry APIs.

## Architecture

### Before
- BrowseServers page directly called `IServerRegistry` instances
- Each search/browse hit external APIs (with caching layer)
- Search required clicking a button
- Multiple registries loaded progressively

### After
- BrowseServers page calls `IServerBrowseService` 
- All queries run against local SQLite database
- Search triggers automatically on typing (debounced 300ms)
- Single efficient database query with all filters applied

## Components

### ServerBrowseService
**Location**: `src/McpManager.Application/Services/ServerBrowseService.cs`

Provides efficient database querying with:
- **SearchServersAsync**: Main query method with filters
  - Search query (LIKE search on name, description, tags)
  - Registry filter
  - Category/tag filter
  - Sorting (downloads, recent, name, score)
  - Pagination (page number and size)
- **GetAvailableCategoriesAsync**: Returns all unique tags from cached servers
- **GetRegistriesAsync**: Returns list of registries with server counts

### Updated BrowseServers Page
**Location**: `src/McpManager.Web/Components/Pages/BrowseServers.razor`

Key changes:
- Removed `IEnumerable<IServerRegistry>` injection
- Added `IServerBrowseService` injection  
- Removed search button
- Added debounced live search (300ms delay)
- Simplified loading states (no progressive loading)
- All data from single database query

## User Experience Improvements

### Live Search
- Search box updates results as you type
- 300ms debounce prevents excessive queries
- Spinner shows during search
- Instant feedback

### Better Performance
- No network calls during browsing
- Instant filter/sort changes
- Database indexes provide fast queries
- Pagination handled efficiently

### Consistent Data
- All users see same data (from cache)
- Background worker keeps data fresh (hourly)
- Predictable query times

## Background Workers

### RegistryRefreshWorker
**Location**: `src/McpManager.Infrastructure/BackgroundWorkers/RegistryRefreshWorker.cs`

**Unchanged** - still populates database cache:
- Runs every 60 minutes
- Fetches from external registries
- Updates database via `IRegistryCacheRepository`
- Operates independently of UI

## Database Schema

### CachedRegistryServerEntity
Stores all server information:
- Id (composite: registryName::serverId)
- Name, Description, Version, Author
- RepositoryUrl, InstallCommand
- TagsJson (JSON array)
- DownloadCount, Score
- LastUpdatedInRegistry, FetchedAt

### Indexes
SQLite provides efficient queries on:
- Primary key (Id)
- Registry name filtering
- LIKE searches on text fields

## Configuration

### Service Registration
**Location**: `src/McpManager.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<IServerBrowseService, ServerBrowseService>();
```

## Testing

### Manual Testing
1. Run application: `dotnet run --project src/McpManager.Web`
2. Navigate to /servers/browse
3. Type in search box - results update live
4. Change filters - instant updates
5. Check pagination works

### Verify Background Worker
1. Check logs for "Registry Refresh Worker starting"
2. Wait 2 seconds (initial delay)
3. See "Starting registry refresh for X registries"
4. Database populates automatically

## Future Enhancements

### Possible Improvements
1. Full-text search with ranking
2. Search suggestions/autocomplete
3. Advanced filters (author, date range)
4. Saved searches
5. Search history
6. Export results

### Performance Optimization
1. Add database indexes for common queries
2. Cache category list
3. Virtual scrolling for large result sets
4. Progressive loading indicator

## Troubleshooting

### No Servers Show Up
- Check if database exists: `~/.local/share/McpManager/mcpmanager.db`
- Wait 2 minutes for initial refresh
- Check logs for refresh errors

### Search Not Working
- Verify IServerBrowseService is registered
- Check browser console for JS errors
- Ensure debounce timer is working

### Slow Queries
- Check database size
- Verify SQLite indexes exist
- Review query execution plan

## Migration Notes

### Breaking Changes
- None - backward compatible

### Deployment Notes
- Database auto-creates on first run
- Background worker populates cache automatically
- No manual migration needed

## Documentation Links
- [Versioning System](VERSIONING.md)
- [Desktop Deployment](DESKTOP_DEPLOYMENT.md)
- [Code Signing](CODE_SIGNING.md)
