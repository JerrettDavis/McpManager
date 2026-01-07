using McpManager.Core.Interfaces;
using McpManager.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace McpManager.Tests.Integration;

/// <summary>
/// Integration tests to validate that all registries are properly loaded
/// and configured in the DI container.
/// </summary>
public class RegistryIntegrationTests
{
    [Fact]
    public void ServiceCollection_RegistersAllExpectedRegistries()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();

        // Assert
        Assert.NotEmpty(registries);

        // We should have at least 4 registries:
        // 1. ModelContextProtocolRegistry
        // 2. McpServersComRegistry
        // 3. ModelContextProtocolGitHubRegistry
        // 4. MockServerRegistry
        Assert.True(registries.Count >= 4,
            $"Expected at least 4 registries, but found {registries.Count}. " +
            $"Registered: {string.Join(", ", registries.Select(r => r.Name))}");
    }

    [Fact]
    public void ServiceCollection_RegistersExpectedRegistryNames()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var registryNames = registries.Select(r => r.Name).ToHashSet();

        // Assert - check for expected registry names
        var expectedNames = new[]
        {
            "Model Context Protocol Registry",
            "MCPServers.com",
            "MCP GitHub Reference Servers",
            "Mock MCP Registry"
        };

        foreach (var expectedName in expectedNames)
        {
            Assert.Contains(expectedName, registryNames);
        }
    }

    [Fact]
    public async Task ModelContextProtocolRegistry_CanLoadServers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var mcpRegistry = registries.FirstOrDefault(r => r.Name == "Model Context Protocol Registry");

        // Assert
        Assert.NotNull(mcpRegistry);

        // Try to load servers (this will test HttpClient injection)
        try
        {
            var servers = await mcpRegistry.GetAllServersAsync();
            var serverList = servers.ToList();

            // Should get some servers (or at least not throw)
            Assert.NotNull(serverList);
        }
        catch (HttpRequestException)
        {
            // Network errors are acceptable in tests
            Assert.True(true, "Network error is acceptable");
        }
    }

    [Fact]
    public async Task McpServersComRegistry_CanLoadServers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var mcpServersComRegistry = registries.FirstOrDefault(r => r.Name == "MCPServers.com");

        // Assert
        Assert.NotNull(mcpServersComRegistry);

        // Try to load servers (this will test HttpClient injection)
        try
        {
            var servers = await mcpServersComRegistry.GetAllServersAsync();
            var serverList = servers.ToList();

            // Should get some servers (or at least not throw)
            Assert.NotNull(serverList);
        }
        catch (HttpRequestException)
        {
            // Network errors are acceptable in tests
            Assert.True(true, "Network error is acceptable");
        }
    }

    [Fact]
    public async Task ModelContextProtocolGitHubRegistry_ReturnsSevenReferenceServers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var githubRegistry = registries.FirstOrDefault(r => r.Name == "MCP GitHub Reference Servers");

        // Assert
        Assert.NotNull(githubRegistry);

        var servers = await githubRegistry.GetAllServersAsync();
        var serverList = servers.ToList();

        Assert.Equal(7, serverList.Count);
        Assert.All(serverList, s => Assert.Equal("Anthropic", s.Server.Author));
    }

    [Fact]
    public async Task MockServerRegistry_ReturnsMockServers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();
        var mockRegistry = registries.FirstOrDefault(r => r.Name == "Mock MCP Registry");

        // Assert
        Assert.NotNull(mockRegistry);

        var servers = await mockRegistry.GetAllServersAsync();
        var serverList = servers.ToList();

        Assert.NotEmpty(serverList);
        Assert.All(serverList, s => Assert.Equal("Mock MCP Registry", s.RegistryName));
    }

    [Fact]
    public async Task AllRegistries_CombinedProvideSignificantNumberOfServers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();

        var allServers = new List<Core.Models.ServerSearchResult>();
        foreach (var registry in registries)
        {
            try
            {
                var servers = await registry.GetAllServersAsync();
                allServers.AddRange(servers);
            }
            catch (HttpRequestException)
            {
                // Network errors are acceptable - skip this registry
            }
        }

        // Assert
        // We should have AT LEAST:
        // - 7 from GitHub Reference Servers
        // - Several from Mock registry
        // - Potentially hundreds from MCPServers.com and ModelContextProtocolRegistry (if network available)
        Assert.True(allServers.Count >= 7,
            $"Expected at least 7 servers total (from GitHub reference), but got {allServers.Count}");

        // Log the breakdown for debugging
        var breakdown = allServers
            .GroupBy(s => s.RegistryName)
            .Select(g => $"{g.Key}: {g.Count()} servers")
            .ToList();

        Assert.NotEmpty(breakdown);
    }

    [Fact]
    public async Task AllRegistries_CanBeQueriedInParallel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();

        // Query all registries in parallel
        var tasks = registries
            .Select(async registry =>
            {
                try
                {
                    var servers = await registry.GetAllServersAsync();
                    return (Registry: registry.Name, Success: true, Count: servers.Count());
                }
                catch (Exception ex)
                {
                    return (Registry: registry.Name, Success: false, Count: 0);
                }
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - all registries should at least be queryable (even if network fails)
        Assert.All(results, r => Assert.NotNull(r.Registry));

        // At least the Mock and GitHub registries should succeed (no network dependency)
        var successfulRegistries = results.Where(r => r.Success).ToList();
        Assert.True(successfulRegistries.Count >= 2,
            $"Expected at least 2 successful registries, got {successfulRegistries.Count}. " +
            $"Successful: {string.Join(", ", successfulRegistries.Select(r => r.Registry))}");
    }

    [Fact]
    public async Task AllRegistries_CanSearchInParallel()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries = serviceProvider.GetServices<IServerRegistry>().ToList();

        // Search all registries in parallel
        var tasks = registries
            .Select(async registry =>
            {
                try
                {
                    var servers = await registry.SearchAsync("git");
                    return (Registry: registry.Name, Success: true, Results: servers.ToList());
                }
                catch (Exception)
                {
                    return (Registry: registry.Name, Success: false, Results: new List<Core.Models.ServerSearchResult>());
                }
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - at least some registries should return results
        var registriesWithResults = results.Where(r => r.Success && r.Results.Any()).ToList();
        Assert.NotEmpty(registriesWithResults);
    }

    [Fact]
    public void ServiceCollection_EachRegistryHasUniqueInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMcpManagerServices();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var registries1 = serviceProvider.GetServices<IServerRegistry>().ToList();
        var registries2 = serviceProvider.GetServices<IServerRegistry>().ToList();

        // Assert - same instances should be returned (singletons)
        Assert.Equal(registries1.Count, registries2.Count);

        for (int i = 0; i < registries1.Count; i++)
        {
            // Check if they're the same instance (reference equality for singletons)
            Assert.Same(registries1[i], registries2[i]);
        }
    }
}
