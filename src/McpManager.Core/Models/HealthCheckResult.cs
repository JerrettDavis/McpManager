namespace McpManager.Core.Models;

public enum HealthStatus
{
    Healthy,
    Degraded,
    Failing,
    Unknown
}

public class HealthCheckResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ServerId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public bool IsHealthy { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ServerHealthSummary
{
    public string ServerId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; } = HealthStatus.Unknown;
    public DateTime? LastCheckedAt { get; set; }
    public long? LastResponseTimeMs { get; set; }
    public double ErrorRatePercent { get; set; }
    public int TotalChecks { get; set; }
    public int FailedChecks { get; set; }
    public string? LastErrorMessage { get; set; }
    public List<HealthCheckResult> RecentChecks { get; set; } = [];

    public static HealthStatus ComputeStatus(double errorRatePercent, long? responseTimeMs, bool hasChecks)
    {
        if (!hasChecks) return HealthStatus.Unknown;
        if (errorRatePercent > 5 || responseTimeMs is null or > 2000) return HealthStatus.Failing;
        if (errorRatePercent >= 1 || responseTimeMs > 500) return HealthStatus.Degraded;
        return HealthStatus.Healthy;
    }
}
