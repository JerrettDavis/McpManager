using McpManager.Core.Models;

namespace McpManager.Core.Interfaces;

public interface IConfigImporter
{
    Task<IEnumerable<ImportSource>> DetectSourcesAsync();
    Task<ImportResult> ImportAsync(IEnumerable<ImportableServer> servers, string targetAgent);
}
