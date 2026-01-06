using McpManager.Core.Interfaces;
using McpManager.Application.Services;
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
        string? dbPath = null)
    {
        // Configure database
        var databasePath = dbPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpManager",
            "mcpmanager.db");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        services.AddDbContext<McpManagerDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        // Register repositories
        services.AddScoped<IServerRepository, ServerRepository>();
        services.AddScoped<IRegistryCacheRepository, RegistryCacheRepository>();

        // Register HttpClient for MCP registries
        services.AddHttpClient<IServerRegistry, ModelContextProtocolRegistry>(client =>
        {
            client.BaseAddress = new Uri("https://registry.modelcontextprotocol.io/");
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
        });

        // Register application services
        services.AddScoped<IServerManager, ServerManager>();
        services.AddSingleton<IAgentManager, AgentManager>();
        services.AddSingleton<IInstallationManager, InstallationManager>();
        services.AddSingleton<IServerMonitor, ServerMonitor>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ConfigurationParser>();

        // Register agent connectors
        services.AddSingleton<IAgentConnector, ClaudeConnector>();
        services.AddSingleton<IAgentConnector, CopilotConnector>();
        services.AddSingleton<IAgentConnector, ClaudeCodeConnector>();
        services.AddSingleton<IAgentConnector, CodexConnector>();

        // Register server registries (Mock registry for demo/fallback)
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
