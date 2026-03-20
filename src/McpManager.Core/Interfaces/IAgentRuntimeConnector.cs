using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

/// <summary>
/// Optional interface for connectors that can expose a live, read-only runtime catalog.
/// </summary>
public interface IAgentRuntimeConnector
{
    Task<AgentRuntimeCatalog?> GetRuntimeCatalogAsync();
}
