using McpManager.Application.Services;
using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using Moq;

namespace McpManager.Tests.Services;

public class ServerMonitorTests
{
    private readonly Mock<IHealthCheckRepository> _mockRepo;
    private readonly Mock<IServerManager> _mockServerManager;
    private readonly ServerMonitor _monitor;

    public ServerMonitorTests()
    {
        _mockRepo = new Mock<IHealthCheckRepository>();
        _mockServerManager = new Mock<IServerManager>();
        _monitor = new ServerMonitor(_mockRepo.Object, _mockServerManager.Object);
    }

    [Fact]
    public async Task StartMonitoringAsync_AddsServerToMonitoredSet()
    {
        await _monitor.StartMonitoringAsync("server1");
        Assert.Contains("server1", _monitor.GetMonitoredServerIds());
    }

    [Fact]
    public async Task StopMonitoringAsync_RemovesServerFromMonitoredSet()
    {
        await _monitor.StartMonitoringAsync("server1");
        await _monitor.StopMonitoringAsync("server1");
        Assert.DoesNotContain("server1", _monitor.GetMonitoredServerIds());
    }

    [Fact]
    public async Task GetHealthSummaryAsync_ReturnsUnknown_WhenNoChecks()
    {
        _mockRepo.Setup(r => r.GetLatestAsync("server1")).ReturnsAsync((HealthCheckResult?)null);
        _mockRepo.Setup(r => r.GetErrorCountAsync("server1", 20)).ReturnsAsync((0, 0));
        _mockRepo.Setup(r => r.GetRecentAsync("server1", 20)).ReturnsAsync([]);

        var summary = await _monitor.GetHealthSummaryAsync("server1");

        Assert.NotNull(summary);
        Assert.Equal(HealthStatus.Unknown, summary.Status);
    }

    [Fact]
    public async Task GetHealthSummaryAsync_ComputesHealthyStatus()
    {
        var latest = new HealthCheckResult
        {
            ServerId = "server1", IsHealthy = true,
            ResponseTimeMs = 100, CheckedAt = DateTime.UtcNow
        };
        _mockRepo.Setup(r => r.GetLatestAsync("server1")).ReturnsAsync(latest);
        _mockRepo.Setup(r => r.GetErrorCountAsync("server1", 20)).ReturnsAsync((20, 0));
        _mockRepo.Setup(r => r.GetRecentAsync("server1", 20)).ReturnsAsync([latest]);

        var summary = await _monitor.GetHealthSummaryAsync("server1");

        Assert.Equal(HealthStatus.Healthy, summary!.Status);
        Assert.Equal(100, summary.LastResponseTimeMs);
    }

    [Fact]
    public async Task PerformHealthCheckAsync_PersistsResult()
    {
        var result = await _monitor.PerformHealthCheckAsync("server1");

        _mockRepo.Verify(r => r.AddAsync(It.Is<HealthCheckResult>(
            h => h.ServerId == "server1")), Times.Once);
    }

    [Fact]
    public async Task IsServerRunningAsync_ReturnsTrueWhenMonitored()
    {
        await _monitor.StartMonitoringAsync("server1");
        Assert.True(await _monitor.IsServerRunningAsync("server1"));
    }

    [Fact]
    public async Task IsServerRunningAsync_ReturnsFalseWhenNotMonitored()
    {
        Assert.False(await _monitor.IsServerRunningAsync("server1"));
    }

    [Fact]
    public async Task GetAllHealthSummariesAsync_ReturnsSummariesForAllInstalledServers()
    {
        _mockServerManager.Setup(m => m.GetInstalledServersAsync())
            .ReturnsAsync(new List<McpServer>
            {
                new() { Id = "s1", Name = "Server 1" },
                new() { Id = "s2", Name = "Server 2" }
            });
        _mockRepo.Setup(r => r.GetLatestAsync(It.IsAny<string>())).ReturnsAsync((HealthCheckResult?)null);
        _mockRepo.Setup(r => r.GetErrorCountAsync(It.IsAny<string>(), 20)).ReturnsAsync((0, 0));
        _mockRepo.Setup(r => r.GetRecentAsync(It.IsAny<string>(), 20)).ReturnsAsync([]);

        var summaries = (await _monitor.GetAllHealthSummariesAsync()).ToList();

        Assert.Equal(2, summaries.Count);
        Assert.Contains(summaries, s => s.ServerId == "s1");
        Assert.Contains(summaries, s => s.ServerId == "s2");
    }
}
