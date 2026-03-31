using System.Net;
using System.Text.Json;
using McpManager.Infrastructure.Registries;
using Moq;
using Moq.Protected;
using Xunit;

namespace McpManager.Tests.Registries;

/// <summary>
/// Unit tests for SmitheryRegistry using mocked HttpClient.
/// </summary>
public class SmitheryRegistryTests
{
    private static SmitheryRegistry CreateRegistryWithHandler(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://registry.smithery.ai/")
        };
        return new SmitheryRegistry(client);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, string content)
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        return mock;
    }

    private static Mock<HttpMessageHandler> CreateSequentialMockHandler(
        params (HttpStatusCode statusCode, string content)[] responses)
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var sequence = mock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var (statusCode, content) in responses)
        {
            sequence.ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
        }

        return mock;
    }

    #region SearchAsync

    [Fact]
    public async Task SearchAsync_WithValidResponse_ReturnsParsedResults()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new
                {
                    id = "1",
                    @namespace = "acme",
                    name = "test-server",
                    displayName = "Test Server",
                    description = "A test MCP server",
                    repositoryUrl = "https://github.com/acme/test-server",
                    homepageUrl = "https://acme.dev",
                    deploymentType = "npm",
                    latestReleaseVersion = "1.2.3",
                    isVerified = true,
                    isRecommended = false,
                    downloadCount = 5000L,
                    keywords = new[] { "test", "mcp" },
                    createdAt = "2025-01-01T00:00:00Z",
                    updatedAt = "2025-03-01T00:00:00Z",
                    fullName = "@acme/test-server"
                }
            },
            pagination = new { page = 1, limit = 50, hasMore = false }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        // Act
        var results = (await registry.SearchAsync("test")).ToList();

        // Assert
        Assert.Single(results);
        var result = results[0];
        Assert.Equal("Smithery.ai", result.RegistryName);
        Assert.Equal("acme/test-server", result.Server.Id);
        Assert.Equal("Test Server", result.Server.Name);
        Assert.Equal("A test MCP server", result.Server.Description);
        Assert.Equal("1.2.3", result.Server.Version);
        Assert.Equal("acme", result.Server.Author);
        Assert.Contains("npx -y", result.Server.InstallCommand);
        Assert.Equal(5000, result.DownloadCount);
        Assert.True(result.Server.IsVerified);
        Assert.False(result.Server.IsRecommended);
        Assert.Contains("test", result.Server.Tags);
        Assert.Contains("mcp", result.Server.Tags);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyData_ReturnsEmpty()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = Array.Empty<object>(),
            pagination = new { page = 1, limit = 50, hasMore = false }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = await registry.SearchAsync("nonexistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithNullData_ReturnsEmpty()
    {
        var json = "{}";
        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = await registry.SearchAsync("test");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithHttpError_ReturnsEmpty()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "");
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = await registry.SearchAsync("test");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithMalformedJson_ReturnsEmpty()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, "not valid json {{{");
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = await registry.SearchAsync("test");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithRequestException_ReturnsEmpty()
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var registry = CreateRegistryWithHandler(mock.Object);

        var results = await registry.SearchAsync("test");

        Assert.Empty(results);
    }

    #endregion

    #region GetAllServersAsync

    [Fact]
    public async Task GetAllServersAsync_SinglePage_ReturnsResults()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new
                {
                    id = "1",
                    @namespace = "acme",
                    name = "server-one",
                    displayName = "Server One",
                    description = "First server",
                    repositoryUrl = "https://github.com/acme/server-one",
                    deploymentType = "npm",
                    fullName = "@acme/server-one"
                }
            },
            pagination = new { page = 1, limit = 50, hasMore = false }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = (await registry.GetAllServersAsync()).ToList();

        Assert.Single(results);
        Assert.Equal("acme/server-one", results[0].Server.Id);
    }

    [Fact]
    public async Task GetAllServersAsync_MultiplePages_PaginatesCorrectly()
    {
        var page1 = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { id = "1", @namespace = "a", name = "s1", fullName = "a/s1" }
            },
            pagination = new { page = 1, limit = 50, hasMore = true }
        });

        var page2 = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new { id = "2", @namespace = "b", name = "s2", fullName = "b/s2" }
            },
            pagination = new { page = 2, limit = 50, hasMore = false }
        });

        var handler = CreateSequentialMockHandler(
            (HttpStatusCode.OK, page1),
            (HttpStatusCode.OK, page2));
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = (await registry.GetAllServersAsync()).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("a/s1", results[0].Server.Id);
        Assert.Equal("b/s2", results[1].Server.Id);
    }

    [Fact]
    public async Task GetAllServersAsync_ApiError_ReturnsPartialOrEmpty()
    {
        var handler = CreateMockHandler(HttpStatusCode.ServiceUnavailable, "");
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = await registry.GetAllServersAsync();

        Assert.Empty(results);
    }

    #endregion

    #region GetServerDetailsAsync

    [Fact]
    public async Task GetServerDetailsAsync_WithValidResponse_ReturnsServer()
    {
        var json = JsonSerializer.Serialize(new
        {
            id = "42",
            @namespace = "acme",
            name = "detail-server",
            displayName = "Detail Server",
            description = "Detailed description",
            repositoryUrl = "https://github.com/acme/detail-server",
            homepageUrl = "https://detail.acme.dev",
            deploymentType = "python",
            latestReleaseVersion = "2.0.0",
            isVerified = true,
            isRecommended = true,
            downloadCount = 10000L,
            keywords = new[] { "detail" },
            fullName = "acme-detail-server"
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var server = await registry.GetServerDetailsAsync("acme/detail-server");

        Assert.NotNull(server);
        Assert.Equal("acme/detail-server", server.Id);
        Assert.Equal("Detail Server", server.Name);
        Assert.Equal("2.0.0", server.Version);
        Assert.Equal("acme", server.Author);
        Assert.Contains("pip install", server.InstallCommand);
        Assert.True(server.IsVerified);
        Assert.True(server.IsRecommended);
        Assert.Equal("https://detail.acme.dev", server.HomepageUrl);
    }

    [Fact]
    public async Task GetServerDetailsAsync_NotFound_ReturnsNull()
    {
        var handler = CreateMockHandler(HttpStatusCode.NotFound, "");
        var registry = CreateRegistryWithHandler(handler.Object);

        var server = await registry.GetServerDetailsAsync("nonexistent/server");

        Assert.Null(server);
    }

    [Fact]
    public async Task GetServerDetailsAsync_MalformedJson_ReturnsNull()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, "{{invalid json");
        var registry = CreateRegistryWithHandler(handler.Object);

        var server = await registry.GetServerDetailsAsync("acme/broken");

        Assert.Null(server);
    }

    [Fact]
    public async Task GetServerDetailsAsync_ConnectionError_ReturnsNull()
    {
        var mock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var registry = CreateRegistryWithHandler(mock.Object);

        var server = await registry.GetServerDetailsAsync("acme/timeout");

        Assert.Null(server);
    }

    #endregion

    #region Model Mapping

    [Theory]
    [InlineData("npm", "npx -y")]
    [InlineData("python", "pip install")]
    [InlineData("pypi", "pip install")]
    [InlineData("docker", "docker pull")]
    public async Task ConvertToServer_MapsDeploymentType_ToCorrectInstallCommand(
        string deploymentType, string expectedPrefix)
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new
                {
                    id = "1",
                    @namespace = "acme",
                    name = "cmd-test",
                    deploymentType,
                    fullName = "acme/cmd-test"
                }
            },
            pagination = new { page = 1, limit = 50, hasMore = false }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = (await registry.SearchAsync("cmd-test")).ToList();

        Assert.Single(results);
        Assert.StartsWith(expectedPrefix, results[0].Server.InstallCommand);
    }

    [Fact]
    public async Task ConvertToServer_UnknownDeploymentType_FallsBackToSeeUrl()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new
                {
                    id = "1",
                    @namespace = "acme",
                    name = "unknown-type",
                    deploymentType = "custom",
                    repositoryUrl = "https://github.com/acme/unknown-type",
                    fullName = "acme/unknown-type"
                }
            },
            pagination = new { page = 1, limit = 50, hasMore = false }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = (await registry.SearchAsync("unknown")).ToList();

        Assert.Single(results);
        Assert.Contains("# See", results[0].Server.InstallCommand);
    }

    [Fact]
    public async Task CalculateScore_VerifiedAndRecommended_ProducesHigherScore()
    {
        var makeJson = (bool verified, bool recommended, long downloads) =>
            JsonSerializer.Serialize(new
            {
                data = new[]
                {
                    new
                    {
                        id = "1",
                        @namespace = "acme",
                        name = "score-test",
                        isVerified = verified,
                        isRecommended = recommended,
                        downloadCount = downloads,
                        fullName = "acme/score-test"
                    }
                },
                pagination = new { page = 1, limit = 50, hasMore = false }
            });

        // Baseline: not verified, not recommended, no downloads
        var baseHandler = CreateMockHandler(HttpStatusCode.OK, makeJson(false, false, 0));
        var baseRegistry = CreateRegistryWithHandler(baseHandler.Object);
        var baseResults = (await baseRegistry.SearchAsync("score")).ToList();
        var baseScore = baseResults[0].Score;

        // Verified + recommended + high downloads
        var boostedHandler = CreateMockHandler(HttpStatusCode.OK, makeJson(true, true, 5000));
        var boostedRegistry = CreateRegistryWithHandler(boostedHandler.Object);
        var boostedResults = (await boostedRegistry.SearchAsync("score")).ToList();
        var boostedScore = boostedResults[0].Score;

        Assert.True(boostedScore > baseScore,
            $"Boosted score ({boostedScore}) should be higher than base score ({baseScore})");
    }

    [Fact]
    public async Task ConvertToServer_NullFields_DefaultsGracefully()
    {
        var json = JsonSerializer.Serialize(new
        {
            data = new[]
            {
                new
                {
                    id = "1",
                    // Most fields are null/missing
                    @namespace = (string?)null,
                    name = (string?)null,
                    displayName = (string?)null,
                    description = (string?)null,
                    repositoryUrl = (string?)null,
                    deploymentType = (string?)null,
                    latestReleaseVersion = (string?)null,
                    keywords = (string[]?)null,
                    fullName = (string?)null
                }
            },
            pagination = new { page = 1, limit = 50, hasMore = false }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var registry = CreateRegistryWithHandler(handler.Object);

        var results = (await registry.SearchAsync("null-test")).ToList();

        Assert.Single(results);
        var server = results[0].Server;
        Assert.Equal("Unknown Server", server.Name);
        Assert.Equal(string.Empty, server.Description);
        Assert.Equal("latest", server.Version);
        Assert.Equal("Unknown", server.Author);
        Assert.Empty(server.Tags);
    }

    #endregion

    #region Registry Name

    [Fact]
    public void Name_ReturnsSmitheryAi()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, "{}");
        var registry = CreateRegistryWithHandler(handler.Object);

        Assert.Equal("Smithery.ai", registry.Name);
    }

    #endregion
}
