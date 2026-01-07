using McpManager.Core.Interfaces;
using McpManager.Application.Services;
using McpManager.Core.Models;
using McpManager.Infrastructure.Connectors;
using McpManager.Infrastructure.Registries;
using McpManager.Infrastructure.Persistence;
using McpManager.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace McpManager.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring McpManager services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all McpManager services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="dbPath">Optional database path. If not provided, uses default location.</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddMcpManagerServices(
        this IServiceCollection services, 
        string appName = "McpManager",
        string? dbPath = null)
    {
        // Configure database
        var databasePath = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "mcpmanager.db");

        // Ensure directory exists
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContext<McpManagerDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        // Register repositories
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IRegistryCacheRepository, RegistryCacheRepository>();

        // Register HttpClient for MCP registries with proper factory pattern
        services.AddHttpClient("ModelContextProtocolRegistry", client =>
        {
            client.BaseAddress = new Uri("https://registry.modelcontextprotocol.io/");
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IServerRegistry>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ModelContextProtocolRegistry");
            var innerRegistry = new ModelContextProtocolRegistry(client);
            
            // Wrap with caching for better performance - use service provider to get scoped dependencies when needed
            return new CachedServerRegistry(innerRegistry, sp);
        });

        services.AddHttpClient("McpServersComRegistry", client =>
        {
            client.BaseAddress = new Uri("https://api.mcpservers.com/api/v1/");
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IServerRegistry>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("McpServersComRegistry");
            var innerRegistry = new McpServersComRegistry(client);
            
            // Wrap with caching for better performance - use service provider to get scoped dependencies when needed
            return new CachedServerRegistry(innerRegistry, sp);
        });

        services.AddHttpClient("ModelContextProtocolGitHubRegistry", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddSingleton<IServerRegistry>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("ModelContextProtocolGitHubRegistry");
            var innerRegistry = new ModelContextProtocolGitHubRegistry(client);
            
            // Wrap with caching for better performance - use service provider to get scoped dependencies when needed
            return new CachedServerRegistry(innerRegistry, sp);
        });

        // Register application services
        services.AddScoped<IServerManager, ServerManager>();
        services.AddScoped<IInstallationManager, InstallationManager>();
        services.AddScoped<IConfigurationService, ConfigurationService>();
        services.AddSingleton<IAgentManager, AgentManager>();
        services.AddSingleton<ICollection<ServerInstallation>, List<ServerInstallation>>(_ => []);
        services.AddSingleton<IServerMonitor, ServerMonitor>();
        services.AddSingleton<ConfigurationParser>();

        // Register agent connectors
        services.AddSingleton<IAgentConnector, ClaudeConnector>();
        services.AddSingleton<IAgentConnector, CopilotConnector>();
        services.AddSingleton<IAgentConnector, ClaudeCodeConnector>();
        services.AddSingleton<IAgentConnector, CodexConnector>();

        // Register mock registry for demo/fallback
        services.AddSingleton<IServerRegistry, MockServerRegistry>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    public static void EnsureDatabaseCreated(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<McpManagerDbContext>();
        dbContext.Database.Migrate();
    }
}
