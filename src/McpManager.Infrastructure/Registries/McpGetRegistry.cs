using McpManager.Core.Interfaces;
using McpManager.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpManager.Infrastructure.Registries;

/// <summary>
/// Registry that connects to the official MCP registry at mcp-get.com.
/// </summary>
public class McpGetRegistry(HttpClient httpClient) : IServerRegistry
{
    private const string BaseUrl = "https://mcp-get.com/api/servers";

    public string Name => "MCP Get Registry";

    public async Task<IEnumerable<ServerSearchResult>> SearchAsync(string query, int maxResults = 50)
    {
        try
        {
            var url = $"{BaseUrl}?search={Uri.EscapeDataString(query)}&limit={maxResults}";
            var response = await httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<McpApiResponse>(json);

            if (apiResponse?.Servers == null)
            {
                return [];
            }

            return apiResponse.Servers.Select(ConvertToSearchResult);
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
            var allServers = new List<McpServerDto>();
            var page = 1;
            var hasMore = true;

            while (hasMore && page <= 10) // Limit to 10 pages
            {
                var url = $"{BaseUrl}?page={page}&limit=50";
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    break;
                }

                var json = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<McpApiResponse>(json);

                if (apiResponse?.Servers == null || !apiResponse.Servers.Any())
                {
                    hasMore = false;
                }
                else
                {
                    allServers.AddRange(apiResponse.Servers);
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
            var url = $"{BaseUrl}/{serverId}";
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var serverDto = JsonSerializer.Deserialize<McpServerDto>(json);

            return serverDto == null ? null : ConvertToServer(serverDto);
        }
        catch
        {
            return null;
        }
    }

    private ServerSearchResult ConvertToSearchResult(McpServerDto dto)
    {
        return new ServerSearchResult
        {
            Server = ConvertToServer(dto),
            Score = 1.0,
            RegistryName = Name,
            DownloadCount = dto.Downloads ?? 0,
            LastUpdated = dto.UpdatedAt
        };
    }

    private static McpServer ConvertToServer(McpServerDto dto)
    {
        return new McpServer
        {
            Id = dto.Id ?? dto.Name?.ToLowerInvariant().Replace(" ", "-") ?? Guid.NewGuid().ToString(),
            Name = dto.Name ?? "Unknown Server",
            Description = dto.Description ?? string.Empty,
            Version = dto.Version ?? "1.0.0",
            Author = dto.Author ?? "Unknown",
            RepositoryUrl = dto.Repository ?? string.Empty,
            InstallCommand = dto.InstallCommand ?? $"npm install -g {dto.Name}",
            Tags = dto.Tags ?? []
        };
    }

    private class McpApiResponse
    {
        [JsonPropertyName("servers")]
        public List<McpServerDto>? Servers { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("page")]
        public int Page { get; set; }
    }

    private class McpServerDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("repository")]
        public string? Repository { get; set; }

        [JsonPropertyName("install_command")]
        public string? InstallCommand { get; set; }

        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }

        [JsonPropertyName("downloads")]
        public long? Downloads { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
