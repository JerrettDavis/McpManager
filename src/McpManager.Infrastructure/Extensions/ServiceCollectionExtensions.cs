using McpManager.Core.Interfaces;
using McpManager.Application.Services;
using McpManager.Core.Models;
using McpManager.Infrastructure.Connectors;
using McpManager.Infrastructure.Registries;
using McpManager.Infrastructure.Services;
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
        // Version service (singleton - computed once)
        services.AddSingleton<IVersionService, VersionService>();
        
        // Server browse service (scoped - needs database access)
        services.AddScoped<IServerBrowseService, ServerBrowseService>();
        
        // Download stats service (scoped - needs HTTP and database)
        services.AddScoped<IDownloadStatsService, DownloadStatsService>();
        
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

        // Register HttpClients for package registries
        services.AddHttpClient("NpmRegistry", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        
        services.AddHttpClient("PyPIRegistry", client =>
        {
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

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

        // Register mock registry for demo/fallback, wrapped with caching
        services.AddSingleton<IServerRegistry>(sp =>
        {
            var innerRegistry = new MockServerRegistry();
            return new CachedServerRegistry(innerRegistry, sp);
        });

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
        
        try
        {
            // Check if there are any pending migrations
            var pendingMigrations = dbContext.Database.GetPendingMigrations().ToList();
            
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Applying {pendingMigrations.Count} pending migration(s)...");
                dbContext.Database.Migrate();
                Console.WriteLine("Database migrations applied successfully.");
            }
            else
            {
                // No pending migrations, just ensure database exists
                dbContext.Database.EnsureCreated();
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.SqliteErrorCode == 1)
        {
            // SQLite Error 1: Table already exists
            // This means the database schema is already present but migration history may be out of sync
            Console.WriteLine("Warning: Database tables already exist. Verifying migration history...");
            
            try
            {
                // Try to mark all migrations as applied
                var appliedMigrations = dbContext.Database.GetAppliedMigrations().ToList();
                Console.WriteLine($"Database has {appliedMigrations.Count} migration(s) already applied.");
                
                // Database is functional, continue startup
                dbContext.Database.EnsureCreated();
            }
            catch (Exception innerEx)
            {
                Console.Error.WriteLine($"Warning: Could not verify migration history: {innerEx.Message}");
                Console.Error.WriteLine("Database may be in an inconsistent state. Consider deleting the database file to recreate it.");
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't crash the application
            Console.Error.WriteLine($"Error during database initialization: {ex.Message}");
            Console.Error.WriteLine("Attempting to ensure database exists...");
            
            try
            {
                dbContext.Database.EnsureCreated();
                Console.WriteLine("Database created successfully using EnsureCreated fallback.");
            }
            catch (Exception fallbackEx)
            {
                // If this also fails, the application will still start but database features may not work
                Console.Error.WriteLine($"Critical: Could not initialize database: {fallbackEx.Message}");
                Console.Error.WriteLine("Some features may not be available. Please check database permissions and path.");
            }
        }
    }
}
