using McpManager.Web.Components;
using McpManager.Core.Interfaces;
using McpManager.Application.Services;
using McpManager.Infrastructure.Connectors;
using McpManager.Infrastructure.Registries;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register application services (Singleton for in-memory state)
builder.Services.AddSingleton<IServerManager, ServerManager>();
builder.Services.AddSingleton<IAgentManager, AgentManager>();
builder.Services.AddSingleton<IInstallationManager, InstallationManager>();
builder.Services.AddSingleton<IServerMonitor, ServerMonitor>();

// Register agent connectors
builder.Services.AddSingleton<IAgentConnector, ClaudeConnector>();
builder.Services.AddSingleton<IAgentConnector, CopilotConnector>();

// Register server registries
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
