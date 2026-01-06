using McpManager.Application.Services;
using Xunit;

namespace McpManager.Tests.Services;

public class ConfigurationParserTests
{
    private readonly ConfigurationParser _parser;

    public ConfigurationParserTests()
    {
        _parser = new ConfigurationParser();
    }

    [Fact]
    public void ParseConfiguration_WithEmptyString_ReturnsFalse()
    {
        // Act
        var (success, server, error) = _parser.ParseConfiguration("");

        // Assert
        Assert.False(success);
        Assert.Null(server);
        Assert.Contains("empty", error.ToLower());
    }

    [Fact]
    public void ParseConfiguration_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        var (success, server, error) = _parser.ParseConfiguration(invalidJson);

        // Assert
        Assert.False(success);
        Assert.Null(server);
        Assert.Contains("json", error.ToLower());
    }

    [Fact]
    public void ParseConfiguration_WithClaudeStyleFullConfig_ReturnsServer()
    {
        // Arrange
        var config = @"{
            ""mcpServers"": {
                ""my-server"": {
                    ""command"": ""node"",
                    ""args"": ""/path/to/server.js""
                }
            }
        }";

        // Act
        var (success, server, error) = _parser.ParseConfiguration(config);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Equal("my-server", server.Id);
        Assert.Equal("node", server.Configuration["command"]);
        Assert.Equal("/path/to/server.js", server.Configuration["args"]);
    }

    [Fact]
    public void ParseConfiguration_WithCodexStyleSingleServer_ReturnsServer()
    {
        // Arrange
        var config = @"{
            ""command"": ""npx"",
            ""args"": [""server-filesystem"", ""/path""],
            ""env"": {
                ""API_KEY"": ""test-key""
            }
        }";

        // Act
        var (success, server, error) = _parser.ParseConfiguration(config, "test-server");

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Equal("test-server", server.Id);
        Assert.Equal("npx", server.Configuration["command"]);
        Assert.Contains("server-filesystem", server.Configuration["args"]);
        Assert.Contains("/path", server.Configuration["args"]);
        Assert.Contains("API_KEY", server.Configuration["env"]);
    }

    [Fact]
    public void ParseConfiguration_WithCodexStyleArgsAsArray_ConvertsToString()
    {
        // Arrange
        var config = @"{
            ""command"": ""python"",
            ""args"": [""-m"", ""myserver"", ""--port"", ""8080""]
        }";

        // Act
        var (success, server, error) = _parser.ParseConfiguration(config);

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Contains("-m", server.Configuration["args"]);
        Assert.Contains("myserver", server.Configuration["args"]);
        Assert.Contains("8080", server.Configuration["args"]);
    }

    [Fact]
    public void ParseConfiguration_WithSimpleKeyValue_ReturnsServer()
    {
        // Arrange
        var config = @"{
            ""command"": ""node"",
            ""path"": ""/usr/local/bin/server"",
            ""enabled"": ""true""
        }";

        // Act
        var (success, server, error) = _parser.ParseConfiguration(config, "simple-server");

        // Assert
        Assert.True(success);
        Assert.NotNull(server);
        Assert.Equal("simple-server", server.Id);
        Assert.Equal("node", server.Configuration["command"]);
        Assert.Equal("/usr/local/bin/server", server.Configuration["path"]);
        Assert.Equal("true", server.Configuration["enabled"]);
    }

    [Fact]
    public void CreateServerFromManualInput_CreatesValidServer()
    {
        // Act
        var server = _parser.CreateServerFromManualInput(
            id: "test-server",
            name: "Test Server",
            description: "A test server",
            command: "node",
            args: "/path/to/server.js",
            version: "1.0.0",
            author: "Test Author"
        );

        // Assert
        Assert.NotNull(server);
        Assert.Equal("test-server", server.Id);
        Assert.Equal("Test Server", server.Name);
        Assert.Equal("A test server", server.Description);
        Assert.Equal("node", server.Configuration["command"]);
        Assert.Equal("/path/to/server.js", server.Configuration["args"]);
        Assert.Equal("1.0.0", server.Version);
        Assert.Equal("Test Author", server.Author);
    }

    [Fact]
    public void CreateServerFromManualInput_WithEmptyId_GeneratesId()
    {
        // Act
        var server = _parser.CreateServerFromManualInput(
            id: "",
            name: "Test Server",
            description: "A test server",
            command: "node",
            args: "/path/to/server.js"
        );

        // Assert
        Assert.NotNull(server);
        Assert.NotEmpty(server.Id);
        Assert.StartsWith("custom-", server.Id);
    }

    [Fact]
    public void CreateServerFromManualInput_WithEnvVars_IncludesEnvInConfig()
    {
        // Arrange
        var envVars = new Dictionary<string, string>
        {
            ["API_KEY"] = "test-key",
            ["DEBUG"] = "true"
        };

        // Act
        var server = _parser.CreateServerFromManualInput(
            id: "test-server",
            name: "Test Server",
            description: "A test server",
            command: "node",
            args: "/path/to/server.js",
            envVars: envVars
        );

        // Assert
        Assert.NotNull(server);
        Assert.True(server.Configuration.ContainsKey("env"));
        Assert.Contains("API_KEY", server.Configuration["env"]);
        Assert.Contains("test-key", server.Configuration["env"]);
    }

    [Fact]
    public void CreateServerFromManualInput_WithoutVersion_UsesDefault()
    {
        // Act
        var server = _parser.CreateServerFromManualInput(
            id: "test-server",
            name: "Test Server",
            description: "A test server",
            command: "node",
            args: "/path/to/server.js"
        );

        // Assert
        Assert.NotNull(server);
        Assert.Equal("1.0.0", server.Version);
        Assert.Equal("Custom", server.Author);
    }
}
