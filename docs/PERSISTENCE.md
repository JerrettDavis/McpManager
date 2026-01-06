# Persistence & Caching Architecture

## Overview

MCP Manager uses **Entity Framework Core** with **SQLite** to provide durable persistence and intelligent caching for MCP servers and registry data. This enables offline usage, faster startup, better observability, and a reliable local source of truth.

## Database Location

The SQLite database is stored at:
- **Windows**: `%LOCALAPPDATA%\McpManager\mcpmanager.db`
- **macOS/Linux**: `~/.local/share/McpManager/mcpmanager.db`

The database is automatically created and migrated on application startup.

---

## Database Schema

### InstalledServers Table

Stores all locally installed MCP servers with full metadata.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | TEXT (PK) | Unique server identifier |
| `Name` | TEXT | Display name |
| `Description` | TEXT | Server description |
| `Version` | TEXT | Server version |
| `Author` | TEXT | Author/maintainer |
| `RepositoryUrl` | TEXT | Repository URL |
| `InstallCommand` | TEXT | Installation command |
| `TagsJson` | TEXT | Server tags (JSON array) |
| `InstalledAt` | DATETIME | Installation timestamp |
| `UpdatedAt` | DATETIME | Last update timestamp |
| `ConfigurationJson` | TEXT | **Server configuration (JSON object)** |
| `RegistrySource` | TEXT | Origin registry name |
| `InstallLocation` | TEXT | Installation path |

**Key Feature: Configuration Persistence**
- The `ConfigurationJson` field stores the complete server configuration as JSON
- Global server configuration is preserved across restarts
- Configuration updates are automatically persisted
- Supports complex configuration values (nested JSON, multiline strings, special characters)

### CachedRegistryServers Table

Caches MCP server entries from remote registries for offline browsing and faster UI.

| Column | Type | Description |
|--------|------|-------------|
| `Id` | TEXT (PK) | Composite: `{RegistryName}::{ServerId}` |
| `RegistryName` | TEXT | Source registry name |
| `ServerId` | TEXT | Server ID in registry |
| `Name` | TEXT | Display name |
| `Description` | TEXT | Server description |
| `Version` | TEXT | Server version |
| `Author` | TEXT | Author/maintainer |
| `RepositoryUrl` | TEXT | Repository URL |
| `InstallCommand` | TEXT | Installation command |
| `TagsJson` | TEXT | Server tags (JSON array) |
| `DownloadCount` | INTEGER | Popularity metric |
| `Score` | REAL | Search relevance score |
| `LastUpdatedInRegistry` | DATETIME | Registry update timestamp |
| `FetchedAt` | DATETIME | Cache fetch timestamp |
| `MetadataJson` | TEXT | Additional metadata (JSON) |

### RegistryMetadata Table

Tracks registry refresh operations and scheduling.

| Column | Type | Description |
|--------|------|-------------|
| `RegistryName` | TEXT (PK) | Registry identifier |
| `LastRefreshAt` | DATETIME | Last successful refresh |
| `NextRefreshAt` | DATETIME | Next scheduled refresh |
| `TotalServersCached` | INTEGER | Number of cached servers |
| `LastRefreshSuccessful` | BOOLEAN | Success status |
| `LastRefreshError` | TEXT | Error message (if failed) |
| `RefreshIntervalMinutes` | INTEGER | Refresh interval (default: 60) |

---

## Caching Strategy

### Read-Through Caching Pattern

```
UI Request → CachedServerRegistry → Check Cache Age
  ↓
  └─ If Fresh: Return from local database
  └─ If Stale: Return from local database (background worker will update)
```

### Background Refresh

- **Worker**: `RegistryRefreshWorker`
- **Interval**: Configurable per registry (default: 60 minutes)
- **Startup Delay**: 30 seconds to allow application initialization
- **Behavior**: Non-blocking, runs asynchronously in background

### Cache Freshness

Cache age is determined by comparing `FetchedAt` timestamp with configurable max age:

```csharp
bool isCacheStale = await _cacheRepository.IsCacheStaleAsync(registryName, TimeSpan.FromHours(1));
```

**Default Policy**:
- Cache max age: 1 hour
- Background refresh: Every 60 minutes
- UI always reads from cache (never blocks on remote calls)

---

## Repository Interfaces

### IServerRepository

Manages locally installed MCP servers with full CRUD operations.

```csharp
Task<IEnumerable<McpServer>> GetAllAsync();
Task<McpServer?> GetByIdAsync(string serverId);
Task<bool> AddAsync(McpServer server);
Task<bool> UpdateAsync(McpServer server);
Task<bool> DeleteAsync(string serverId);
Task<bool> ExistsAsync(string serverId);
```

**Configuration Handling**:
- `AddAsync()`: Persists initial server configuration
- `UpdateAsync()`: Updates configuration when changed
- `GetByIdAsync()` / `GetAllAsync()`: Retrieves servers with their full configuration

### IRegistryCacheRepository

Manages cached registry data for offline and fast access.

```csharp
Task<IEnumerable<ServerSearchResult>> GetByRegistryAsync(string registryName);
Task<IEnumerable<ServerSearchResult>> GetAllAsync();
Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults);
Task<ServerSearchResult?> GetByIdAsync(string registryName, string serverId);
Task<int> UpsertManyAsync(string registryName, IEnumerable<ServerSearchResult> servers);
Task<DateTime?> GetLastRefreshTimeAsync(string registryName);
Task UpdateRegistryMetadataAsync(string registryName, int serverCount, bool success, string? error);
Task<IEnumerable<string>> GetRegistriesNeedingRefreshAsync();
Task<bool> IsCacheStaleAsync(string registryName, TimeSpan maxAge);
```

---

## Cached Registry Wrapper

### CachedServerRegistry

Wraps any `IServerRegistry` implementation with transparent read-through caching:

```csharp
public CachedServerRegistry(
    IServerRegistry innerRegistry, 
    IRegistryCacheRepository cacheRepository,
    TimeSpan? cacheMaxAge = null)
```

**Behavior**:
1. Check if cache is stale
2. If fresh: return from cache
3. If stale: return from cache, background worker will update
4. Never blocks on remote registry calls

---

## Background Worker

### RegistryRefreshWorker

Periodically refreshes all registered MCP registries in the background.

**Configuration**:
- Startup delay: 30 seconds
- Default refresh interval: 60 minutes
- Runs as `IHostedService`

**Workflow**:
1. Waits for startup delay
2. Fetches all servers from each remote registry
3. Upserts servers into cache (preserves existing data)
4. Updates registry metadata (timestamp, count, status)
5. Logs errors without failing the application
6. Repeats after refresh interval

**Error Handling**:
- Exceptions are logged but don't crash the worker
- Failed registries are marked with `LastRefreshSuccessful = false`
- Error messages stored in `LastRefreshError`

---

## Offline Behavior

### When Offline or Remote Registry Unavailable

1. **UI remains functional**: All queries read from local cache
2. **Last cached data is displayed**: Cache timestamp visible to users
3. **Background worker logs errors**: No disruption to application
4. **Configuration persists**: Installed server configuration remains available

### Data Staleness Indicators

Future enhancement: UI can display cache age:
```
"Last updated: 45 minutes ago"
"Cached 2 hours ago (refresh in progress)"
```

---

## Migrations

### Creating a New Migration

```bash
cd src/McpManager.Infrastructure
dotnet ef migrations add MigrationName --context McpManagerDbContext
```

### Applying Migrations

Migrations are **automatically applied on startup** via:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<McpManagerDbContext>();
    dbContext.Database.Migrate();
}
```

---

## Testing

### Unit Tests

Repository layer tests use **EF Core In-Memory provider**:

```csharp
var options = new DbContextOptionsBuilder<McpManagerDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;

var context = new McpManagerDbContext(options);
var repository = new ServerRepository(context);
```

**Tests include**:
- CRUD operations
- Configuration persistence (including complex values)
- Cache upsert behavior
- Registry metadata tracking

### Integration Tests

Future enhancement: SQLite-based integration tests for:
- Migration compatibility
- Concurrent access patterns
- Database locking scenarios

---

## Performance Considerations

### Database Size

- **Typical size**: <10 MB for hundreds of servers
- **Growth rate**: Minimal (registry cache is bounded by available servers)
- **Maintenance**: No cleanup required (upsert pattern prevents duplication)

### Query Performance

- **Indexes**: Created on frequently queried columns
  - `InstalledServers.Name`
  - `InstalledServers.InstalledAt`
  - `CachedRegistryServers.RegistryName, ServerId`
  - `CachedRegistryServers.Name`
  - `CachedRegistryServers.FetchedAt`
  - `RegistryMetadata.NextRefreshAt`

- **Read optimization**: In-memory caching via repository pattern
- **Write optimization**: Batch upserts for registry refresh

---

## Future Enhancements

### Planned Features

1. **Cache Expiration Policy**: Configurable TTL per registry
2. **Compression**: JSON compression for large configurations
3. **Backup/Restore**: Export/import installed servers
4. **Sync**: Cross-device server configuration synchronization
5. **Analytics**: Track server usage and popularity
6. **Version History**: Track server update history

### Schema Evolution

The schema is designed for forward compatibility:
- Optional columns for future features
- JSON fields for flexible metadata
- Indexed columns for performance

---

## Troubleshooting

### Database Locked

**Symptom**: `SqliteException: database is locked`

**Solution**: Close other MCP Manager instances or restart the application

### Migration Failures

**Symptom**: `DbUpdateException` on startup

**Solution**: 
1. Back up database file
2. Delete database file
3. Restart application (database will be recreated)

### Corrupted Database

**Symptom**: Random errors, crashes on startup

**Solution**:
1. Back up database file
2. Delete database file: `rm ~/.local/share/McpManager/mcpmanager.db`
3. Restart application (all data will be lost)

---

## Configuration Persistence Implementation

Server configuration is automatically persisted using JSON serialization:

### Storage Format

```json
{
  "apiKey": "your-api-key",
  "endpoint": "https://api.example.com",
  "timeout": "30",
  "features": "{\"nested\":\"value\"}"
}
```

### Code Examples

**Installing a server with configuration:**
```csharp
var server = new McpServer
{
    Id = "my-server",
    Name = "My Server",
    Configuration = new Dictionary<string, string>
    {
        ["apiKey"] = "secret-key",
        ["endpoint"] = "https://api.example.com"
    }
};

await serverManager.InstallServerAsync(server);
// Configuration is automatically persisted to database
```

**Updating server configuration:**
```csharp
var config = new Dictionary<string, string>
{
    ["apiKey"] = "new-key",
    ["timeout"] = "60"
};

await serverManager.UpdateServerConfigurationAsync("my-server", config);
// Updated configuration is persisted immediately
```

**Retrieving server with configuration:**
```csharp
var server = await serverManager.GetServerByIdAsync("my-server");
// server.Configuration contains the persisted configuration
var apiKey = server.Configuration["apiKey"];
```

### Configuration Features

- **Automatic serialization**: Dictionary ↔ JSON
- **Support for complex values**: Nested JSON, multiline strings, special characters
- **Immediate persistence**: Changes written on update
- **No size limits**: Store configurations of any size
- **Type safety**: Strongly typed Dictionary<string, string> in code

---

## Summary

MCP Manager's persistence layer provides:

✅ **Durable storage** for installed servers and configuration  
✅ **Intelligent caching** of registry data  
✅ **Offline functionality** via local database  
✅ **Fast startup** with cached data  
✅ **Background refresh** without UI blocking  
✅ **Automatic migrations** for schema evolution  
✅ **Comprehensive testing** with in-memory provider  
✅ **Configuration persistence** for installed servers  

The architecture follows **clean architecture principles** with clear separation between domain, application, and infrastructure concerns.
