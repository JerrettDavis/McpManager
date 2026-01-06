using Microsoft.EntityFrameworkCore;
using McpManager.Core.Models;
using McpManager.Infrastructure.Persistence;
using McpManager.Infrastructure.Persistence.Repositories;
using Xunit;

namespace McpManager.Tests.Persistence;

/// <summary>
/// Unit tests for RegistryCacheRepository
/// </summary>
public class RegistryCacheRepositoryTests : IDisposable
{
    private readonly McpManagerDbContext _context;
    private readonly RegistryCacheRepository _repository;

    public RegistryCacheRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<McpManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new McpManagerDbContext(options);
        _repository = new RegistryCacheRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetByRegistryAsync_ReturnsServersForRegistry()
    {
        // Arrange
        var results = new List<ServerSearchResult>
        {
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server1", Name = "Server 1" },
                RegistryName = "TestRegistry"
            },
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server2", Name = "Server 2" },
                RegistryName = "TestRegistry"
            }
        };

        await _repository.UpsertManyAsync("TestRegistry", results);

        // Act
        var retrieved = await _repository.GetByRegistryAsync("TestRegistry");

        // Assert
        Assert.Equal(2, retrieved.Count());
    }

    [Fact]
    public async Task UpsertManyAsync_InsertsNewServers()
    {
        // Arrange
        var results = new List<ServerSearchResult>
        {
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server1", Name = "Server 1" },
                RegistryName = "TestRegistry"
            }
        };

        // Act
        var count = await _repository.UpsertManyAsync("TestRegistry", results);

        // Assert
        Assert.Equal(1, count);
        var cached = await _repository.GetByRegistryAsync("TestRegistry");
        Assert.Single(cached);
    }

    [Fact]
    public async Task UpsertManyAsync_UpdatesExistingServers()
    {
        // Arrange
        var initialResult = new ServerSearchResult
        {
            Server = new McpServer { Id = "server1", Name = "Old Name" },
            RegistryName = "TestRegistry"
        };
        await _repository.UpsertManyAsync("TestRegistry", new[] { initialResult });

        var updatedResult = new ServerSearchResult
        {
            Server = new McpServer { Id = "server1", Name = "New Name" },
            RegistryName = "TestRegistry"
        };

        // Act
        await _repository.UpsertManyAsync("TestRegistry", new[] { updatedResult });

        // Assert
        var cached = await _repository.GetByIdAsync("TestRegistry", "server1");
        Assert.NotNull(cached);
        Assert.Equal("New Name", cached.Server.Name);
    }

    [Fact]
    public async Task SearchAsync_FindsServersByName()
    {
        // Arrange
        var results = new List<ServerSearchResult>
        {
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server1", Name = "Database Server" },
                RegistryName = "TestRegistry"
            },
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server2", Name = "API Server" },
                RegistryName = "TestRegistry"
            }
        };
        await _repository.UpsertManyAsync("TestRegistry", results);

        // Act
        var found = await _repository.SearchAsync("database");

        // Assert
        Assert.Single(found);
        Assert.Equal("Database Server", found.First().Server.Name);
    }

    [Fact]
    public async Task SearchAsync_EscapesWildcards()
    {
        // Arrange
        var results = new List<ServerSearchResult>
        {
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server1", Name = "Test%Server" },
                RegistryName = "TestRegistry"
            },
            new ServerSearchResult
            {
                Server = new McpServer { Id = "server2", Name = "TestServer" },
                RegistryName = "TestRegistry"
            }
        };
        await _repository.UpsertManyAsync("TestRegistry", results);

        // Act - Search with wildcard character
        var found = await _repository.SearchAsync("Test%");

        // Assert - Should only find the exact match with %, not all servers starting with Test
        Assert.Single(found);
        Assert.Equal("Test%Server", found.First().Server.Name);
    }

    [Fact]
    public async Task UpdateRegistryMetadataAsync_CreatesNewMetadata()
    {
        // Act
        await _repository.UpdateRegistryMetadataAsync("TestRegistry", 10, true);

        // Assert
        var lastRefresh = await _repository.GetLastRefreshTimeAsync("TestRegistry");
        Assert.NotNull(lastRefresh);
    }

    [Fact]
    public async Task UpdateRegistryMetadataAsync_UpdatesExistingMetadata()
    {
        // Arrange
        await _repository.UpdateRegistryMetadataAsync("TestRegistry", 5, true);
        var firstRefresh = await _repository.GetLastRefreshTimeAsync("TestRegistry");

        await Task.Delay(100); // Ensure time difference

        // Act
        await _repository.UpdateRegistryMetadataAsync("TestRegistry", 10, true);

        // Assert
        var secondRefresh = await _repository.GetLastRefreshTimeAsync("TestRegistry");
        Assert.NotNull(secondRefresh);
        Assert.True(secondRefresh > firstRefresh);
    }

    [Fact]
    public async Task IsCacheStaleAsync_ReturnsTrueForNonExistentCache()
    {
        // Act
        var isStale = await _repository.IsCacheStaleAsync("NonExistent", TimeSpan.FromHours(1));

        // Assert
        Assert.True(isStale);
    }

    [Fact]
    public async Task IsCacheStaleAsync_ReturnsFalseForFreshCache()
    {
        // Arrange
        await _repository.UpdateRegistryMetadataAsync("TestRegistry", 5, true);

        // Act
        var isStale = await _repository.IsCacheStaleAsync("TestRegistry", TimeSpan.FromHours(1));

        // Assert
        Assert.False(isStale);
    }

    [Fact]
    public async Task IsCacheStaleAsync_ReturnsTrueForOldCache()
    {
        // Arrange
        await _repository.UpdateRegistryMetadataAsync("TestRegistry", 5, true);
        await Task.Delay(50); // Ensure time passes

        // Act - Check with very short max age
        var isStale = await _repository.IsCacheStaleAsync("TestRegistry", TimeSpan.FromMilliseconds(1));

        // Assert
        Assert.True(isStale);
    }

    [Fact]
    public async Task GetLastRefreshTimeAsync_ReturnsNullForNonExistentRegistry()
    {
        // Act
        var lastRefresh = await _repository.GetLastRefreshTimeAsync("NonExistent");

        // Assert
        Assert.Null(lastRefresh);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsServersFromMultipleRegistries()
    {
        // Arrange
        await _repository.UpsertManyAsync("Registry1", new[]
        {
            new ServerSearchResult { Server = new McpServer { Id = "s1", Name = "Server 1" }, RegistryName = "Registry1" }
        });
        await _repository.UpsertManyAsync("Registry2", new[]
        {
            new ServerSearchResult { Server = new McpServer { Id = "s2", Name = "Server 2" }, RegistryName = "Registry2" }
        });

        // Act
        var all = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCorrectServer()
    {
        // Arrange
        var result = new ServerSearchResult
        {
            Server = new McpServer { Id = "test-server", Name = "Test Server" },
            RegistryName = "TestRegistry"
        };
        await _repository.UpsertManyAsync("TestRegistry", new[] { result });

        // Act
        var found = await _repository.GetByIdAsync("TestRegistry", "test-server");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test Server", found.Server.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistent()
    {
        // Act
        var found = await _repository.GetByIdAsync("TestRegistry", "non-existent");

        // Assert
        Assert.Null(found);
    }
}
