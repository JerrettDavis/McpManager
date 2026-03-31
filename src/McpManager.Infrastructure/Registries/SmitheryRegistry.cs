using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Registry that connects to the Smithery.ai API for MCP server discovery.
/// Smithery is the largest open marketplace of Model Context Protocol servers.
/// </summary>
public class SmitheryRegistry(HttpClient httpClient) : IServerRegistry
{
    public string Name => "Smithery.ai";

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        try
        {
            var url = $"servers?q={Uri.EscapeDataString(query)}&page=1&limit={maxResults}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<SmitheryApiResponse>(json);

            if (apiResponse?.Data == null)
            {
                return [];
            }

            return apiResponse.Data.Select(ConvertToSearchResult);
        }
        catch
        {
            return [];
        }
    }

    public async Task<IEnumerable<ServerSearchResult>> GetAllServersAsync()
    {
        try
        {
            var allServers = new List<SmitheryServer>();
            var page = 1;
            const int limit = 50;
            var hasMore = true;

            while (hasMore && page <= 20) // Limit to 20 pages (1000 servers max)
            {
                var url = $"servers?page={page}&limit={limit}";
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString
                };
                var apiResponse = JsonSerializer.Deserialize<SmitheryApiResponse>(json, options);

                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    hasMore = false;
                }
                else
                {
                    allServers.AddRange(apiResponse.Data);

                    // Check if we have more pages
                    hasMore = apiResponse.Pagination?.HasMore ?? false;
                    page++;
                }
            }

            return allServers.Select(ConvertToSearchResult);
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
            // Smithery uses namespace/name format for server IDs
            var url = $"servers/{Uri.EscapeDataString(serverId)}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var server = JsonSerializer.Deserialize<SmitheryServer>(json);

            return server == null ? null : ConvertToServer(server);
        }
        catch
        {
            return null;
        }
    }

    private ServerSearchResult ConvertToSearchResult(SmitheryServer server)
    {
        return new ServerSearchResult
        {
            Server = ConvertToServer(server),
            Score = CalculateScore(server),
            RegistryName = Name,
            DownloadCount = server.DownloadCount ?? 0,
            LastUpdated = server.UpdatedAt ?? server.CreatedAt
        };
    }

    private static McpServer ConvertToServer(SmitheryServer server)
    {
        // Determine install command based on deployment type
        var installCommand = server.DeploymentType?.ToLowerInvariant() switch
        {
            "npm" => $"npx -y {server.FullName}",
            "python" or "pypi" => $"pip install {server.FullName}",
            "docker" => $"docker pull {server.FullName}",
            "stdio" => $"# See {server.HomepageUrl ?? server.RepositoryUrl} for installation",
            _ => $"# See {server.HomepageUrl ?? server.RepositoryUrl} for installation"
        };

        // Construct full ID from namespace and name
        var fullServerId = $"{server.Namespace}/{server.Name}";

        return new McpServer
        {
            Id = fullServerId,
            Name = server.DisplayName ?? server.Name ?? "Unknown Server",
            Description = server.Description ?? string.Empty,
            Version = server.LatestReleaseVersion ?? "latest",
            Author = server.Namespace ?? "Unknown",
            RepositoryUrl = server.RepositoryUrl ?? string.Empty,
            HomepageUrl = server.HomepageUrl,
            InstallCommand = installCommand,
            Tags = server.Keywords ?? [],
            IsVerified = server.IsVerified ?? false,
            IsRecommended = server.IsRecommended ?? false
        };
    }

    private static double CalculateScore(SmitheryServer server)
    {
        double score = 1.0; // Base score

        // Boost verified servers
        if (server.IsVerified == true)
            score += 2.0;

        // Boost recommended servers
        if (server.IsRecommended == true)
            score += 1.5;

        // Boost by download count
        if (server.DownloadCount.HasValue && server.DownloadCount.Value > 0)
            score += Math.Min(server.DownloadCount.Value / 1000.0, 3.0);

        return Math.Min(score / 7.5, 1.0); // Normalize to 0-1 range
    }

    // API Response Models
    private class SmitheryApiResponse
    {
        [JsonPropertyName("data")]
        public List<SmitheryServer>? Data { get; set; }

        [JsonPropertyName("pagination")]
        public PaginationInfo? Pagination { get; set; }
    }

    private class PaginationInfo
    {
        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("hasMore")]
        public bool HasMore { get; set; }
    }

    private class SmitheryServer
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("namespace")]
        public string? Namespace { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("repositoryUrl")]
        public string? RepositoryUrl { get; set; }

        [JsonPropertyName("homepageUrl")]
        public string? HomepageUrl { get; set; }

        [JsonPropertyName("deploymentType")]
        public string? DeploymentType { get; set; }

        [JsonPropertyName("latestReleaseVersion")]
        public string? LatestReleaseVersion { get; set; }

        [JsonPropertyName("isVerified")]
        public bool? IsVerified { get; set; }

        [JsonPropertyName("isRecommended")]
        public bool? IsRecommended { get; set; }

        [JsonPropertyName("downloadCount")]
        public long? DownloadCount { get; set; }

        [JsonPropertyName("keywords")]
        public List<string>? Keywords { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("fullName")]
        public string? FullName { get; set; }
    }
}
