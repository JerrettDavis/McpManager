using McpManager.Core.Interfaces;
using McpManager.Infrastructure.BackgroundWorkers;
using McpManager.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace McpManager.Tests.EndToEnd;

/// <summary>
/// End-to-end tests that simulate the full application flow:
/// 1. App starts
/// 2. RegistryRefreshWorker populates cache
/// 3. UI queries cache and displays servers
/// </summary>
public class RegistryEndToEndTests(ITestOutputHelper output)
{
    [Fact]
    public async Task FullApplicationFlow_RegistriesPopulateCacheAndUICanQuery()
    {
        // Arrange - simulate full app startup
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        var serviceProvider = services.BuildServiceProvider();

        // Ensure clean database for each test
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.McpManagerDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        output.WriteLine("=== STEP 1: Verify Registries Are Registered ===");
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        output.WriteLine($"Found {registries.Count} registries:");
        foreach (var registry in registries)
        {
            output.WriteLine($"  - {registry.Name}");
        }
        Assert.True(registries.Count >= 4, $"Expected at least 4 registries, found {registries.Count}");

        output.WriteLine("\n=== STEP 2: Manually Trigger Registry Refresh (Simulating Background Worker) ===");
        var cacheRepo = serviceProvider.GetRequiredService<IRegistryCacheRepository>();

        var totalCached = 0;
        foreach (var registry in registries)
        {
            if (registry is ICachedServerRegistry)
            {
                output.WriteLine($"Skipping {registry.Name} (cached wrapper)");
                continue;
            }

            try
            {
                output.WriteLine($"Refreshing {registry.Name}...");
                var startTime = DateTime.UtcNow;
                var servers = await registry.GetAllServersAsync();
                var serverList = servers.ToList();
                var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

                output.WriteLine($"  Fetched {serverList.Count} servers in {duration:F0}ms");

                // Cache the servers
                var cachedCount = await cacheRepo.UpsertManyAsync(registry.Name, serverList);
                await cacheRepo.UpdateRegistryMetadataAsync(registry.Name, cachedCount, true);

                output.WriteLine($"  Cached {cachedCount} servers");
                totalCached += cachedCount;
            }
            catch (Exception ex)
            {
                output.WriteLine($"  Error: {ex.Message}");
                await cacheRepo.UpdateRegistryMetadataAsync(registry.Name, 0, false, ex.Message);
            }
        }

        output.WriteLine($"\nTotal servers cached: {totalCached}");
        Assert.True(totalCached >= 7, $"Expected at least 7 cached servers (from GitHub), got {totalCached}");

        output.WriteLine("\n=== STEP 3: Query Cache (What UI Would See) ===");

        // Query all cached servers
        var allCachedServers = await cacheRepo.GetAllAsync();
        var cachedList = allCachedServers.ToList();
        output.WriteLine($"UI would see {cachedList.Count} servers total");

        // Break down by registry
        var byRegistry = cachedList
            .GroupBy(s => s.RegistryName)
            .OrderByDescending(g => g.Count())
            .ToList();

        output.WriteLine("\nServers by registry:");
        foreach (var group in byRegistry)
        {
            output.WriteLine($"  {group.Key}: {group.Count()} servers");

            // Show sample servers
            var samples = group.Take(3).ToList();
            foreach (var sample in samples)
            {
                output.WriteLine($"    - {sample.Server.Name} ({sample.Server.Id})");
            }
            if (group.Count() > 3)
            {
                output.WriteLine($"    ... and {group.Count() - 3} more");
            }
        }

        // Assert - UI should see servers from multiple registries
        Assert.NotEmpty(cachedList);
        Assert.True(byRegistry.Count >= 2, $"Expected servers from at least 2 registries, got {byRegistry.Count}");

        output.WriteLine("\n=== STEP 4: Test Search Functionality (What BrowseServers.SearchAsync Would Do) ===");
        var searchResults = await cacheRepo.SearchAsync("git", 50);
        var searchList = searchResults.ToList();
        output.WriteLine($"Search for 'git' returned {searchList.Count} results:");
        foreach (var result in searchList.Take(5))
        {
            output.WriteLine($"  - {result.Server.Name} from {result.RegistryName}");
        }

        Assert.NotEmpty(searchList);

        output.WriteLine("\n=== STEP 5: Verify Cache Metadata ===");
        foreach (var registry in registries.Where(r => r is not ICachedServerRegistry))
        {
            var lastRefresh = await cacheRepo.GetLastRefreshTimeAsync(registry.Name);
            var serverCount = (await cacheRepo.GetByRegistryAsync(registry.Name)).Count();

            output.WriteLine($"{registry.Name}:");
            output.WriteLine($"  Last refresh: {lastRefresh?.ToString() ?? "Never"}");
            output.WriteLine($"  Server count: {serverCount}");
        }

        output.WriteLine("\n✓ End-to-End Test Complete");
    }

    [Fact]
    public async Task RegistryRefreshWorker_PopulatesCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));

        var serviceProvider = services.BuildServiceProvider();

        // Ensure clean database for each test
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.McpManagerDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        output.WriteLine("=== Testing RegistryRefreshWorker Logic ===");

        // Get dependencies
        var cacheRepo = serviceProvider.GetRequiredService<IRegistryCacheRepository>();
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();

        output.WriteLine($"Found {registries.Count} registries");

        // Manually execute what the background worker would do
        var refreshedCount = 0;
        var failedCount = 0;

        foreach (var registry in registries)
        {
            // Skip cached wrappers (as the worker does)
            if (registry is ICachedServerRegistry)
            {
                output.WriteLine($"Skipping cached wrapper: {registry.Name}");
                continue;
            }

            try
            {
                output.WriteLine($"Refreshing {registry.Name}...");
                var servers = await registry.GetAllServersAsync();
                var serverList = servers.ToList();

                var count = await cacheRepo.UpsertManyAsync(registry.Name, serverList);
                await cacheRepo.UpdateRegistryMetadataAsync(registry.Name, count, true);

                output.WriteLine($"  ✓ Cached {count} servers");
                refreshedCount++;
            }
            catch (Exception ex)
            {
                output.WriteLine($"  ✗ Failed: {ex.Message}");
                await cacheRepo.UpdateRegistryMetadataAsync(registry.Name, 0, false, ex.Message);
                failedCount++;
            }
        }

        output.WriteLine($"\nRefreshed: {refreshedCount}, Failed: {failedCount}");

        // Verify cache has data
        var allServers = await cacheRepo.GetAllAsync();
        var totalServers = allServers.Count();

        output.WriteLine($"Total servers in cache: {totalServers}");

        Assert.True(refreshedCount >= 2, $"Expected at least 2 successful refreshes, got {refreshedCount}");
        Assert.True(totalServers >= 7, $"Expected at least 7 total servers in cache, got {totalServers}");
    }

    [Fact]
    public async Task BrowseServersPage_WouldSeeAllRegistries()
    {
        // This test simulates exactly what BrowseServers.razor does

        // Arrange - set up dependencies
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Ensure clean database for each test
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.McpManagerDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        output.WriteLine("=== Simulating BrowseServers.razor OnInitializedAsync ===");

        // Step 1: Populate cache (normally done by background worker)
        output.WriteLine("\nStep 1: Populating cache (background worker simulation)...");
        var cacheRepo = serviceProvider.GetRequiredService<IRegistryCacheRepository>();
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();

        output.WriteLine($"Injected registries count: {registries.Count}");

        foreach (var registry in registries.Where(r => r is not ICachedServerRegistry))
        {
            try
            {
                var servers = await registry.GetAllServersAsync();
                await cacheRepo.UpsertManyAsync(registry.Name, servers.ToList());
                output.WriteLine($"  ✓ Cached servers from {registry.Name}");
            }
            catch (Exception ex)
            {
                output.WriteLine($"  ✗ Failed to cache {registry.Name}: {ex.Message}");
            }
        }

        // Step 2: Simulate LoadAllServersAsync() from BrowseServers.razor
        output.WriteLine("\nStep 2: Simulating BrowseServers.LoadAllServersAsync()...");

        var allResults = new List<Core.Models.ServerSearchResult>();
        var selectedRegistry = string.Empty; // Empty = all registries

        var registriesToQuery = string.IsNullOrEmpty(selectedRegistry)
            ? registries
            : registries.Where(r => r.Name == selectedRegistry);

        output.WriteLine($"Querying {registriesToQuery.Count()} registries...");

        foreach (var registry in registriesToQuery)
        {
            try
            {
                output.WriteLine($"  Querying {registry.Name}...");
                var results = await registry.GetAllServersAsync();
                var resultList = results.ToList();
                allResults.AddRange(resultList);
                output.WriteLine($"    Got {resultList.Count} servers");
            }
            catch (Exception ex)
            {
                output.WriteLine($"    Error: {ex.Message}");
            }
        }

        // Step 3: Deduplicate (as BrowseServers does)
        output.WriteLine("\nStep 3: Deduplicating servers...");
        var searchResults = allResults
            .GroupBy(r => r.Server.Id)
            .Select(g => g.First())
            .ToList();

        output.WriteLine($"Total servers (with duplicates): {allResults.Count}");
        output.WriteLine($"Unique servers: {searchResults.Count}");

        // Step 4: Show what would be displayed
        output.WriteLine("\nStep 4: Servers that would be displayed:");
        var byRegistry = searchResults
            .GroupBy(s => s.RegistryName)
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var group in byRegistry)
        {
            output.WriteLine($"  {group.Key}: {group.Count()} servers");
        }

        // Step 5: Test pagination
        output.WriteLine("\nStep 5: Testing pagination (12 per page)...");
        const int pageSize = 12;
        var totalPages = (int)Math.Ceiling((double)searchResults.Count / pageSize);
        output.WriteLine($"Total pages: {totalPages}");
        output.WriteLine($"Page 1 would show {Math.Min(pageSize, searchResults.Count)} servers");

        // Assertions
        Assert.NotEmpty(searchResults);
        Assert.True(byRegistry.Count >= 2,
            $"Expected servers from at least 2 registries, got {byRegistry.Count}. " +
            $"Registries: {string.Join(", ", byRegistry.Select(g => g.Key))}");

        output.WriteLine("\n✓ BrowseServers simulation complete");
        output.WriteLine($"✓ User would see {searchResults.Count} servers across {byRegistry.Count} registries");
    }

    [Fact]
    public async Task DirectRegistryQuery_VsCacheQuery_CompareResults()
    {
        // This test compares what we get from direct registry queries vs cache

        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Ensure clean database for each test
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.McpManagerDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        output.WriteLine("=== Comparing Direct Registry Queries vs Cache ===\n");

        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var cacheRepo = serviceProvider.GetRequiredService<IRegistryCacheRepository>();

        var directQueryResults = new Dictionary<string, int>();
        var cacheQueryResults = new Dictionary<string, int>();

        // Query each registry directly
        output.WriteLine("Direct Registry Queries:");
        foreach (var registry in registries.Where(r => r is not ICachedServerRegistry))
        {
            try
            {
                var servers = await registry.GetAllServersAsync();
                var count = servers.Count();
                directQueryResults[registry.Name] = count;
                output.WriteLine($"  {registry.Name}: {count} servers");

                // Also populate cache
                await cacheRepo.UpsertManyAsync(registry.Name, servers.ToList());
            }
            catch (Exception ex)
            {
                output.WriteLine($"  {registry.Name}: FAILED ({ex.Message})");
                directQueryResults[registry.Name] = 0;
            }
        }

        // Query cache
        output.WriteLine("\nCache Queries:");
        foreach (var registry in registries.Where(r => r is not ICachedServerRegistry))
        {
            var cached = await cacheRepo.GetByRegistryAsync(registry.Name);
            var count = cached.Count();
            cacheQueryResults[registry.Name] = count;
            output.WriteLine($"  {registry.Name}: {count} servers");
        }

        // Compare
        output.WriteLine("\nComparison:");
        foreach (var registryName in directQueryResults.Keys)
        {
            var directCount = directQueryResults[registryName];
            var cacheCount = cacheQueryResults.GetValueOrDefault(registryName, 0);
            var match = directCount == cacheCount ? "✓" : "✗";

            output.WriteLine($"  {match} {registryName}: Direct={directCount}, Cache={cacheCount}");

            if (directCount > 0)
            {
                Assert.Equal(directCount, cacheCount);
            }
        }

        var totalDirect = directQueryResults.Values.Sum();
        var totalCache = cacheQueryResults.Values.Sum();

        output.WriteLine($"\nTotal: Direct={totalDirect}, Cache={totalCache}");
        Assert.True(totalCache >= 7, $"Cache should have at least 7 servers, has {totalCache}");
    }

    [Fact]
    public async Task CachedServerRegistry_ReturnsDataFromCache()
    {
        // Test that CachedServerRegistry wrapper actually uses the cache

        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Ensure clean database for each test
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.McpManagerDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
        }

        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var cacheRepo = serviceProvider.GetRequiredService<IRegistryCacheRepository>();

        output.WriteLine("=== Testing CachedServerRegistry Wrapper ===\n");

        // Find a non-cached registry to test
        var baseRegistry = registries.FirstOrDefault(r =>
            r.Name == "MCP GitHub Reference Servers" &&
            r is not ICachedServerRegistry);

        Assert.NotNull(baseRegistry);

        // Populate cache
        output.WriteLine($"Populating cache from {baseRegistry.Name}...");
        var servers = await baseRegistry.GetAllServersAsync();
        var serverList = servers.ToList();
        await cacheRepo.UpsertManyAsync(baseRegistry.Name, serverList);
        output.WriteLine($"Cached {serverList.Count} servers");

        // Create a cached wrapper
        var cachedRegistry = new Infrastructure.Registries.CachedServerRegistry(
            baseRegistry,
            cacheRepo,
            TimeSpan.FromHours(1));

        output.WriteLine($"\nQuerying through CachedServerRegistry...");
        var cachedResults = await cachedRegistry.GetAllServersAsync();
        var cachedList = cachedResults.ToList();

        output.WriteLine($"Got {cachedList.Count} servers from cache");

        Assert.Equal(serverList.Count, cachedList.Count);
        output.WriteLine("✓ CachedServerRegistry returned correct count");
    }
}
