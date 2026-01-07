using Xunit;
using Xunit.Abstractions;

namespace McpManager.Tests.Diagnostics;

/// <summary>
/// Diagnostic tests to check actual API responses and identify deserialization issues
/// </summary>
public class RegistryApiDiagnostics(ITestOutputHelper output)
{
    [Fact]
    public async Task McpServersComAPI_RawResponse()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.mcpservers.com/api/v1/")
        };
        client.DefaultRequestHeaders.Add("User-Agent", "McpManager-Diagnostic/1.0");

        try
        {
            output.WriteLine("Testing MCPServers.com API...");
            output.WriteLine($"Base URL: {client.BaseAddress}");

            // Test endpoint
            var url = "mcp/registry?limit=5";
            output.WriteLine($"Request URL: {url}");

            var response = await client.GetAsync(url);
            output.WriteLine($"Status: {response.StatusCode}");
            output.WriteLine($"Success: {response.IsSuccessStatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            output.WriteLine($"Response length: {content.Length}");
            output.WriteLine("Response content:");
            output.WriteLine(content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

            Assert.True(response.IsSuccessStatusCode, $"API call failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            output.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            output.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task ModelContextProtocolAPI_RawResponse()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://registry.modelcontextprotocol.io/")
        };
        client.DefaultRequestHeaders.Add("User-Agent", "McpManager-Diagnostic/1.0");

        try
        {
            output.WriteLine("Testing ModelContextProtocol Registry API...");
            output.WriteLine($"Base URL: {client.BaseAddress}");

            // Test endpoint
            var url = "v0.1/servers";
            output.WriteLine($"Request URL: {url}");

            var response = await client.GetAsync(url);
            output.WriteLine($"Status: {response.StatusCode}");
            output.WriteLine($"Success: {response.IsSuccessStatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                output.WriteLine($"Headers: {string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
            }

            var content = await response.Content.ReadAsStringAsync();
            output.WriteLine($"Response length: {content.Length}");
            output.WriteLine("Response content:");
            output.WriteLine(content.Length > 1000 ? content.Substring(0, 1000) + "..." : content);

            Assert.True(response.IsSuccessStatusCode, $"API call failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            output.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            output.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task GitHubRaw_PackageLock()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "McpManager-Diagnostic/1.0");

        try
        {
            output.WriteLine("Testing GitHub raw content...");

            var url = "https://raw.githubusercontent.com/modelcontextprotocol/servers/refs/heads/main/package-lock.json";
            output.WriteLine($"Request URL: {url}");

            var response = await client.GetAsync(url);
            output.WriteLine($"Status: {response.StatusCode}");
            output.WriteLine($"Success: {response.IsSuccessStatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            output.WriteLine($"Response length: {content.Length}");
            output.WriteLine("First 500 chars:");
            output.WriteLine(content.Length > 500 ? content.Substring(0, 500) : content);

            Assert.True(response.IsSuccessStatusCode, $"API call failed: {response.StatusCode}");
            Assert.True(content.Contains("@modelcontextprotocol"), "Should contain MCP packages");
        }
        catch (Exception ex)
        {
            output.WriteLine($"ERROR: {ex.GetType().Name}: {ex.Message}");
            output.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    [Fact]
    public async Task TestAllRegistries_WithActualHTTPClient()
    {
        output.WriteLine("=== Testing All Registries with Real HTTP Calls ===\n");

        // Test 1: MCPServers.com
        output.WriteLine("1. MCPServers.com Registry");
        try
        {
            var mcpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.mcpservers.com/api/v1/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            mcpClient.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");

            var mcpRegistry = new Infrastructure.Registries.McpServersComRegistry(mcpClient);
            var mcpServers = await mcpRegistry.GetAllServersAsync();
            var mcpCount = mcpServers.Count();

            output.WriteLine($"   Result: {mcpCount} servers");
            if (mcpCount > 0)
            {
                var sample = mcpServers.First();
                output.WriteLine($"   Sample: {sample.Server.Name} ({sample.Server.Id})");
            }
            else
            {
                output.WriteLine("   WARNING: No servers returned!");
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"   ERROR: {ex.Message}");
        }

        output.WriteLine("");

        // Test 2: ModelContextProtocol Registry
        output.WriteLine("2. ModelContextProtocol Registry");
        try
        {
            var mcpProtocolClient = new HttpClient
            {
                BaseAddress = new Uri("https://registry.modelcontextprotocol.io/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            mcpProtocolClient.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");

            var protocolRegistry = new Infrastructure.Registries.ModelContextProtocolRegistry(mcpProtocolClient);
            var protocolServers = await protocolRegistry.GetAllServersAsync();
            var protocolCount = protocolServers.Count();

            output.WriteLine($"   Result: {protocolCount} servers");
            if (protocolCount > 0)
            {
                var sample = protocolServers.First();
                output.WriteLine($"   Sample: {sample.Server.Name} ({sample.Server.Id})");
            }
            else
            {
                output.WriteLine("   WARNING: No servers returned!");
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"   ERROR: {ex.Message}");
        }

        output.WriteLine("");

        // Test 3: GitHub Reference Servers
        output.WriteLine("3. GitHub Reference Servers");
        try
        {
            var githubClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            githubClient.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");

            var githubRegistry = new Infrastructure.Registries.ModelContextProtocolGitHubRegistry(githubClient);
            var githubServers = await githubRegistry.GetAllServersAsync();
            var githubCount = githubServers.Count();

            output.WriteLine($"   Result: {githubCount} servers");
            if (githubCount > 0)
            {
                foreach (var server in githubServers)
                {
                    output.WriteLine($"   - {server.Server.Name}");
                }
            }
            else
            {
                output.WriteLine("   WARNING: No servers returned!");
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"   ERROR: {ex.Message}");
        }
    }
}
