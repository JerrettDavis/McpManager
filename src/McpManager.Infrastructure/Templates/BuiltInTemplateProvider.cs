using McpManager.Core.Interfaces;
using McpManager.Core.Models;

namespace McpManager.Infrastructure.Templates;

/// <summary>
/// Provides built-in server templates with pre-configured server bundles.
/// </summary>
public class BuiltInTemplateProvider(IInstallationManager installationManager) : ITemplateProvider
{
    private static readonly List<ServerTemplate> Templates =
    [
        new ServerTemplate
        {
            Id = "full-stack-developer",
            Name = "Full Stack Developer",
            Description = "Essential tools for full-stack development: file system access, web fetching, search, and GitHub integration.",
            Category = "Development",
            Icon = "code",
            Version = "1.0.0",
            Author = "McpManager",
            Servers =
            [
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-filesystem",
                    Name = "Filesystem",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-filesystem", "." }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-fetch",
                    Name = "Fetch",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-fetch" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-brave-search",
                    Name = "Brave Search",
                    Required = false,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-brave-search" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-github",
                    Name = "GitHub",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-github" }
                    }
                }
            ],
            RequiredEnvironmentVariables = new Dictionary<string, string>
            {
                ["BRAVE_API_KEY"] = "API key for Brave Search (optional if Brave Search server is skipped)",
                ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "GitHub personal access token for repository access"
            }
        },

        new ServerTemplate
        {
            Id = "ai-researcher",
            Name = "AI Researcher",
            Description = "Tools for AI-assisted research: web search, content fetching, memory persistence, and structured reasoning.",
            Category = "Research",
            Icon = "brain",
            Version = "1.0.0",
            Author = "McpManager",
            Servers =
            [
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-brave-search",
                    Name = "Brave Search",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-brave-search" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-fetch",
                    Name = "Fetch",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-fetch" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-memory",
                    Name = "Memory",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-memory" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-sequential-thinking",
                    Name = "Sequential Thinking",
                    Required = false,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-sequential-thinking" }
                    }
                }
            ],
            RequiredEnvironmentVariables = new Dictionary<string, string>
            {
                ["BRAVE_API_KEY"] = "API key for Brave Search"
            }
        },

        new ServerTemplate
        {
            Id = "devops-toolkit",
            Name = "DevOps Toolkit",
            Description = "Infrastructure and operations tooling: file system management, GitHub workflows, Docker, and Kubernetes.",
            Category = "DevOps",
            Icon = "server",
            Version = "1.0.0",
            Author = "McpManager",
            Servers =
            [
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-filesystem",
                    Name = "Filesystem",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-filesystem", "." }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-github",
                    Name = "GitHub",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-github" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-docker",
                    Name = "Docker",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-docker" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-kubernetes",
                    Name = "Kubernetes",
                    Required = false,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-kubernetes" }
                    }
                }
            ],
            RequiredEnvironmentVariables = new Dictionary<string, string>
            {
                ["GITHUB_PERSONAL_ACCESS_TOKEN"] = "GitHub personal access token for repository and workflow access"
            }
        },

        new ServerTemplate
        {
            Id = "data-analyst",
            Name = "Data Analyst",
            Description = "Data analysis and exploration tools: SQLite queries, file system access, web fetching, and browser automation.",
            Category = "Data",
            Icon = "chart",
            Version = "1.0.0",
            Author = "McpManager",
            Servers =
            [
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-sqlite",
                    Name = "SQLite",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-sqlite" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-filesystem",
                    Name = "Filesystem",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-filesystem", "." }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-fetch",
                    Name = "Fetch",
                    Required = true,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-fetch" }
                    }
                },
                new TemplateServer
                {
                    ServerId = "@modelcontextprotocol/server-puppeteer",
                    Name = "Puppeteer",
                    Required = false,
                    DefaultConfig = new Dictionary<string, object>
                    {
                        ["command"] = "npx",
                        ["args"] = new[] { "-y", "@modelcontextprotocol/server-puppeteer" }
                    }
                }
            ],
            RequiredEnvironmentVariables = []
        }
    ];

    public Task<IEnumerable<ServerTemplate>> GetTemplatesAsync()
    {
        return Task.FromResult<IEnumerable<ServerTemplate>>(Templates);
    }

    public Task<ServerTemplate?> GetTemplateByIdAsync(string templateId)
    {
        var template = Templates.FirstOrDefault(t =>
            string.Equals(t.Id, templateId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(template);
    }

    public async Task<TemplateInstallResult> InstallTemplateAsync(string templateId, IEnumerable<string> agentIds)
    {
        var template = await GetTemplateByIdAsync(templateId);
        if (template == null)
        {
            return new TemplateInstallResult
            {
                Success = false,
                Error = $"Template '{templateId}' not found"
            };
        }

        var result = new TemplateInstallResult { Success = true };
        var agentIdList = agentIds.ToList();

        foreach (var templateServer in template.Servers)
        {
            var serverResult = new TemplateServerInstallResult
            {
                ServerId = templateServer.ServerId,
                ServerName = templateServer.Name
            };

            try
            {
                var config = templateServer.DefaultConfig
                    .Where(kvp => kvp.Value is string)
                    .ToDictionary(kvp => kvp.Key, kvp => (string)kvp.Value);

                foreach (var agentId in agentIdList)
                {
                    await installationManager.AddServerToAgentAsync(
                        templateServer.ServerId, agentId, config);
                }

                serverResult.Installed = true;
            }
            catch (Exception ex)
            {
                serverResult.Installed = false;
                serverResult.Error = ex.Message;
                result.Success = false;
            }

            result.ServerResults.Add(serverResult);
        }

        return result;
    }
}
