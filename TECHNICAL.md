# ScalyTails — Technical Manual

A developer reference for understanding, extending, and maintaining the codebase.

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Tech Stack](#2-tech-stack)
3. [Directory Structure](#3-directory-structure)
4. [Architecture](#4-architecture)
5. [Service Layer](#5-service-layer)
6. [MVVM Pattern](#6-mvvm-pattern)
7. [Navigation System](#7-navigation-system)
8. [Key Patterns and Conventions](#8-key-patterns-and-conventions)
9. [WPF Converters](#9-wpf-converters)
10. [Models Reference](#10-models-reference)
11. [Adding a New Page](#11-adding-a-new-page)
12. [Known Constraints and Gotchas](#12-known-constraints-and-gotchas)
13. [Build and Run](#13-build-and-run)

---

## 1. Project Overview

ScalyTails is a Windows desktop GUI for the Tailscale VPN client. It wraps two separate Tailscale interfaces:

- **Tailscale CLI** (`tailscale.exe`) — for the local node: connection state, peers, exit nodes, subnet routes, SSH, Taildrive, Serve/Funnel, and diagnostics. No authentication required; the CLI uses the local daemon socket.
- **Tailscale REST Management API** (`api.tailscale.com/api/v2`) — for the tailnet admin: devices, users, DNS, ACL policy, network logs, and auth keys. Requires a `tskey-api-` access token.

This split is fundamental to the design. Pages that only need local state (Overview, Peers, Exit Nodes, etc.) work offline with no credentials. Pages that need tailnet-wide admin data (Devices, DNS, Policy, etc.) require an API key configured in Settings.

---

## 2. Tech Stack

| Component | Library / Version |
|---|---|
| UI Framework | WPF (.NET 8, `net8.0-windows`) |
| MVVM helpers | CommunityToolkit.Mvvm 8.3.2 |
| UI theme | MaterialDesignThemes 5.1.0 (MDI icon set) |
| System tray | Hardcodet.NotifyIcon.Wpf 2.0.1 |
| Serialization | System.Text.Json (built-in) |
| HTTP client | System.Net.Http.HttpClient (built-in) |

---

## 3. Directory Structure

```
ScalyTails/
├── App.xaml / App.xaml.cs          Application entry point, service wiring, tray icon
├── MainWindow.xaml / .xaml.cs      Shell window: nav sidebar + content frame
│
├── Models/
│   ├── AppSettings.cs              Persisted user settings (API key, tailnet name)
│   ├── ApiModels.cs                REST API response/request types
│   ├── ApiResult.cs                Generic result wrapper for API calls
│   ├── CliResult.cs                Wrapper for CLI subprocess output
│   ├── NetCheckResult.cs           JSON model for `tailscale netcheck --format json`
│   ├── ServeStatus.cs              JSON model for `tailscale serve status --json`
│   ├── TailscalePrefs.cs           JSON model for `tailscale debug prefs`
│   └── TailscaleStatus.cs          JSON model for `tailscale status --json --peers`
│
├── Services/
│   ├── IAppSettingsService.cs      Interface for persisting app settings
│   ├── AppSettingsService.cs       Reads/writes %APPDATA%\ScalyTails\settings.json
│   ├── IApiKeyAware.cs             Interface for ViewModels that display API key status
│   ├── ITailscaleService.cs        Interface for all CLI operations
│   ├── TailscaleService.cs         Implements CLI ops by spawning tailscale.exe subprocesses
│   ├── ITailscaleApiService.cs     Interface for all REST API operations
│   └── TailscaleApiService.cs      Implements REST API calls via HttpClient
│
├── ViewModels/
│   ├── MainViewModel.cs            Drives Overview, Peers, Exit Nodes, Routes, Serve pages
│   ├── DiagnosticsViewModel.cs     Drives Diagnostics page (netcheck, ping, whois, etc.)
│   ├── SettingsViewModel.cs        Drives Settings page (API key, account switching)
│   ├── DnsViewModel.cs             Drives DNS page
│   ├── DevicesAdminViewModel.cs    Drives Devices admin page
│   ├── UsersViewModel.cs           Drives Users page
│   ├── PolicyViewModel.cs          Drives Policy/ACL editor page
│   ├── LogsViewModel.cs            Drives Network Logs page
│   ├── KeysViewModel.cs            Drives Auth Keys page
│   ├── TailDriveViewModel.cs       Drives TailDrive share management page
│   ├── PeerViewModel.cs            Per-row model for the peers DataGrid
│   ├── ExitNodeViewModel.cs        Per-row model for the exit nodes list
│   ├── ServeEntryViewModel.cs      Per-row model for the serve entries list
│   └── SubnetRouteViewModel.cs     Per-row model for the subnet routes list
│
├── Views/
│   ├── OverviewPage.xaml/.cs       Connection status, quick toggles
│   ├── PeersPage.xaml/.cs          Peer list, SSH, Taildrop file send
│   ├── ExitNodesPage.xaml/.cs      Exit node selector
│   ├── SubnetRoutesPage.xaml/.cs   Advertised routes editor
│   ├── ServePage.xaml/.cs          Serve / Funnel configuration
│   ├── DiagnosticsPage.xaml/.cs    NetCheck, Ping peer, Whois, Update, Bug report
│   ├── TailDrivePage.xaml/.cs      TailDrive share list and management
│   ├── SettingsPage.xaml/.cs       API key input, account switching
│   ├── DnsPage.xaml/.cs            Nameservers, search paths, MagicDNS toggle
│   ├── DevicesAdminPage.xaml/.cs   Device list with authorize/expire/delete
│   ├── UsersPage.xaml/.cs          Tailnet user list
│   ├── PolicyPage.xaml/.cs         ACL policy JSON editor
│   ├── LogsPage.xaml/.cs           Network flow log viewer
│   └── KeysPage.xaml/.cs           Auth key list and creation form
│
└── Converters/
    ├── BoolToVisibilityConverter.cs  bool/int/string → Visibility (with Invert option)
    ├── BoolToColorConverter.cs       bool → Brush (configurable true/false colors)
    ├── BoolToStringConverter.cs      bool → string (configurable true/false labels)
    ├── BoolToIconConverter.cs        bool → PackIconKind (check/close circle)
    ├── InverseBoolConverter.cs       bool → !bool (for IsEnabled bindings)
    └── BytesConverter.cs             long (bytes) → human-readable string (KB/MB/GB)
```

---

## 4. Architecture

```
┌────────────────────���────────────────────────────────────────────┐
│  App.xaml.cs                                                     │
│  Service construction + tray icon                                │
└────────────────────────┬────────────────────────────────────────┘
                         │
           ┌─────────────▼──────────────┐
           │  MainWindow.xaml.cs         │
           │  Nav sidebar + PageHost     │
           └──────┬──────────────┬───────┘
                  │              │
     ┌────────────▼───┐   ┌──────▼─────────────────┐
     │ CLI-backed pages│   │ API-backed pages         │
     │ (MainViewModel) │   │ (own ViewModel per page) │
     └────────┬────────┘   └──────┬──────────────────┘
              │                   │
   ┌──────────▼──────┐   ┌────────▼──────────────┐
   │ ITailscaleService│   │ ITailscaleApiService   │
   │ (CLI subprocess) │   │ (HttpClient REST)      │
   └──────────────────┘   └────────────────────────┘
```

### Dual service design

Pages are split into two groups with different data sources and lifecycles:

**CLI-backed** (no credentials needed, always available if Tailscale is installed):
Overview, Peers, Exit Nodes, Subnet Routes, Serve/Funnel, Diagnostics, TailDrive, Settings (account switching portion)

These all share a single `MainViewModel` which polls `tailscale status`, `tailscale debug prefs`, and `tailscale serve status` every 5 seconds via a background `PeriodicTimer`.

**API-backed** (requires `tskey-api-` access token):
Devices, Users, DNS, Policy, Logs, Keys

Each of these has its own ViewModel that only loads data when the user navigates to the page (on-demand, not on a timer). They all implement `IApiKeyAware` to react to key changes.

---

## 5. Service Layer

### TailscaleService (CLI)

Wraps `tailscale.exe` by spawning subprocesses with `Process`. All operations follow the same pattern:

```csharp
private async Task<CliResult> RunAsync(string arguments, CancellationToken ct = default)
```

Returns a `CliResult` record with `Stdout`, `Stderr`, `ExitCode`, and a computed `Success` property (`ExitCode == 0`).

**Finding tailscale.exe:** `TailscaleService` checks standard install paths (`Program Files`, `Program Files (x86)`) and falls back to PATH resolution. `IsTailscaleInstalled` is false if all lookups fail.

**Netcheck output quirk:** Some Tailscale CLI versions write `netcheck --format json` output to stderr rather than stdout. `DiagnosticsViewModel` checks both and uses whichever is non-empty.

### TailscaleApiService (REST)

Uses a long-lived `HttpClient`. Auth is set **per-request** rather than as a default header so that saving a new API key in Settings takes effect on the very next call without restarting.

```csharp
// HTTP Basic auth: key as username, empty password — matches the official Tailscale Go client
var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.Settings.ApiKey + ":"));
request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
```

**Important:** Tailscale has two distinct key types:
- `tskey-auth-...` — device enrollment keys (used with `tailscale up --auth-key`). Will always return HTTP 401 against the REST API.
- `tskey-api-...` — API access tokens (used with this app). Generated at tailscale.com/admin/settings/keys → "Generate access token".

### ApiResult\<T\>

All GET methods on `ITailscaleApiService` return `ApiResult<T>` instead of `T?`. This carries both the data and the error reason, so ViewModels can surface specific messages (e.g. "HTTP 401 — invalid or expired API key") rather than silently showing empty lists.

```csharp
public class ApiResult<T>
{
    public T?     Data    { get; init; }
    public bool   Success { get; init; }
    public string Error   { get; init; } = "";

    public static ApiResult<T> Ok(T data)         => new() { Data = data, Success = true };
    public static ApiResult<T> Fail(string error)  => new() { Success = false, Error = error };
}
```

Mutation methods (POST/DELETE) return plain `bool` — success or failure is enough.

### AppSettingsService

Persists `AppSettings` as JSON to `%APPDATA%\ScalyTails\settings.json`. Load and Save both silently swallow exceptions — a corrupted settings file falls back to defaults rather than crashing, and a failed write (permissions, full disk) is non-fatal.

---

## 6. MVVM Pattern

The project uses **CommunityToolkit.Mvvm** for source-generated boilerplate.

### Observable properties

```csharp
[ObservableProperty] private string _statusMessage = "";
// Generates: public string StatusMessage { get => ...; set { ... OnPropertyChanged(); } }
```

### Relay commands

```csharp
[RelayCommand]
private async Task RefreshAsync(CancellationToken ct = default) { ... }
// Generates: public IAsyncRelayCommand RefreshCommand { get; }
```

The generated command name is the method name with `Async` stripped and `Command` appended. `PingAsync` → `PingCommand`, `RefreshAsync` → `RefreshCommand`.

### Partial property callbacks

CommunityToolkit generates a `partial void OnXxxChanged(T value)` hook for every `[ObservableProperty]`. Implement it to react to value changes without manually overriding the setter:

```csharp
partial void OnSelectedPeerChanged(TailscalePeer? value)
{
    if (value is not null)
        PingTarget = value.PrimaryIP;
}
```

This is used throughout the project for filter text (DevicesAdminViewModel), API key warnings (SettingsViewModel), dirty tracking (PolicyViewModel), and the peer ping selection (DiagnosticsViewModel).

### IApiKeyAware

ViewModels for API-backed pages implement this interface:

```csharp
public interface IApiKeyAware
{
    void OnApiKeyChanged();
}
```

`MainWindow.NavButton_Click` calls `OnApiKeyChanged()` whenever the user navigates to an API page. Implementations simply raise `PropertyChanged` for `HasApiKey`, which updates the "No API key configured" banner in the XAML without requiring a full data reload.

---

## 7. Navigation System

Navigation is flat — a `RadioButton` sidebar on the left, a `ContentControl` (`PageHost`) in the center.

- All 14 page instances are pre-allocated in `MainWindow` fields. Switching pages just sets `PageHost.Content`.
- Page instances retain their `DataContext` and scroll state for the lifetime of the window.
- No frame history, no URI routing. The `RadioButton.Tag` string is the routing key (see `NavButton_Click` in `MainWindow.xaml.cs`).
- Six of the 14 pages share `MainViewModel` as their `DataContext`. The remaining eight have dedicated ViewModels.

---

## 8. Key Patterns and Conventions

### Background refresh loop

`MainViewModel` polls Tailscale every 5 seconds using `PeriodicTimer`:

```csharp
_refreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
while (await _refreshTimer.WaitForNextTickAsync(ct))
    await RefreshAsync(ct);
```

`PeriodicTimer` skips missed ticks — if a refresh takes longer than 5 seconds it doesn't queue up overlapping calls.

### Dispatcher marshalling

The 5-second refresh fires three parallel CLI calls (`Task.WhenAll`), then applies the results on the UI thread:

```csharp
await Application.Current.Dispatcher.InvokeAsync(() =>
    ApplyStatus(status, prefs, serve));
```

This is required because `ObservableCollection` writes must happen on the WPF dispatcher thread.

### RunBusyAsync helper

All mutating commands in `MainViewModel` go through a single helper that sets `IsBusy`, runs the action, shows errors, and triggers a status refresh:

```csharp
private async Task RunBusyAsync(string message, Func<CancellationToken, Task<CliResult>> action)
```

`IsBusy` disables UI controls via `IsEnabled="{Binding IsBusy, Converter={StaticResource InverseBool}}"`.

### PasswordBox binding

WPF's `PasswordBox` does not support data binding for security reasons. The workaround in `SettingsPage.xaml.cs` wires the `PasswordChanged` event to manually push the value into the ViewModel:

```csharp
private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
{
    if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
        vm.ApiKey = pb.Password;
}
```

### Shutdown mode

`App.xaml` uses `ShutdownMode="OnExplicitShutdown"`. The default `OnMainWindowClose` would kill the process when the window is hidden to tray. `Shutdown()` is only called from the tray Exit menu item.

### SSH terminal cascade

`MainViewModel.SshToPeer` tries to open a terminal in preference order: Windows Terminal (`wt.exe`) → PowerShell → `cmd.exe`. This ensures SSH works regardless of which terminal the user has installed.

---

## 9. WPF Converters

All converters live in `ScalyTails.Converters` and are declared as resources in individual page XAML files.

| Converter | Input | Output | Notes |
|---|---|---|---|
| `BoolToVisibilityConverter` | bool / int / string | Visibility | `Invert="True"` for inverse. Handles `string.Length` bindings. |
| `BoolToColorConverter` | bool | Brush | Configure `TrueColor` / `FalseColor` in XAML. |
| `BoolToStringConverter` | bool | string | Configure `TrueValue` / `FalseValue` in XAML. |
| `BoolToIconConverter` | bool | PackIconKind | CheckCircle vs CloseCircle. |
| `InverseBoolConverter` | bool | bool | Used to enable buttons when `IsBusy` is false. |
| `BytesConverter` | long | string | Formats bytes as B / KB / MB / GB. |

**Important:** `BoolToVisibilityConverter` intentionally handles `int` and `string` values in addition to `bool`. XAML binds to properties like `{Binding SomeText.Length}` which is an `int`, not a `bool`. The pattern `value is true` only matches boolean literals — integers always evaluate to hidden without this handling.

---

## 10. Models Reference

### CLI models

| Model | Source command | Notes |
|---|---|---|
| `TailscaleStatus` | `tailscale status --json --peers` | `AllPeers` guards the null `Peer` dict when disconnected |
| `TailscalePrefs` | `tailscale debug prefs` | Includes route acceptance, SSH, shields-up, exit node prefs |
| `ServeStatus` | `tailscale serve status --json` | `ToEntries()` flattens nested Web/TCP dicts to a bindable list |
| `NetCheckResult` | `tailscale netcheck --format json` | `RegionLatency` values are nanoseconds (Go `time.Duration`); `SortedDerpLatencies()` converts to ms |
| `CliResult` | All subprocess calls | Record: `Stdout`, `Stderr`, `ExitCode`. `Success = ExitCode == 0`. `Output` returns whichever stream is non-empty. |

### API models (ApiModels.cs)

All classes map directly to the [Tailscale REST API v2](https://tailscale.com/api) JSON schema. Key computed properties:

- `ApiDevice.IsExpiringSoon` — true when key expires within 14 days
- `ApiDevice.TagsDisplay` — strips mandatory `tag:` prefix for cleaner UI display
- `ApiAuthKey.IsActive` — false if revoked, invalid, or past expiry date
- `CreateKeyRequest.ExpirySeconds` — defaults to 7,776,000 (90 days), the API maximum

---

## 11. Adding a New Page

Follow this checklist to add a page end-to-end.

### Step 1 — Create the ViewModel

For a CLI-backed page with no API dependency, add a class in `ViewModels/`:

```csharp
public partial class MyFeatureViewModel : ObservableObject
{
    private readonly ITailscaleService _tailscale;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    public MyFeatureViewModel(ITailscaleService tailscale)
    {
        _tailscale = tailscale;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try { /* ... */ }
        finally { IsBusy = false; }
    }
}
```

For an API-backed page, depend on `ITailscaleApiService` and implement `IApiKeyAware`:

```csharp
public partial class MyFeatureViewModel : ObservableObject, IApiKeyAware
{
    private readonly ITailscaleApiService _api;

    public bool HasApiKey => _api.IsConfigured;
    public void OnApiKeyChanged() => OnPropertyChanged(nameof(HasApiKey));

    // ...
}
```

### Step 2 — Create the View

Add `Views/MyFeaturePage.xaml` (WPF UserControl) and its `.xaml.cs` code-behind. The code-behind for most pages is a single line:

```csharp
public partial class MyFeaturePage : UserControl
{
    public MyFeaturePage() => InitializeComponent();
}
```

### Step 3 — Wire up in MainWindow

In `MainWindow.xaml.cs`:

```csharp
// Field declaration
private readonly MyFeaturePage _myFeaturePage = new();

// In constructor
var myFeatureVm = new MyFeatureViewModel(cliService);  // or apiService
_myFeaturePage.DataContext = myFeatureVm;

// In NavButton_Click switch
"MyFeature" => _myFeaturePage,
```

### Step 4 — Add the nav button

In `MainWindow.xaml`, add a `RadioButton` to the appropriate nav section (CLI pages at top, API pages in "ADMIN" section):

```xml
<RadioButton Tag="MyFeature" GroupName="Nav"
             Style="{StaticResource NavButton}"
             Click="NavButton_Click">
    <StackPanel Orientation="Horizontal">
        <materialDesign:PackIcon Kind="SomeIcon" Width="18" Height="18" .../>
        <TextBlock Text="My Feature" .../>
    </StackPanel>
</RadioButton>
```

**Icon note:** Use only icon names from the [MDI icon set](https://pictogrammers.com/library/mdi/). Names like `PackIconKind.NetworkCheck` that exist in other icon families will throw a `XamlParseException` at runtime.

---

## 12. Known Constraints and Gotchas

### MaterialDesign icon names
Only MDI icons are bundled. If an icon name is wrong, the app crashes at startup with `XamlParseException: {"Cannot create instance of 'PackIconKind'..."}`. Always verify icon names at pictogrammers.com/library/mdi before using them.

### PasswordBox data binding
`PasswordBox` does not support `{Binding}` for its `Password` property. Use the `PasswordChanged` event in code-behind to push the value into the ViewModel (see `SettingsPage.xaml.cs`).

### Tailscale key types
Two distinct key types exist:
- `tskey-auth-...` — device auth keys (for enrolling devices, NOT for this app)
- `tskey-api-...` — API access tokens (required for all admin pages)

Using an auth key returns HTTP 401 with `{"message":"API token invalid"}` on every API call. The Settings page shows a red warning when it detects the `tskey-auth-` prefix.

### ObservableCollection thread safety
`ObservableCollection` raises `CollectionChanged` on the thread that modifies it. WPF will throw if this happens off the dispatcher thread. Always marshal collection writes via `Application.Current.Dispatcher.InvokeAsync(...)` when coming from a background task.

### IsBusy vs IsPinging
`MainViewModel.IsBusy` disables most controls. `DiagnosticsViewModel.IsPinging` is a separate flag used only for the Ping button, because the main `IsBusy` would block unrelated controls on the same page while a ping is in flight.

### PeriodicTimer and cancellation
`RefreshLoopAsync` catches `OperationCanceledException` and exits silently — this is the normal shutdown path when `StopRefresh()` cancels the token. Do not rethrow it.

### Settings persistence path
Settings are stored in `%APPDATA%\ScalyTails\settings.json` (e.g. `C:\Users\<user>\AppData\Roaming\ScalyTails\settings.json`). They survive app reinstalls. Deleting this file resets the app to a clean state.

### Network flow logs
The Logs page requires the "Network flow logging" feature to be enabled in the Tailscale admin console (tailscale.com/admin/logs) and may require a paid plan. HTTP 403/404 from this endpoint is expected on free plans.

---

## 13. Build and Run

### Prerequisites

- .NET 8 SDK
- Tailscale for Windows installed (the app is functional without it, but all CLI features are disabled)

### Run in development

```
dotnet run
```

### Publish self-contained executable

```powershell
dotnet publish --configuration Release --runtime win-x64 --self-contained true `
    --output publish/
```

### Build installer (requires Inno Setup 6)

```powershell
.\build-installer.ps1
```

Produces `dist\ScalyTailsSetup.exe`. The installer places the app in `%ProgramFiles%\ScalyTails` and creates Start Menu shortcuts.

### Stopping a running instance before rebuild

If the app is running in the tray and you try to build, MSBuild will fail with MSB3027 (file locked). Kill the process first:

```powershell
Stop-Process -Name ScalyTails -Force -ErrorAction SilentlyContinue
```
