using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Registry that connects to the mcpservers.com API for MCP server discovery.
/// </summary>
public class McpServersComRegistry(HttpClient httpClient) : IServerRegistry
{
    public string Name => "MCPServers.com";

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        try
        {
            var url = $"mcp/registry?search={Uri.EscapeDataString(query)}&limit={maxResults}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<RegistryResponse>(json);

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
            var allPlugins = new List<McpPlugin>();
            var page = 1;
            const int limit = 50;
            var hasMore = true;

            while (hasMore && page <= 20) // Limit to 20 pages (1000 servers max)
            {
                var url = $"mcp/registry?page={page}&limit={limit}";
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"McpServersComRegistry: HTTP {response.StatusCode} for {url}");
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                };
                var apiResponse = JsonSerializer.Deserialize<RegistryResponse>(json, options);

                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    if (page == 1)
                    {
                        Console.WriteLine($"McpServersComRegistry: No data in response. JSON length: {json.Length}");
                    }
                    hasMore = false;
                }
                else
                {
                    allPlugins.AddRange(apiResponse.Data);

                    // Check if we have more pages
                    hasMore = apiResponse.TotalCount > (page * limit);
                    page++;
                }
            }

            Console.WriteLine($"McpServersComRegistry: Loaded {allPlugins.Count} servers");
            return allPlugins.Select(ConvertToSearchResult);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"McpServersComRegistry ERROR: {ex.Message}");
            return [];
        }
    }

    public async Task<McpServer?> GetServerDetailsAsync(string serverId)
    {
        try
        {
            var url = $"mcp/{Uri.EscapeDataString(serverId)}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var plugin = JsonSerializer.Deserialize<McpPlugin>(json);

            return plugin == null ? null : ConvertToServer(plugin);
        }
        catch
        {
            return null;
        }
    }

    private ServerSearchResult ConvertToSearchResult(McpPlugin plugin)
    {
        return new ServerSearchResult
        {
            Server = ConvertToServer(plugin),
            Score = CalculateScore(plugin),
            RegistryName = Name,
            DownloadCount = plugin.DownloadCount ?? 0,
            LastUpdated = TryParseDateTime(plugin.CreatedAt)
        };
    }

    private static McpServer ConvertToServer(McpPlugin plugin)
    {
        // Determine install command based on language type
        var installCommand = plugin.LanguageType?.ToLowerInvariant() switch
        {
            "npm" or "typescript" or "javascript" => $"npx -y {plugin.Name}",
            "python" => $"pip install {plugin.Name}",
            "docker" => $"docker pull {plugin.Name}",
            _ => $"# See repository for installation: {plugin.GithubUrl}"
        };

        // Extract repository name/owner from GitHub URL for better ID
        var serverId = plugin.Id ?? ExtractServerIdFromGitHub(plugin.GithubUrl) ?? plugin.Name ?? Guid.NewGuid().ToString();

        return new McpServer
        {
            Id = serverId,
            Name = plugin.Name ?? "Unknown Server",
            Description = plugin.Description ?? string.Empty,
            Version = "latest", // API doesn't provide version info
            Author = ExtractAuthorFromGitHub(plugin.GithubUrl),
            RepositoryUrl = plugin.GithubUrl ?? string.Empty,
            InstallCommand = installCommand,
            Tags = plugin.Categories ?? []
        };
    }

    private static string ExtractServerIdFromGitHub(string? githubUrl)
    {
        if (string.IsNullOrEmpty(githubUrl))
            return string.Empty;

        try
        {
            var uri = new Uri(githubUrl);
            var segments = uri.Segments.Where(s => s != "/").Select(s => s.TrimEnd('/')).ToArray();

            if (segments.Length >= 2)
            {
                return $"{segments[0]}/{segments[1]}";
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return string.Empty;
    }

    private static string ExtractAuthorFromGitHub(string? githubUrl)
    {
        if (string.IsNullOrEmpty(githubUrl))
            return "Unknown";

        try
        {
            var uri = new Uri(githubUrl);
            var segments = uri.Segments.Where(s => s != "/").Select(s => s.TrimEnd('/')).ToArray();

            if (segments.Length >= 1)
            {
                return segments[0];
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return "Unknown";
    }

    private static DateTime? TryParseDateTime(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var result))
            return result;

        return null;
    }

    private static double CalculateScore(McpPlugin plugin)
    {
        double score = 0;

        // Base score
        score += 1.0;

        // Boost recommended plugins
        if (plugin.IsRecommended == true)
            score += 2.0;

        // Boost verified plugins
        if (plugin.IsVerified == true)
            score += 1.5;

        // Boost by GitHub stars (normalized)
        if (plugin.Stars.HasValue && plugin.Stars.Value > 0)
            score += Math.Min(plugin.Stars.Value / 100.0, 2.0);

        // Boost by downloads (normalized)
        if (plugin.DownloadCount.HasValue && plugin.DownloadCount.Value > 0)
            score += Math.Min(plugin.DownloadCount.Value / 1000.0, 2.0);

        return Math.Min(score / 8.5, 1.0); // Normalize to 0-1 range
    }

    private class RegistryResponse
    {
        [JsonPropertyName("data")]
        public List<McpPlugin>? Data { get; set; }

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    private class McpPlugin
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("githubUrl")]
        public string? GithubUrl { get; set; }

        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("stars")]
        public int? Stars { get; set; }

        [JsonPropertyName("commitHash")]
        public string? CommitHash { get; set; }

        [JsonPropertyName("isVerified")]
        public bool? IsVerified { get; set; }

        [JsonPropertyName("isRecommended")]
        public bool? IsRecommended { get; set; }

        [JsonPropertyName("categories")]
        public List<string>? Categories { get; set; }

        [JsonPropertyName("downloadCount")]
        public long? DownloadCount { get; set; }

        [JsonPropertyName("languageType")]
        public string? LanguageType { get; set; }

        [JsonPropertyName("supportsStdio")]
        public bool? SupportsStdio { get; set; }

        [JsonPropertyName("supportsSSE")]
        public bool? SupportsSSE { get; set; }

        [JsonPropertyName("supportsWebSocket")]
        public bool? SupportsWebSocket { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("bannerUrl")]
        public string? BannerUrl { get; set; }
    }
}
