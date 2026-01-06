namespace McpManager.Core.Interfaces;

/// <summary>
/// Marker interface to identify server registries that provide caching.
/// Used by background workers to avoid double-wrapping cached registries.
/// </summary>
public interface ICachedServerRegistry : IServerRegistry
{
}
