using McpManager.Web.Components;
using McpManager.Infrastructure.Extensions;
using McpManager.Infrastructure.BackgroundWorkers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add McpManager services
builder.Services.AddMcpManagerServices();

// Register background workers
builder.Services.AddHostedService<RegistryRefreshWorker>();

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
