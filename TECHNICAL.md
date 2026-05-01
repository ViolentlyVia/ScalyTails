# ScalyTails — Technical Reference

Developer reference for understanding, extending, and building the codebase.

---

## Table of Contents

1. [Tech Stack](#1-tech-stack)
2. [Project Structure](#2-project-structure)
3. [Architecture](#3-architecture)
4. [Service Layer](#4-service-layer)
5. [Models Reference](#5-models-reference)
6. [Settings Persistence](#6-settings-persistence)
7. [Basic / Advanced Mode](#7-basic--advanced-mode)
8. [Adding a New Page](#8-adding-a-new-page)
9. [Known Constraints and Gotchas](#9-known-constraints-and-gotchas)
10. [Build and Publish](#10-build-and-publish)

---

## 1. Tech Stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET 10 |
| Web framework | Blazor Server (Interactive Server render mode) |
| UI components | MudBlazor 9.x |
| CLI integration | `System.Diagnostics.Process` wrapping `tailscale.exe` |
| API integration | `System.Net.Http.HttpClient` against `api.tailscale.com/api/v2` |
| Settings storage | JSON file via `System.Text.Json` |
| Hosting | Kestrel on `http://0.0.0.0:5169` |

Blazor Server is used rather than Blazor WebAssembly because the app needs direct access to the host machine's `tailscale` CLI binary and the local filesystem, which WASM cannot provide.

---

## 2. Project Structure

```
ScalyTails/
├── ScalyTails.Web/
│   ├── Components/
│   │   ├── App.razor              — root component, sets render mode
│   │   ├── Routes.razor           — router
│   │   ├── _Imports.razor         — global using directives for all .razor files
│   │   ├── Layout/
│   │   │   ├── MainLayout.razor   — shell: sidebar + content area
│   │   │   └── NavMenu.razor      — mode-aware navigation sidebar
│   │   └── Pages/
│   │       ├── Overview.razor
│   │       ├── Peers.razor
│   │       ├── ExitNodes.razor
│   │       ├── SubnetRoutes.razor
│   │       ├── Serve.razor
│   │       ├── Diagnostics.razor
│   │       ├── TailDrive.razor
│   │       ├── Settings.razor
│   │       ├── Dns.razor
│   │       ├── DevicesAdmin.razor
│   │       ├── Users.razor
│   │       ├── Policy.razor
│   │       ├── Logs.razor
│   │       └── Keys.razor
│   ├── Models/
│   │   ├── TailscaleStatus.cs     — CLI status JSON shape
│   │   ├── TailscalePrefs.cs      — CLI prefs JSON shape
│   │   ├── ServeStatus.cs         — CLI serve status JSON shape
│   │   ├── NetCheckResult.cs      — CLI netcheck JSON shape
│   │   ├── ApiModels.cs           — REST API response models
│   │   ├── ApiResult.cs           — generic API result wrapper
│   │   ├── CliResult.cs           — CLI process result wrapper
│   │   └── AppSettings.cs         — persisted user settings
│   ├── Services/
│   │   ├── ITailscaleService.cs   — interface: CLI operations
│   │   ├── TailscaleService.cs    — implementation: spawns tailscale.exe
│   │   ├── ITailscaleApiService.cs — interface: REST API operations
│   │   ├── TailscaleApiService.cs  — implementation: HttpClient
│   │   ├── IAppSettingsService.cs  — interface: settings + Changed event
│   │   ├── AppSettingsService.cs   — implementation: JSON persistence
│   │   └── IApiKeyAware.cs        — marker interface (unused; reserved)
│   ├── wwwroot/                   — static assets
│   ├── Program.cs                 — DI setup, Kestrel config, middleware
│   └── ScalyTails.Web.csproj
├── .vscode/
│   ├── launch.json                — VS Code debug config (Blazor)
│   └── tasks.json                 — build + watch tasks
├── README.md
├── TECHNICAL.md
└── ScalyTails.slnx
```

---

## 3. Architecture

```
Browser  ←—— WebSocket (SignalR) ——→  Blazor Server
                                           │
                           ┌───────────────┼───────────────────┐
                           ▼               ▼                   ▼
                   TailscaleService  TailscaleApiService  AppSettingsService
                           │               │                   │
                    tailscale.exe    api.tailscale.com    settings.json
                    (local CLI)      (REST, HTTPS)        (local disk)
```

All three services are registered as **singletons** in `Program.cs`. This means a single `HttpClient` and a single settings instance are shared across all concurrent Blazor circuits (browser tabs). This is intentional — settings mutations from one tab are immediately visible to others.

Blazor Server uses a persistent SignalR connection between the browser and the server. UI events (button clicks, form input) travel over that connection and are handled server-side, which is why direct CLI and filesystem access work without any additional API layer.

---

## 4. Service Layer

### ITailscaleService / TailscaleService

Wraps the `tailscale` CLI binary. Every method spawns a short-lived process, captures stdout/stderr, and returns a `CliResult`.

**CLI discovery:** Checks `C:\Program Files\Tailscale\tailscale.exe`, then `C:\Program Files (x86)\Tailscale\tailscale.exe`, then falls back to `tailscale` on `PATH`.

**Key methods:**

```csharp
Task<TailscaleStatus?> GetStatusAsync()      // tailscale status --json
Task<TailscalePrefs?>  GetPrefsAsync()       // tailscale prefs --json (undocumented)
Task<CliResult>        ConnectAsync()        // tailscale up
Task<CliResult>        DisconnectAsync()     // tailscale down
Task<CliResult>        SetExitNodeAsync(string? nodeIP, bool allowLan)
Task<CliResult>        AdvertiseRoutesAsync(IEnumerable<string> routes)
Task<CliResult>        NetCheckAsync()       // tailscale netcheck --format=json
Task<CliResult>        PingAsync(string host, int count = 3)
Task<CliResult>        WhoisAsync(string ip)
Task<ServeStatus?>     GetServeStatusAsync() // tailscale serve status --json
Task<CliResult>        AddServeAsync(string protocol, int port, string target)
Task<CliResult>        DriveShareAsync(string name, string path)
Task<CliResult>        SwitchAccountAsync(string account)
```

**CliResult:**

```csharp
public record CliResult(bool Success, string Stdout, string Stderr);
```

`Success` is true when the process exits with code 0.

### ITailscaleApiService / TailscaleApiService

Wraps the [Tailscale REST API](https://tailscale.com/api) (`api.tailscale.com/api/v2`). Uses Bearer token auth with the API key from settings. The tailnet name (also from settings, defaulting to `"-"` which means the authenticated tailnet) is embedded in request URLs.

**IsConfigured** returns `true` when both `ApiKey` and `Tailnet` are non-empty in settings. Pages gate their Load calls behind this check and show a warning alert when it is false.

**ApiResult wrapper:**

```csharp
public class ApiResult<T>
{
    public bool    Success { get; init; }
    public T?      Data    { get; init; }
    public string? Error   { get; init; }
}
```

### IAppSettingsService / AppSettingsService

Loads and saves `AppSettings` as JSON. Also fires a `Changed` event whenever settings are saved, which `NavMenu` subscribes to in order to re-render without a page reload.

**Storage path:**
- Windows: `%APPDATA%\ScalyTails\settings.json`
- Linux: `~/.config/ScalyTails/settings.json`

**Interface:**

```csharp
public interface IAppSettingsService
{
    AppSettings Settings { get; }
    void Save();
    event Action? Changed;
}
```

**Reactive NavMenu pattern:**

```csharp
// NavMenu.razor
@implements IDisposable
@inject IAppSettingsService AppSettings

@code {
    protected override void OnInitialized()
        => AppSettings.Changed += OnSettingsChanged;

    private void OnSettingsChanged()
        => InvokeAsync(StateHasChanged);

    public void Dispose()
        => AppSettings.Changed -= OnSettingsChanged;
}
```

---

## 5. Models Reference

### AppSettings

```csharp
public class AppSettings
{
    public string ApiKey      { get; set; } = "";
    public string Tailnet     { get; set; } = "-";  // "-" = authenticated tailnet
    public bool   AdvancedMode { get; set; } = false;
}
```

### TailscaleStatus

Deserialised from `tailscale status --json`. Key computed properties:

```csharp
bool IsRunning    // BackendState is "Running" or "Starting"
bool NeedsLogin   // BackendState == "NeedsLogin"
IEnumerable<TailscalePeer> AllPeers   // flattens the Peer dictionary
TailscalePeer? ActiveExitNode         // peer where ExitNode == true
```

### TailscalePeer

Notable computed properties:

```csharp
string PrimaryIP      // first entry in TailscaleIPs
string DisplayName    // HostName, falling back to trimmed DNSName
List<string> SubnetRoutes  // AllowedIPs filtered to exclude /32 and /128
```

`SubnetRoutes` is derived rather than directly from the JSON because `tailscale status --json` does not emit an `AdvertisedRoutes` field for peers. Instead, `AllowedIPs` contains both the peer's own Tailscale IPs (as `/32`/`/128`) and any subnet routes it is currently routing — filtering out the host routes leaves the subnets.

### CliResult

```csharp
public record CliResult(bool Success, string Stdout, string Stderr);
```

### ApiResult\<T\>

```csharp
public class ApiResult<T>
{
    public bool    Success { get; init; }
    public T?      Data    { get; init; }
    public string? Error   { get; init; }
}
```

---

## 6. Settings Persistence

`AppSettingsService` reads the JSON file on construction and holds the deserialized `AppSettings` object in memory for the lifetime of the process. `Save()` serializes it back to disk and fires `Changed`.

Because it is a singleton, all Blazor circuits share the same in-memory settings. There is no locking — concurrent `Save()` calls from multiple browser tabs could theoretically race, but in practice this does not occur because settings mutations are user-driven one at a time.

---

## 7. Basic / Advanced Mode

`AppSettings.AdvancedMode` (default `false`) controls:

- **NavMenu.razor** — renders different link sets depending on the mode. Subscribes to `IAppSettingsService.Changed` so it updates instantly without navigation.
- **Page titles and headings** — each affected page reads `AppSettings.Settings.AdvancedMode` to pick between technical and friendly names. Example:

```csharp
// SubnetRoutes.razor
private string _pageTitle =>
    AppSettings.Settings.AdvancedMode ? "Subnet Routes" : "Network Sharing";
```

- **Visibility** — API-backed pages (`/dns`, `/devices`, `/users`, `/policy`, `/logs`, `/keys`) are only linked from NavMenu in Advanced mode. Their routes still exist and respond; they are simply not surfaced in Basic mode.

---

## 8. Adding a New Page

1. **Create the Razor component** in `ScalyTails.Web/Components/Pages/MyPage.razor`:

```razor
@page "/my-page"
@inject ITailscaleService Tailscale
@inject IAppSettingsService AppSettings

<PageTitle>My Page — ScalyTails</PageTitle>

<MudStack Spacing="4">
    <MudStack Row="true" AlignItems="AlignItems.Center" Spacing="2">
        <MudIcon Icon="@Icons.Material.Filled.SomeIcon" Color="Color.Primary" />
        <MudText Typo="Typo.h5">My Page</MudText>
        <MudTooltip Text="Tailscale Documentation">
            <MudIconButton Icon="@Icons.Material.Filled.HelpOutline"
                           Href="https://tailscale.com/kb/XXXX/my-feature/"
                           Target="_blank" Size="Size.Small" Color="Color.Default" />
        </MudTooltip>
    </MudStack>
    <!-- content -->
</MudStack>

@code {
    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync() { /* ... */ }
}
```

2. **Add a nav link** in `NavMenu.razor` under the appropriate section (LOCAL or ADMIN):

```razor
<MudNavLink Href="/my-page" Icon="@Icons.Material.Filled.SomeIcon">
    My Page
</MudNavLink>
```

3. If the page needs an API key, gate the load call:

```csharp
protected override async Task OnInitializedAsync()
{
    if (Api.IsConfigured) await LoadAsync();
}
```

And add a warning alert in the markup:

```razor
@if (!Api.IsConfigured)
{
    <MudAlert Severity="Severity.Warning">
        API key required. Go to <MudLink Href="/settings">Settings</MudLink> to add one.
    </MudAlert>
}
```

---

## 9. Known Constraints and Gotchas

### File encoding

Some Razor files in this project have historically had UTF-8 mojibake — characters like `…` (U+2026), `—` (U+2014), `✓` (U+2713), and `✗` (U+2717) stored as multi-byte Windows-1252 sequences inside a UTF-8 file. When editing these files, always use an editor that reads and writes UTF-8 without BOM. Avoid tools that silently re-encode.

If you see garbled characters like `â€¦` or `âœ"` in the running app, the source file has been double-encoded. Fix with a script that reads as UTF-8 and replaces the mojibake codepoint sequences with the correct Unicode characters.

### TailscalePeer.SubnetRoutes derivation

`tailscale status --json` does not include an `AdvertisedRoutes` field for peers. Subnet routes must be derived from `AllowedIPs` by filtering out `/32` (IPv4) and `/128` (IPv6) host entries. See `TailscaleStatus.cs`.

### Blazor Server and long-running CLI calls

`TailscaleService` methods spawn a new process per call. Long-running commands (ping, update apply) block the async call for their duration. There is no cancellation plumbed through yet — if a user navigates away, the process runs to completion in the background before the result is discarded.

### Port and binding

The app binds to `http://0.0.0.0:5169` (all interfaces, not just localhost). This is intentional so other devices on the Tailnet can reach the dashboard. Do not expose port 5169 on untrusted networks — Tailscale's network-level access controls are expected to gate access.

### Tailnet name default

The API tailnet name defaults to `"-"`, which the Tailscale API interprets as the tailnet of the authenticated key. Users only need to change this if they manage multiple tailnets with a single key.

### MudBlazor switch binding

Do not use `@bind-Value` and `ValueChanged` together on `MudSwitch` — MudBlazor does not support both simultaneously. Use `Value` (one-way) with `ValueChanged` only.

---

## 10. Build and Publish

### Development

```sh
cd ScalyTails.Web
dotnet run
```

Or use `dotnet watch` for hot reload:

```sh
dotnet watch --project ScalyTails.Web/ScalyTails.Web.csproj
```

### Release — Windows (self-contained, single file)

```powershell
dotnet publish ScalyTails.Web/ScalyTails.Web.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output ./release-output
```

### Release — Linux (self-contained, single file)

```sh
dotnet publish ScalyTails.Web/ScalyTails.Web.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    --output ./release-output
```

### Debian package

Build the Linux binary first, then use `dpkg-deb` to package it. The package installs files to `/usr/share/scalytails-web/`, creates a `scalytails` system user, and registers a systemd service (`scalytails-web.service`). See the `postinst` and `prerm` scripts embedded in the release workflow for the full setup/teardown logic.

### Version bump

Update `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` in `ScalyTails.Web/ScalyTails.Web.csproj`, then tag the commit:

```sh
git tag v0.x.0
git push origin v0.x.0
```
