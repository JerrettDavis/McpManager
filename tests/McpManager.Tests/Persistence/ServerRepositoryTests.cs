using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using McpManager.Core.Models;
using McpManager.Infrastructure.Persistence;
using McpManager.Infrastructure.Persistence.Repositories;
using Moq;

namespace McpManager.Tests.Persistence;

/// <summary>
/// Unit tests for ServerRepository
/// </summary>
public class ServerRepositoryTests : IDisposable
{
    private readonly McpManagerDbContext _context;
    private readonly ServerRepository _repository;
    private readonly Mock<ILogger<ServerRepository>> _mockLogger;

    public ServerRepositoryTests()
    {
        // Create in-memory database for testing
        var options = new DbContextOptionsBuilder<McpManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new McpManagerDbContext(options);
        _mockLogger = new Mock<ILogger<ServerRepository>>();
        _repository = new ServerRepository(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_InitiallyReturnsEmpty()
    {
        // Act
        var servers = await _repository.GetAllAsync();

        // Assert
        Assert.Empty(servers);
    }

    [Fact]
    public async Task AddAsync_AddsServerSuccessfully()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Description = "A test server",
            Version = "1.0.0",
            Author = "Test Author",
            RepositoryUrl = "https://github.com/test/server",
            InstallCommand = "npm install test-server",
            Tags = ["test", "demo"],
            Configuration = new Dictionary<string, string>
            {
                ["apiKey"] = "test-key",
                ["endpoint"] = "https://api.example.com"
            }
        };

        // Act
        var result = await _repository.AddAsync(server);

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByIdAsync("test-server");
        Assert.NotNull(retrieved);
        Assert.Equal("Test Server", retrieved.Name);
        Assert.Equal(2, retrieved.Tags.Count);
        Assert.Equal(2, retrieved.Configuration.Count);
        Assert.Equal("test-key", retrieved.Configuration["apiKey"]);
    }

    [Fact]
    public async Task AddAsync_ReturnsFalseForDuplicateId()
    {
        // Arrange
        var server1 = new McpServer { Id = "test-server", Name = "Server 1" };
        var server2 = new McpServer { Id = "test-server", Name = "Server 2" };

        await _repository.AddAsync(server1);

        // Act
        var result = await _repository.AddAsync(server2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForNonExistentServer()
    {
        // Act
        var server = await _repository.GetByIdAsync("non-existent");

        // Assert
        Assert.Null(server);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesServerSuccessfully()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Original Name",
            Configuration = new Dictionary<string, string> { ["key1"] = "value1" }
        };
        await _repository.AddAsync(server);

        server.Name = "Updated Name";
        server.Configuration["key2"] = "value2";

        // Act
        var result = await _repository.UpdateAsync(server);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync("test-server");
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(2, updated.Configuration.Count);
        Assert.Equal("value2", updated.Configuration["key2"]);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesConfigurationCorrectly()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Configuration = new Dictionary<string, string>
            {
                ["apiKey"] = "old-key",
                ["endpoint"] = "https://old.example.com"
            }
        };
        await _repository.AddAsync(server);

        // Update configuration
        server.Configuration["apiKey"] = "new-key";
        server.Configuration["timeout"] = "30";

        // Act
        var result = await _repository.UpdateAsync(server);

        // Assert
        Assert.True(result);
        var updated = await _repository.GetByIdAsync("test-server");
        Assert.NotNull(updated);
        Assert.Equal(3, updated.Configuration.Count);
        Assert.Equal("new-key", updated.Configuration["apiKey"]);
        Assert.Equal("30", updated.Configuration["timeout"]);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalseForNonExistentServer()
    {
        // Arrange
        var server = new McpServer { Id = "non-existent", Name = "Test" };

        // Act
        var result = await _repository.UpdateAsync(server);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_RemovesServerSuccessfully()
    {
        // Arrange
        var server = new McpServer { Id = "test-server", Name = "Test Server" };
        await _repository.AddAsync(server);

        // Act
        var result = await _repository.DeleteAsync("test-server");

        // Assert
        Assert.True(result);
        var retrieved = await _repository.GetByIdAsync("test-server");
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseForNonExistentServer()
    {
        // Act
        var result = await _repository.DeleteAsync("non-existent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrueForExistingServer()
    {
        // Arrange
        var server = new McpServer { Id = "test-server", Name = "Test Server" };
        await _repository.AddAsync(server);

        // Act
        var exists = await _repository.ExistsAsync("test-server");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseForNonExistentServer()
    {
        // Act
        var exists = await _repository.ExistsAsync("non-existent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMultipleServers()
    {
        // Arrange
        await _repository.AddAsync(new McpServer { Id = "server1", Name = "Server 1" });
        await _repository.AddAsync(new McpServer { Id = "server2", Name = "Server 2" });
        await _repository.AddAsync(new McpServer { Id = "server3", Name = "Server 3" });

        // Act
        var servers = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, servers.Count());
    }

    [Fact]
    public async Task Configuration_PersistsEmptyDictionary()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Configuration = new Dictionary<string, string>()
        };

        // Act
        await _repository.AddAsync(server);
        var retrieved = await _repository.GetByIdAsync("test-server");

        // Assert
        Assert.NotNull(retrieved);
        Assert.NotNull(retrieved.Configuration);
        Assert.Empty(retrieved.Configuration);
    }

    [Fact]
    public async Task Configuration_PersistsComplexValues()
    {
        // Arrange
        var server = new McpServer
        {
            Id = "test-server",
            Name = "Test Server",
            Configuration = new Dictionary<string, string>
            {
                ["jsonData"] = "{\"nested\":\"value\"}",
                ["multiline"] = "line1\nline2\nline3",
                ["specialChars"] = "!@#$%^&*()[]{}",
                ["unicode"] = "Hello 世界 🌍"
            }
        };

        // Act
        await _repository.AddAsync(server);
        var retrieved = await _repository.GetByIdAsync("test-server");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(4, retrieved.Configuration.Count);
        Assert.Equal("{\"nested\":\"value\"}", retrieved.Configuration["jsonData"]);
        Assert.Equal("line1\nline2\nline3", retrieved.Configuration["multiline"]);
        Assert.Equal("!@#$%^&*()[]{}",  retrieved.Configuration["specialChars"]);
        Assert.Equal("Hello 世界 🌍", retrieved.Configuration["unicode"]);
    }
}
