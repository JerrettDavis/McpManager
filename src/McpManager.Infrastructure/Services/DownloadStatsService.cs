using McpManager.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace McpManager.Infrastructure.Services;

/// <summary>
/// Service for fetching and caching package download statistics.
/// </summary>
public interface IDownloadStatsService
{
    /// <summary>
    /// Fetches download count for an NPM package.
    /// </summary>
    Task<long?> GetNpmDownloadsAsync(string packageName);

    /// <summary>
    /// Fetches download count for a PyPI package.
    /// </summary>
    Task<long?> GetPyPiDownloadsAsync(string packageName);

    /// <summary>
    /// Updates download counts for all servers in the cache that need refreshing.
    /// </summary>
    Task UpdateDownloadCountsAsync();
}

/// <summary>
/// Implementation of download statistics service with caching.
/// </summary>
public class DownloadStatsService(
    IHttpClientFactory httpClientFactory,
    IRegistryCacheRepository cacheRepository,
    ILogger<DownloadStatsService> logger) : IDownloadStatsService
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromDays(3);

    public async Task<long?> GetNpmDownloadsAsync(string packageName)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("NpmRegistry");
            
            // NPM registry API for weekly downloads
            var response = await client.GetAsync($"https://api.npmjs.org/downloads/point/last-week/{packageName}");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonSerializer.Deserialize<NpmDownloadResponse>(json);
            
            return data?.Downloads;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch NPM downloads for package: {PackageName}", packageName);
            return null;
        }
    }

    public async Task<long?> GetPyPiDownloadsAsync(string packageName)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("PyPIRegistry");
            
            // PyPI JSON API
            var response = await client.GetAsync($"https://pypi.org/pypi/{packageName}/json");
            
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = System.Text.Json.JsonSerializer.Deserialize<PyPiPackageResponse>(json);
            
            // PyPI doesn't provide download counts in the API anymore
            // Would need to use pypistats.org or BigQuery
            // For now, return null and we can enhance later
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch PyPI downloads for package: {PackageName}", packageName);
            return null;
        }
    }

    public async Task UpdateDownloadCountsAsync()
    {
        logger.LogInformation("Starting download counts update");

        var allServers = await cacheRepository.GetAllAsync();
        var serversToUpdate = allServers.Where(s => ShouldUpdateDownloads(s)).ToList();

        logger.LogInformation("Updating download counts for {Count} servers", serversToUpdate.Count);

        var updateTasks = serversToUpdate.Select(async server =>
        {
            try
            {
                var packageName = ExtractPackageName(server.Server.InstallCommand);
                if (string.IsNullOrEmpty(packageName))
                {
                    return;
                }

                long? downloads = null;

                // Determine package type from install command
                if (server.Server.InstallCommand.Contains("npm") || server.Server.InstallCommand.Contains("npx"))
                {
                    downloads = await GetNpmDownloadsAsync(packageName);
                }
                else if (server.Server.InstallCommand.Contains("pip") || server.Server.InstallCommand.Contains("python"))
                {
                    downloads = await GetPyPiDownloadsAsync(packageName);
                }

                if (downloads.HasValue && downloads.Value > 0)
                {
                    // Update the server's download count
                    server.DownloadCount = downloads.Value;
                    
                    // Update in database
                    await cacheRepository.UpsertManyAsync(server.RegistryName, new[] { server });
                    
                    logger.LogDebug("Updated {ServerName}: {Downloads} downloads", 
                        server.Server.Name, downloads.Value);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to update downloads for {ServerName}", server.Server.Name);
            }
        });

        await Task.WhenAll(updateTasks);

        logger.LogInformation("Download counts update completed");
    }

    private static bool ShouldUpdateDownloads(Core.Models.ServerSearchResult server)
    {
        // Update if:
        // 1. No download count set (DownloadCount == 0)
        // 2. Downloads haven't been updated recently (check metadata if available)
        if (server.DownloadCount == 0)
        {
            return true;
        }

        // For now, we'll update based on LastUpdated field
        // TODO: Use DownloadsLastUpdated from metadata once migration is in place
        var age = DateTime.UtcNow - (server.LastUpdated ?? DateTime.MinValue);
        return age > StaleThreshold;
    }

    private static string? ExtractPackageName(string installCommand)
    {
        if (string.IsNullOrWhiteSpace(installCommand))
        {
            return null;
        }

        // Handle various install command formats:
        // npm install @modelcontextprotocol/server-filesystem
        // npx -y @modelcontextprotocol/server-filesystem
        // pip install mcp-server-git
        // uvx mcp-server-git

        var parts = installCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Find the package name (usually after install/-y or last argument)
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "install" || parts[i] == "-y")
            {
                if (i + 1 < parts.Length)
                {
                    return parts[i + 1];
                }
            }
        }

        // If no install keyword, take the last part that looks like a package
        var lastPart = parts.LastOrDefault(p => p.StartsWith("@") || p.Contains("-") || p.Contains("_"));
        return lastPart;
    }

    private class NpmDownloadResponse
    {
        public long Downloads { get; set; }
        public string? Package { get; set; }
    }

    private class PyPiPackageResponse
    {
        public PyPiInfo? Info { get; set; }
    }

    private class PyPiInfo
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
    }
}
