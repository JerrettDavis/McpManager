namespace McpManager.Infrastructure.Persistence.Entities;

/// <summary>
/// Entity representing metadata about registry refresh operations.
/// </summary>
public class RegistryMetadataEntity
{
    /// <summary>
    /// Name of the registry (primary key).
    /// </summary>
    public string RegistryName { get; set; } = string.Empty;

    /// <summary>
    /// Last successful refresh timestamp.
    /// </summary>
    public DateTime LastRefreshAt { get; set; }

    /// <summary>
    /// Next scheduled refresh timestamp.
    /// </summary>
    public DateTime? NextRefreshAt { get; set; }

    /// <summary>
    /// Total number of servers cached from this registry.
    /// </summary>
    public int TotalServersCached { get; set; }

    /// <summary>
    /// Whether the last refresh was successful.
    /// </summary>
    public bool LastRefreshSuccessful { get; set; } = true;

    /// <summary>
    /// Error message from last refresh if failed.
    /// </summary>
    public string? LastRefreshError { get; set; }

    /// <summary>
    /// Refresh interval in minutes.
    /// </summary>
    public int RefreshIntervalMinutes { get; set; } = 60;
}
