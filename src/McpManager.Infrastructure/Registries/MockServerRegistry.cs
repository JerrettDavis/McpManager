using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Mock registry for demonstration purposes.
/// In a real implementation, this would connect to npm, GitHub, or other MCP server registries.
/// </summary>
public class MockServerRegistry : IServerRegistry
{
    public string Name => "Mock MCP Registry";

    private readonly List<McpServer> _mockServers =
    [
        new McpServer
        {
            Id = "filesystem-server",
            Name = "Filesystem Server",
            Description = "Provides filesystem access to AI agents",
            Version = "1.0.0",
            Author = "MCP Team",
            RepositoryUrl = "https://github.com/example/filesystem-server",
            InstallCommand = "npm install -g @modelcontextprotocol/server-filesystem",
            Tags = ["filesystem", "files", "storage"]
        },

        new McpServer
        {
            Id = "database-server",
            Name = "Database Server",
            Description = "Provides database query capabilities",
            Version = "2.1.0",
            Author = "MCP Team",
            RepositoryUrl = "https://github.com/example/database-server",
            InstallCommand = "npm install -g @modelcontextprotocol/server-database",
            Tags = ["database", "sql", "query"]
        },

        new McpServer
        {
            Id = "api-server",
            Name = "API Server",
            Description = "Generic REST API client for AI agents",
            Version = "1.5.2",
            Author = "Community",
            RepositoryUrl = "https://github.com/example/api-server",
            InstallCommand = "npm install -g mcp-api-server",
            Tags = ["api", "rest", "http"]
        },

        new McpServer
        {
            Id = "github-server",
            Name = "GitHub Server",
            Description = "Interact with GitHub repositories and issues",
            Version = "3.0.1",
            Author = "GitHub",
            RepositoryUrl = "https://github.com/github/mcp-github-server",
            InstallCommand = "npm install -g @github/mcp-server",
            Tags = ["github", "git", "version-control"]
        },

        new McpServer
        {
            Id = "slack-server",
            Name = "Slack Server",
            Description = "Send and receive Slack messages",
            Version = "1.2.0",
            Author = "Community",
            RepositoryUrl = "https://github.com/example/slack-server",
            InstallCommand = "npm install -g mcp-slack-server",
            Tags = ["slack", "messaging", "communication"]
        }
    ];

    public Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        var lowerQuery = query.ToLowerInvariant();
        var results = _mockServers
            .Where(s => s.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                       s.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                       s.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)))
            .Select(s => new ServerSearchResult
            {
                Server = s,
                Score = CalculateScore(s, lowerQuery),
                RegistryName = Name,
                DownloadCount = Random.Shared.Next(100, 10000),
                LastUpdated = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365))
            })
            .OrderByDescending(r => r.Score)
            .Take(maxResults);

        return Task.FromResult<IEnumerable<ServerSearchResult>>(results);
    }

    public Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
    {
        var results = _mockServers.Select(s => new ServerSearchResult
        {
            Server = s,
            Score = 1.0,
            RegistryName = Name,
            DownloadCount = Random.Shared.Next(100, 10000),
            LastUpdated = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365))
        });

        return Task.FromResult<IEnumerable<ServerSearchResult>>(results);
    }

    public Task<McpServer?> GetServerDetailsAsync(string serverId)
    {
        var server = _mockServers.FirstOrDefault(s => s.Id == serverId);
        return Task.FromResult(server);
    }

    private static double CalculateScore(McpServer server, string query)
    {
        double score = 0;

        if (server.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            score += 10;

        if (server.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            score += 5;

        if (server.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            score += 3;

        return score > 0 ? score / 18.0 : 0; // Normalize to 0-1
    }
}
