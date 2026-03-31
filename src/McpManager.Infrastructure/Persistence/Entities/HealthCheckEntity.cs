namespace McpManager.Infrastructure.Persistence.Entities;

public class HealthCheckEntity
{
    public string Id { get; set; } = string.Empty;
    public string ServerId { get; set; } = string.Empty;
    public DateTime CheckedAt { get; set; }
    public bool IsHealthy { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}
