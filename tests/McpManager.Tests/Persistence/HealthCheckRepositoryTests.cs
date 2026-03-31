using Microsoft.EntityFrameworkCore;
using McpManager.Core.Models;
using McpManager.Infrastructure.Persistence;
using McpManager.Infrastructure.Persistence.Repositories;

namespace McpManager.Tests.Persistence;

public class HealthCheckRepositoryTests : IDisposable
{
    private readonly McpManagerDbContext _context;
    private readonly HealthCheckRepository _repository;

    public HealthCheckRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<McpManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new McpManagerDbContext(options);
        _repository = new HealthCheckRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task AddAsync_PersistsHealthCheck()
    {
        var result = new HealthCheckResult
        {
            ServerId = "server1", IsHealthy = true,
            ResponseTimeMs = 150, CheckedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(result);

        var latest = await _repository.GetLatestAsync("server1");
        Assert.NotNull(latest);
        Assert.Equal("server1", latest.ServerId);
        Assert.True(latest.IsHealthy);
        Assert.Equal(150, latest.ResponseTimeMs);
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsNewestFirst()
    {
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            await _repository.AddAsync(new HealthCheckResult
            {
                ServerId = "server1", IsHealthy = true,
                ResponseTimeMs = i * 100, CheckedAt = now.AddMinutes(-i)
            });
        }

        var recent = (await _repository.GetRecentAsync("server1", 3)).ToList();
        Assert.Equal(3, recent.Count);
        Assert.True(recent[0].CheckedAt >= recent[1].CheckedAt);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNull_WhenNoChecks()
    {
        var latest = await _repository.GetLatestAsync("nonexistent");
        Assert.Null(latest);
    }

    [Fact]
    public async Task GetErrorCountAsync_ReturnsCorrectCounts()
    {
        for (var i = 0; i < 10; i++)
        {
            await _repository.AddAsync(new HealthCheckResult
            {
                ServerId = "server1", IsHealthy = i % 3 != 0,
                ResponseTimeMs = 100, CheckedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        var (total, failed) = await _repository.GetErrorCountAsync("server1", 10);
        Assert.Equal(10, total);
        Assert.Equal(4, failed);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOldRecords()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        await _repository.AddAsync(new HealthCheckResult
        {
            ServerId = "server1", IsHealthy = true, ResponseTimeMs = 100,
            CheckedAt = cutoff.AddDays(-1)
        });
        await _repository.AddAsync(new HealthCheckResult
        {
            ServerId = "server1", IsHealthy = true, ResponseTimeMs = 100,
            CheckedAt = DateTime.UtcNow
        });

        var deleted = await _repository.DeleteOlderThanAsync(cutoff);

        Assert.Equal(1, deleted);
        var remaining = (await _repository.GetRecentAsync("server1", 100)).ToList();
        Assert.Single(remaining);
    }
}
