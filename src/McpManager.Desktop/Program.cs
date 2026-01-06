using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using McpManager.Core.Interfaces;
using McpManager.Application.Services;
using McpManager.Infrastructure.Connectors;
using McpManager.Infrastructure.Registries;
using McpManager.Infrastructure.Persistence;
using McpManager.Infrastructure.Persistence.Repositories;
using McpManager.Infrastructure.BackgroundWorkers;
using Microsoft.EntityFrameworkCore;

namespace McpManager.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // Configure services
        appBuilder.Services.AddLogging();

        // Configure database
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "McpManager",
            "mcpmanager.db");

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        appBuilder.Services.AddDbContext<McpManagerDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Register repositories
        appBuilder.Services.AddScoped<IServerRepository, ServerRepository>();
        appBuilder.Services.AddScoped<IRegistryCacheRepository, RegistryCacheRepository>();

        // Register HttpClient for MCP registries
        appBuilder.Services.AddHttpClient<IServerRegistry, ModelContextProtocolRegistry>(client =>
        {
            client.BaseAddress = new Uri("https://registry.modelcontextprotocol.io/");
            client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
        });

        // Register application services
        appBuilder.Services.AddScoped<IServerManager, ServerManager>();
        appBuilder.Services.AddSingleton<IAgentManager, AgentManager>();
        appBuilder.Services.AddSingleton<IInstallationManager, InstallationManager>();
        appBuilder.Services.AddSingleton<IServerMonitor, ServerMonitor>();
        appBuilder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
        appBuilder.Services.AddSingleton<ConfigurationParser>();

        // Register agent connectors
        appBuilder.Services.AddSingleton<IAgentConnector, ClaudeConnector>();
        appBuilder.Services.AddSingleton<IAgentConnector, CopilotConnector>();
        appBuilder.Services.AddSingleton<IAgentConnector, ClaudeCodeConnector>();
        appBuilder.Services.AddSingleton<IAgentConnector, CodexConnector>();

        // Register server registries
        appBuilder.Services.AddSingleton<IServerRegistry, MockServerRegistry>();

        // Register background workers as regular services (not hosted services for desktop)
        appBuilder.Services.AddSingleton<RegistryRefreshWorker>();

        appBuilder.RootComponents.Add<App>("#app");
        appBuilder.RootComponents.Add<HeadOutlet>("head::after");

        var app = appBuilder.Build();

        // Ensure database is created and migrated
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<McpManagerDbContext>();
            dbContext.Database.Migrate();
        }

        // Configure the Photino window
        app.MainWindow
            .SetTitle("MCP Manager")
            .SetSize(1400, 900)
            .SetResizable(true)
            .SetIconFile("favicon.ico");

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.Run();
    }
}
