using McpManager.Web.Components;
using McpManager.Infrastructure.Extensions;
using McpManager.Infrastructure.BackgroundWorkers;

var builder = WebApplication.CreateBuilder(args);

// Check for --reset-db argument
var resetDb = args.Contains("--reset-db", StringComparer.OrdinalIgnoreCase);
if (resetDb)
{
    var dbPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "McpManager",
        "mcpmanager.db");
    
    if (File.Exists(dbPath))
    {
        Console.WriteLine($"Deleting existing database at: {dbPath}");
        File.Delete(dbPath);
        Console.WriteLine("Database deleted successfully.");
    }
    else
    {
        Console.WriteLine("No existing database found to delete.");
    }
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add McpManager services
builder.Services.AddMcpManagerServices();

// Register background workers
builder.Services.AddHostedService<RegistryRefreshWorker>();
builder.Services.AddHostedService<AgentServerSyncWorker>();
builder.Services.AddHostedService<DownloadStatsWorker>();

var app = builder.Build();

// Ensure database is created and migrated
app.Services.EnsureDatabaseCreated();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
