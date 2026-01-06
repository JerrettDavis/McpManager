using McpManager.Web.Components;
using McpManager.Core.Interfaces;
using McpManager.Application.Services;
using McpManager.Infrastructure.Connectors;
using McpManager.Infrastructure.Registries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register HttpClient for MCP registries
builder.Services.AddHttpClient<IServerRegistry, McpGetRegistry>(client =>
{
    client.BaseAddress = new Uri("https://mcp-get.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "McpManager/1.0");
});

// Register application services (Singleton for in-memory state)
builder.Services.AddSingleton<IServerManager, ServerManager>();
builder.Services.AddSingleton<IAgentManager, AgentManager>();
builder.Services.AddSingleton<IInstallationManager, InstallationManager>();
builder.Services.AddSingleton<IServerMonitor, ServerMonitor>();

// Register agent connectors
builder.Services.AddSingleton<IAgentConnector, ClaudeConnector>();
builder.Services.AddSingleton<IAgentConnector, CopilotConnector>();
builder.Services.AddSingleton<IAgentConnector, ClaudeCodeConnector>();
builder.Services.AddSingleton<IAgentConnector, CodexConnector>();

// Register server registries (keep mock as fallback)
builder.Services.AddSingleton<IServerRegistry, MockServerRegistry>();

var app = builder.Build();

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
