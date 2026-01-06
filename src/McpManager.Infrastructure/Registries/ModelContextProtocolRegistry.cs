using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Registry that connects to the official MCP registry at registry.modelcontextprotocol.io.
/// </summary>
public class ModelContextProtocolRegistry : IServerRegistry
{
    private readonly HttpClient _httpClient;

    public string Name => "Model Context Protocol Registry";

    public ModelContextProtocolRegistry(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        try
        {
            var allServers = await GetAllServersAsync();
            var lowerQuery = query.ToLowerInvariant();
            
            return allServers
                .Where(s => s.Server.Name.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                           s.Server.Description.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase) ||
                           s.Server.Tags.Any(t => t.Contains(lowerQuery, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(s => CalculateScore(s.Server, lowerQuery))
                .Take(maxResults);
        }
        catch
        {
            return Enumerable.Empty<ServerSearchResult>();
        }
    }

    public async Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
    {
        try
        {
            var allResults = new List<ServerSearchResult>();
            string? cursor = null;
            var maxPages = 20; // Limit to prevent infinite loops
            var page = 0;

            do
            {
                var url = string.IsNullOrEmpty(cursor) 
                    ? "v0.1/servers" 
                    : $"v0.1/servers?cursor={Uri.EscapeDataString(cursor)}";
                
                var response = await _httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<McpRegistryResponse>(json);

                if (apiResponse?.Servers == null || !apiResponse.Servers.Any())
                {
                    break;
                }

                allResults.AddRange(apiResponse.Servers.Select(ConvertToSearchResult));
                
                cursor = apiResponse.Metadata?.NextCursor;
                page++;
                
            } while (!string.IsNullOrEmpty(cursor) && page < maxPages);

            return allResults;
        }
        catch
        {
            return Enumerable.Empty<ServerSearchResult>();
        }
    }

    public async Task<McpServer?> GetServerDetailsAsync(string serverId)
    {
        try
        {
            var url = $"v0.1/servers/{Uri.EscapeDataString(serverId)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var serverEntry = JsonSerializer.Deserialize<ServerEntry>(json);

            return serverEntry?.Server == null ? null : ConvertToServer(serverEntry.Server);
        }
        catch
        {
            return null;
        }
    }

    private ServerSearchResult ConvertToSearchResult(ServerEntry entry)
    {
        return new ServerSearchResult
        {
            Server = ConvertToServer(entry.Server),
            Score = 1.0,
            RegistryName = Name,
            DownloadCount = 0, // Registry doesn't provide download counts
            LastUpdated = entry.Meta?.Official?.UpdatedAt ?? entry.Meta?.Official?.PublishedAt
        };
    }

    private static McpServer ConvertToServer(ServerDto dto)
    {
        // Extract repository URL
        var repoUrl = string.Empty;
        if (dto.Repository != null)
        {
            repoUrl = dto.Repository.Url ?? string.Empty;
        }

        // Determine install command from packages
        var installCommand = "npm install -g " + dto.Name;
        if (dto.Packages != null && dto.Packages.Any())
        {
            var npmPackage = dto.Packages.FirstOrDefault(p => p.RegistryType == "npm");
            if (npmPackage != null && !string.IsNullOrEmpty(npmPackage.Identifier))
            {
                installCommand = $"npm install -g {npmPackage.Identifier}";
            }
            else if (dto.Packages[0] != null && !string.IsNullOrEmpty(dto.Packages[0].Identifier))
            {
                // Use first package if no npm package found
                var pkg = dto.Packages[0];
                if (pkg.RegistryType == "oci")
                {
                    installCommand = $"docker pull {pkg.Identifier}";
                }
                else
                {
                    installCommand = $"# Install: {pkg.Identifier}";
                }
            }
        }

        return new McpServer
        {
            Id = dto.Name ?? Guid.NewGuid().ToString(),
            Name = dto.Title ?? dto.Name ?? "Unknown Server",
            Description = dto.Description ?? string.Empty,
            Version = dto.Version ?? "1.0.0",
            Author = ExtractAuthor(dto.Name),
            RepositoryUrl = repoUrl,
            InstallCommand = installCommand,
            Tags = new List<string>() // Registry doesn't provide tags in the schema
        };
    }

    private static string ExtractAuthor(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        
        // Names are in format "author.domain/package" or "domain/package"
        var parts = name.Split('/');
        if (parts.Length > 0 && parts[0].Contains('.'))
        {
            return parts[0];
        }
        
        return parts.Length > 0 ? parts[0] : "Unknown";
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

        return score > 0 ? score / 18.0 : 0;
    }

    private class McpRegistryResponse
    {
        [JsonPropertyName("servers")]
        public List<ServerEntry>? Servers { get; set; }

        [JsonPropertyName("metadata")]
        public MetadataDto? Metadata { get; set; }
    }

    private class ServerEntry
    {
        [JsonPropertyName("server")]
        public ServerDto Server { get; set; } = new();

        [JsonPropertyName("_meta")]
        public MetaDto? Meta { get; set; }
    }

    private class MetadataDto
    {
        [JsonPropertyName("nextCursor")]
        public string? NextCursor { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    private class MetaDto
    {
        [JsonPropertyName("io.modelcontextprotocol.registry/official")]
        public OfficialMeta? Official { get; set; }
    }

    private class OfficialMeta
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("isLatest")]
        public bool IsLatest { get; set; }
    }

    private class ServerDto
    {
        [JsonPropertyName("$schema")]
        public string? Schema { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("repository")]
        public RepositoryDto? Repository { get; set; }

        [JsonPropertyName("websiteUrl")]
        public string? WebsiteUrl { get; set; }

        [JsonPropertyName("packages")]
        public List<PackageDto>? Packages { get; set; }
    }

    private class RepositoryDto
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    private class PackageDto
    {
        [JsonPropertyName("registryType")]
        public string? RegistryType { get; set; }

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        [JsonPropertyName("transport")]
        public TransportDto? Transport { get; set; }
    }

    private class TransportDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
