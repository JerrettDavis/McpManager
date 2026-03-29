using McpManager.Core.Interfaces;
using McpManager.Infrastructure.Templates;
using Moq;

namespace McpManager.Tests.Templates;

public class TemplateProviderTests
{
    private readonly BuiltInTemplateProvider _provider;
    private readonly Mock<IInstallationManager> _mockInstallationManager;

    public TemplateProviderTests()
    {
        _mockInstallationManager = new Mock<IInstallationManager>();
        _provider = new BuiltInTemplateProvider(_mockInstallationManager.Object);
    }

    [Fact]
    public async Task GetTemplatesAsync_ReturnsFourBuiltInTemplates()
    {
        var templates = (await _provider.GetTemplatesAsync()).ToList();

        Assert.Equal(4, templates.Count);
    }

    [Theory]
    [InlineData("full-stack-developer", "Full Stack Developer")]
    [InlineData("ai-researcher", "AI Researcher")]
    [InlineData("devops-toolkit", "DevOps Toolkit")]
    [InlineData("data-analyst", "Data Analyst")]
    public async Task GetTemplateByIdAsync_ReturnsCorrectTemplate(string id, string expectedName)
    {
        var template = await _provider.GetTemplateByIdAsync(id);

        Assert.NotNull(template);
        Assert.Equal(id, template.Id);
        Assert.Equal(expectedName, template.Name);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_WithUnknownId_ReturnsNull()
    {
        var template = await _provider.GetTemplateByIdAsync("non-existent-template");

        Assert.Null(template);
    }

    [Fact]
    public async Task GetTemplateByIdAsync_IsCaseInsensitive()
    {
        var template = await _provider.GetTemplateByIdAsync("FULL-STACK-DEVELOPER");

        Assert.NotNull(template);
        Assert.Equal("full-stack-developer", template.Id);
    }

    [Fact]
    public async Task EachTemplate_HasAtLeastTwoServers()
    {
        var templates = await _provider.GetTemplatesAsync();

        foreach (var template in templates)
        {
            Assert.True(template.Servers.Count >= 2,
                $"Template '{template.Id}' has {template.Servers.Count} servers, expected at least 2");
        }
    }

    [Fact]
    public async Task AllTemplates_HaveValidIdsAndDescriptions()
    {
        var templates = await _provider.GetTemplatesAsync();

        foreach (var template in templates)
        {
            Assert.False(string.IsNullOrWhiteSpace(template.Id),
                "Template ID must not be empty");
            Assert.False(string.IsNullOrWhiteSpace(template.Name),
                $"Template '{template.Id}' must have a name");
            Assert.False(string.IsNullOrWhiteSpace(template.Description),
                $"Template '{template.Id}' must have a description");
            Assert.False(string.IsNullOrWhiteSpace(template.Category),
                $"Template '{template.Id}' must have a category");
            Assert.False(string.IsNullOrWhiteSpace(template.Author),
                $"Template '{template.Id}' must have an author");
            Assert.False(string.IsNullOrWhiteSpace(template.Version),
                $"Template '{template.Id}' must have a version");
        }
    }

    [Fact]
    public async Task AllTemplateServers_HaveValidServerIds()
    {
        var templates = await _provider.GetTemplatesAsync();

        foreach (var template in templates)
        {
            foreach (var server in template.Servers)
            {
                Assert.False(string.IsNullOrWhiteSpace(server.ServerId),
                    $"Server in template '{template.Id}' must have a ServerId");
                Assert.False(string.IsNullOrWhiteSpace(server.Name),
                    $"Server '{server.ServerId}' in template '{template.Id}' must have a Name");
            }
        }
    }

    [Fact]
    public async Task InstallTemplateAsync_WithUnknownId_ReturnsFailure()
    {
        var result = await _provider.InstallTemplateAsync("non-existent", ["agent-1"]);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Empty(result.ServerResults);
    }
}
