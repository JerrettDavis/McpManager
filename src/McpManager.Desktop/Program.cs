using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using McpManager.Infrastructure.Extensions;

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

        appBuilder.RootComponents.Add<App>("#app");
        appBuilder.RootComponents.Add<HeadOutlet>("head::after");

        var app = appBuilder.Build();

        // Ensure database is created and migrated
        app.Services.EnsureDatabaseCreated();

        // Configure the Photino window
        app.MainWindow
            .SetTitle("MCP Manager")
            .SetSize(1400, 900)
            .SetResizable(true);

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.Run();
    }
}
