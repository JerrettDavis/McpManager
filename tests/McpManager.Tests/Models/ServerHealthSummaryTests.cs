using McpManager.Core.Models;

namespace McpManager.Tests.Models;

public class ServerHealthSummaryTests
{
    [Fact]
    public void ComputeStatus_ReturnsUnknown_WhenNoChecks()
    {
        Assert.Equal(HealthStatus.Unknown, ServerHealthSummary.ComputeStatus(0, null, hasChecks: false));
    }

    [Theory]
    [InlineData(0, 100L, HealthStatus.Healthy)]
    [InlineData(0.5, 499L, HealthStatus.Healthy)]
    [InlineData(1, 400L, HealthStatus.Degraded)]
    [InlineData(0, 500L, HealthStatus.Healthy)]
    [InlineData(0, 501L, HealthStatus.Degraded)]
    [InlineData(3, 100L, HealthStatus.Degraded)]
    [InlineData(0, 2001L, HealthStatus.Failing)]
    [InlineData(6, 100L, HealthStatus.Failing)]
    [InlineData(0, null, HealthStatus.Failing)]
    public void ComputeStatus_ReturnsExpected(double errorRate, long? responseMs, HealthStatus expected)
    {
        Assert.Equal(expected, ServerHealthSummary.ComputeStatus(errorRate, responseMs, hasChecks: true));
    }
}
