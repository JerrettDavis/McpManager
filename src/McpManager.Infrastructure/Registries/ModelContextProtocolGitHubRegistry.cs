using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Registry that parses the official modelcontextprotocol/servers GitHub repository
/// to discover reference MCP servers maintained by Anthropic.
/// </summary>
public class ModelContextProtocolGitHubRegistry(HttpClient httpClient) : IServerRegistry
{
    private const string ReadmeUrl = "https://raw.githubusercontent.com/modelcontextprotocol/servers/refs/heads/main/README.md";
    private const string PackageLockUrl = "https://raw.githubusercontent.com/modelcontextprotocol/servers/refs/heads/main/package-lock.json";
    private const string BaseRepoUrl = "https://github.com/modelcontextprotocol/servers";

    private static readonly Dictionary<string, ServerInfo> _referenceServers = new()
    {
        ["everything"] = new ServerInfo
        {
            Name = "Everything",
            Description = "Reference server demonstrating MCP features with prompts, resources, and tools",
            Path = "src/everything",
            Category = "Reference Implementation"
        },
        ["fetch"] = new ServerInfo
        {
            Name = "Fetch",
            Description = "Web content fetching and conversion for efficient LLM usage",
            Path = "src/fetch",
            Category = "Web & Data"
        },
        ["filesystem"] = new ServerInfo
        {
            Name = "Filesystem",
            Description = "Secure file operations with configurable access controls",
            Path = "src/filesystem",
            Category = "File System"
        },
        ["git"] = new ServerInfo
        {
            Name = "Git",
            Description = "Tools to read, search, and manipulate Git repositories",
            Path = "src/git",
            Category = "Development Tools"
        },
        ["memory"] = new ServerInfo
        {
            Name = "Memory",
            Description = "Knowledge graph-based persistent memory system",
            Path = "src/memory",
            Category = "AI & ML"
        },
        ["sequential-thinking"] = new ServerInfo
        {
            Name = "Sequential Thinking",
            Description = "Dynamic and reflective problem-solving through thought sequences",
            Path = "src/sequential-thinking",
            Category = "AI & ML"
        },
        ["time"] = new ServerInfo
        {
            Name = "Time",
            Description = "Time and timezone conversion capabilities",
            Path = "src/time",
            Category = "Utilities"
        }
    };

    public string Name => "MCP GitHub Reference Servers";

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        var allServers = await GetAllServersAsync();
        var lowerQuery = query.ToLowerInvariant();

        return allServers
            .Where(s => s.Server.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                       s.Server.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                       s.Server.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(s => s.Score)
            .Take(maxResults);
    }

    public async Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
    {
        try
        {
            // Parse package-lock.json to get version information
            var versions = await ParsePackageLockAsync();

            var results = new List<ServerSearchResult>();

            foreach (var (serverKey, serverInfo) in _referenceServers)
            {
                var packageName = $"@modelcontextprotocol/server-{serverKey}";
                var version = versions.GetValueOrDefault(packageName, "latest");

                var server = new McpServer
                {
                    Id = packageName,
                    Name = serverInfo.Name,
                    Description = serverInfo.Description,
                    Version = version,
                    Author = "Anthropic",
                    RepositoryUrl = $"{BaseRepoUrl}/tree/main/{serverInfo.Path}",
                    InstallCommand = $"npx -y {packageName}",
                    Tags = ["Official", "Reference", serverInfo.Category]
                };

                results.Add(new ServerSearchResult
                {
                    Server = server,
                    Score = 1.0, // All servers have equal score
                    RegistryName = Name,
                    DownloadCount = 0, // Not available from GitHub
                    LastUpdated = DateTime.UtcNow
                });
            }

            return results;
        }
        catch
        {
            return [];
        }
    }

    public async Task<McpServer?> GetServerDetailsAsync(string serverId)
    {
        try
        {
            // Extract server key from package name (e.g., @modelcontextprotocol/server-git -> git)
            var serverKey = serverId.Replace("@modelcontextprotocol/server-", "");

            if (!_referenceServers.TryGetValue(serverKey, out var serverInfo))
            {
                return null;
            }

            var versions = await ParsePackageLockAsync();
            var version = versions.GetValueOrDefault(serverId, "latest");

            // Try to fetch additional details from package.json in the server directory
            var packageJsonUrl = $"https://raw.githubusercontent.com/modelcontextprotocol/servers/refs/heads/main/{serverInfo.Path}/package.json";
            var enhancedDescription = await FetchPackageDetailsAsync(packageJsonUrl, serverInfo.Description);

            return new McpServer
            {
                Id = serverId,
                Name = serverInfo.Name,
                Description = enhancedDescription,
                Version = version,
                Author = "Anthropic",
                RepositoryUrl = $"{BaseRepoUrl}/tree/main/{serverInfo.Path}",
                InstallCommand = $"npx -y {serverId}",
                Tags = ["Official", "Reference", serverInfo.Category]
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<Dictionary<string, string>> ParsePackageLockAsync()
    {
        try
        {
            var response = await httpClient.GetAsync(PackageLockUrl);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var packageLock = JsonSerializer.Deserialize<PackageLock>(json);

            var versions = new Dictionary<string, string>();

            if (packageLock?.Packages != null)
            {
                foreach (var (packagePath, packageInfo) in packageLock.Packages)
                {
                    // Extract package names from workspace packages (e.g., "src/git" -> "@modelcontextprotocol/server-git")
                    if (!string.IsNullOrEmpty(packageInfo.Name) &&
                        packageInfo.Name.StartsWith("@modelcontextprotocol/server-") &&
                        !string.IsNullOrEmpty(packageInfo.Version))
                    {
                        versions[packageInfo.Name] = packageInfo.Version;
                    }
                }
            }

            return versions;
        }
        catch
        {
            return [];
        }
    }

    private async Task<string> FetchPackageDetailsAsync(string packageJsonUrl, string fallbackDescription)
    {
        try
        {
            var response = await httpClient.GetAsync(packageJsonUrl);

            if (!response.IsSuccessStatusCode)
            {
                return fallbackDescription;
            }

            var json = await response.Content.ReadAsStringAsync();
            var packageJson = JsonSerializer.Deserialize<PackageJson>(json);

            return packageJson?.Description ?? fallbackDescription;
        }
        catch
        {
            return fallbackDescription;
        }
    }

    private class ServerInfo
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
        public required string Path { get; init; }
        public required string Category { get; init; }
    }

    private class PackageLock
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("packages")]
        public Dictionary<string, PackageInfo>? Packages { get; set; }
    }

    private class PackageInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("resolved")]
        public string? Resolved { get; set; }

        [JsonPropertyName("link")]
        public bool? Link { get; set; }
    }

    private class PackageJson
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
