using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Photino.Blazor;
using McpManager.Infrastructure.Extensions;
using McpManager.Infrastructure.BackgroundWorkers;

namespace McpManager.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        // Configure services
        appBuilder.Services.AddLogging();

        // Add McpManager services
        appBuilder.Services.AddMcpManagerServices("McpManagerDesktop");

        // Register background workers (though Photino doesn't have IHostedService support)
        appBuilder.Services.AddSingleton<RegistryRefreshWorker>();
        appBuilder.Services.AddSingleton<AgentServerSyncWorker>();
        appBuilder.Services.AddSingleton<DownloadStatsWorker>();
        appBuilder.Services.AddSingleton<ConfigurationWatcherWorker>();

        appBuilder.RootComponents.Add<App>("#app");
        appBuilder.RootComponents.Add<HeadOutlet>("head::after");

        var app = appBuilder.Build();

        // Ensure database is created and migrated
        app.Services.EnsureDatabaseCreated();

        // Start background workers manually since Photino doesn't support IHostedService
        var cancellationTokenSource = new System.Threading.CancellationTokenSource();
        StartBackgroundWorkers(app.Services, cancellationTokenSource.Token);

        // Configure the Photino window
        app.MainWindow
            .SetTitle("MCP Manager")
            .SetSize(1400, 900)
            .SetResizable(true)
            .SetUseOsDefaultLocation(false)
            .SetLeft(100)
            .SetTop(100);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            cancellationTokenSource.Cancel();
        };

        app.Run();
    }

    private static void StartBackgroundWorkers(IServiceProvider services, System.Threading.CancellationToken cancellationToken)
    {
        // Start Registry Refresh Worker
        var registryWorker = services.GetRequiredService<RegistryRefreshWorker>();
        _ = Task.Run(async () =>
        {
            try
            {
                await registryWorker.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry refresh worker error: {ex.Message}");
            }
        }, cancellationToken);

        // Start Agent Server Sync Worker (this auto-installs servers from agents)
        var agentSyncWorker = services.GetRequiredService<AgentServerSyncWorker>();
        _ = Task.Run(async () =>
        {
            try
            {
                await agentSyncWorker.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Agent sync worker error: {ex.Message}");
            }
        }, cancellationToken);

        // Start Download Stats Worker
        var downloadStatsWorker = services.GetRequiredService<DownloadStatsWorker>();
        _ = Task.Run(async () =>
        {
            try
            {
                await downloadStatsWorker.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download stats worker error: {ex.Message}");
            }
        }, cancellationToken);

        // Start Configuration Watcher Worker
        var configWatcherWorker = services.GetRequiredService<ConfigurationWatcherWorker>();
        _ = Task.Run(async () =>
        {
            try
            {
                await configWatcherWorker.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Configuration watcher worker error: {ex.Message}");
            }
        }, cancellationToken);
    }
}
