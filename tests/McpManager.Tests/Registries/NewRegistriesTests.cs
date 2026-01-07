using McpManager.Infrastructure.Registries;
using Xunit;

namespace McpManager.Tests.Registries;

/// <summary>
/// Tests for the new MCP server registries
/// </summary>
public class NewRegistriesTests
{
    [Fact(Skip = "Integration test - requires live API and can be slow")]
    public async Task McpServersComRegistry_GetAllServersAsync_ReturnsResults()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.mcpservers.com/api/v1/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "McpManager-Test/1.0");
        var registry = new McpServersComRegistry(httpClient);

        // Act
        var results = await registry.GetAllServersAsync();

        // Assert
        Assert.NotNull(results);
        var resultList = results.ToList();

        // Should have some servers (assuming the API is available)
        Assert.NotEmpty(resultList);

        // Verify structure
        var firstResult = resultList.First();
        Assert.Equal("MCPServers.com", firstResult.RegistryName);
        Assert.NotNull(firstResult.Server);
        Assert.NotNull(firstResult.Server.Name);
    }

    [Fact]
    public async Task McpServersComRegistry_SearchAsync_ReturnsFilteredResults()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.mcpservers.com/api/v1/")
        };
        httpClient.DefaultRequestHeaders.Add("User-Agent", "McpManager-Test/1.0");
        var registry = new McpServersComRegistry(httpClient);

        // Act
        var results = await registry.SearchAsync("github", maxResults: 10);

        // Assert
        Assert.NotNull(results);
        var resultList = results.ToList();
        Assert.True(resultList.Count <= 10);

        // All results should contain "github" in name or description
        foreach (var result in resultList)
        {
            var containsGithub =
                result.Server.Name.Contains("github", StringComparison.OrdinalIgnoreCase) ||
                result.Server.Description.Contains("github", StringComparison.OrdinalIgnoreCase);
            Assert.True(containsGithub, $"Server {result.Server.Name} doesn't contain 'github'");
        }
    }

    [Fact]
    public async Task ModelContextProtocolGitHubRegistry_GetAllServersAsync_ReturnsReferenceServers()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "McpManager-Test/1.0");
        var registry = new ModelContextProtocolGitHubRegistry(httpClient);

        // Act
        var results = await registry.GetAllServersAsync();

        // Assert
        Assert.NotNull(results);
        var resultList = results.ToList();

        // Should have exactly 7 reference servers
        Assert.Equal(7, resultList.Count);

        // Verify all servers are from Anthropic
        foreach (var result in resultList)
        {
            Assert.Equal("MCP GitHub Reference Servers", result.RegistryName);
            Assert.Equal("Anthropic", result.Server.Author);
            Assert.Contains("Official", result.Server.Tags);
            Assert.Contains("Reference", result.Server.Tags);
        }
    }

    [Fact]
    public async Task ModelContextProtocolGitHubRegistry_SearchAsync_FiltersCorrectly()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "McpManager-Test/1.0");
        var registry = new ModelContextProtocolGitHubRegistry(httpClient);

        // Act
        var results = await registry.SearchAsync("git");

        // Assert
        Assert.NotNull(results);
        var resultList = results.ToList();

        // Should find "Git" server
        Assert.NotEmpty(resultList);
        Assert.Contains(resultList, r => r.Server.Name.Equals("Git", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ModelContextProtocolGitHubRegistry_GetServerDetailsAsync_ReturnsServerInfo()
    {
        // Arrange
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "McpManager-Test/1.0");
        var registry = new ModelContextProtocolGitHubRegistry(httpClient);

        // Act
        var server = await registry.GetServerDetailsAsync("@modelcontextprotocol/server-git");

        // Assert
        Assert.NotNull(server);
        Assert.Equal("Git", server.Name);
        Assert.Equal("Anthropic", server.Author);
        Assert.Contains("git", server.InstallCommand.ToLowerInvariant());
    }

    [Fact]
    public void McpServersComRegistry_HasCorrectName()
    {
        // Arrange & Act
        var httpClient = new HttpClient();
        var registry = new McpServersComRegistry(httpClient);

        // Assert
        Assert.Equal("MCPServers.com", registry.Name);
    }

    [Fact]
    public void ModelContextProtocolGitHubRegistry_HasCorrectName()
    {
        // Arrange & Act
        var httpClient = new HttpClient();
        var registry = new ModelContextProtocolGitHubRegistry(httpClient);

        // Assert
        Assert.Equal("MCP GitHub Reference Servers", registry.Name);
    }
}
