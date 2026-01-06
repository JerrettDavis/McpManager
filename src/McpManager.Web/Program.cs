using McpManager.Web.Components;
using McpManager.Core.Interfaces;
using McpManager.Application.Services;
using McpManager.Infrastructure.Connectors;
using McpManager.Infrastructure.Registries;
using McpManager.Infrastructure.Persistence;
using McpManager.Infrastructure.Persistence.Repositories;
using McpManager.Infrastructure.BackgroundWorkers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure database
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "McpManager",
    "mcpmanager.db");

// Ensure directory exists
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<McpManagerDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Register repositories
builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IRegistryCacheRepository, RegistryCacheRepository>();

// Register HttpClient for MCP registries
builder.Services.AddHttpClient<IServerRegistry, ModelContextProtocolRegistry>(client =>
{
    client.BaseAddress = new Uri("https://registry.modelcontextprotocol.io/");
    client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
});

// Register application services
builder.Services.AddScoped<IServerManager, ServerManager>();
builder.Services.AddSingleton<IAgentManager, AgentManager>();
builder.Services.AddSingleton<IInstallationManager, InstallationManager>();
builder.Services.AddSingleton<IServerMonitor, ServerMonitor>();
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
builder.Services.AddSingleton<ConfigurationParser>();

// Register agent connectors
builder.Services.AddSingleton<IAgentConnector, ClaudeConnector>();
builder.Services.AddSingleton<IAgentConnector, CopilotConnector>();
builder.Services.AddSingleton<IAgentConnector, ClaudeCodeConnector>();
builder.Services.AddSingleton<IAgentConnector, CodexConnector>();

// Register server registries (Mock registry for demo/fallback)
builder.Services.AddSingleton<IServerRegistry, MockServerRegistry>();

// Register background workers
builder.Services.AddHostedService<RegistryRefreshWorker>();

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<McpManagerDbContext>();
    dbContext.Database.Migrate();
}

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
