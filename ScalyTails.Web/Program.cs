using MudBlazor.Services;
using ScalyTails.Web.Components;
using ScalyTails.Web.Services;

var builder = WebApplication.CreateBuilder(args);

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
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
