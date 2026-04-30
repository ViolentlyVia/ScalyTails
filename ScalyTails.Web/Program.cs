using MudBlazor.Services;
using ScalyTails.Web.Components;
using ScalyTails.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Bind HTTP only — no HTTPS. Tailscale encrypts traffic over the Tailnet,
// and the dev cert is untrusted by other machines anyway.
builder.WebHost.UseUrls("http://0.0.0.0:5169");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Core Tailscale services — singletons so the CLI process pool and HTTP client are shared
builder.Services.AddSingleton<ITailscaleService, TailscaleService>();
builder.Services.AddSingleton<ITailscaleApiService, TailscaleApiService>();
builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
