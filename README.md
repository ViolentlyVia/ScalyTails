# ScalyTails

A full-featured Windows GUI for [Tailscale](https://tailscale.com), built with WPF and .NET 8. ScalyTails wraps the Tailscale CLI and REST API into a polished native application with a Material Design interface and system-tray integration.

---

## Features

### CLI-backed pages (no API key required)

| Page | Description |
| --- | --- |
| **Overview** | Connection status, your Tailnet name, local IPs, client version. Connect/disconnect toggle. |
| **Peers** | All devices on your network sorted by online status. One-click SSH and Taildrop file send. |
| **Exit Nodes** | Browse and activate available exit nodes; toggle LAN access while routed. |
| **Subnet Routes** | Add/remove advertised CIDR routes and toggle route acceptance from other devices. |
| **Serve & Funnel** | Expose local services over HTTP, HTTPS, or public Funnel — with a live serve config table. |
| **Diagnostics** | Run `netcheck` (UDP, IPv4/IPv6, NAT type, DERP latencies), ping, whois, bug report, and update check. |
| **Taildrive** | Manage Taildrive shares: browse for a local folder, set a share name, and list active shares. |
| **Settings** | Save your Tailscale API key and tailnet name. Switch between multiple logged-in accounts. |

### API-backed pages (require a Tailscale API key)

| Page | Description |
| --- | --- |
| **DNS** | View and edit tailnet nameservers, search paths, and toggle MagicDNS. |
| **Devices (Admin)** | Full device list with filter, per-device authorize/expire key/delete actions. |
| **Users** | Tailnet member roster with online indicator, role, status, and device count. |
| **Policy** | View and edit your tailnet ACL/policy file in a built-in JSON editor with save/revert. |
| **Logs** | Tail network logs for the last 1–168 hours with source and destination columns. |
| **Keys** | Create reusable/ephemeral/preauthorized auth keys, copy the key value, and revoke existing keys. |

### Other

- **System tray**: minimize to tray, double-click to restore, quick connect/disconnect from the tray menu.
- **Auto-refresh**: status, peers, exit nodes, routes, and serve config refresh every 5 seconds automatically.

---

## Prerequisites

- **Windows 10/11**
- **[Tailscale for Windows](https://tailscale.com/download/windows)** installed (ScalyTails searches `Program Files` and falls back to `PATH`)
- **[.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)**

---

## Build

```sh
git clone https://github.com/ViolentlyVia/ScalyTails
cd ScalyTails
dotnet build
dotnet run
```

Or open `ScalyTails.slnx` in Visual Studio 2022+ / Rider and run from there.

---

## API key setup

Several pages (DNS, Devices, Users, Policy, Logs, Keys) call the [Tailscale REST API](https://tailscale.com/api) and require an API key:

1. Go to **[tailscale.com/admin/settings/keys](https://login.tailscale.com/admin/settings/keys)** and generate a personal API key.
2. Open ScalyTails → **Settings**.
3. Paste the key into the **API Key** field.
4. Set **Tailnet** to your tailnet name (e.g. `example.com`) — leave it as `-` to use the default tailnet for the authenticated key.
5. Click **Save**.

---

## Architecture

```text
ScalyTails/
├── Models/           # JSON-deserialization models for CLI output and REST API responses
├── Services/
│   ├── TailscaleService.cs      # Runs tailscale.exe subprocesses, returns CliResult
│   ├── TailscaleApiService.cs   # HTTP client wrapper for api.tailscale.com/api/v2
│   ├── AppSettingsService.cs    # Persists API key + tailnet to %APPDATA%\ScalyTails\settings.json
│   └── IApiKeyAware.cs          # Interface for pages that need to react to API key changes
├── ViewModels/       # CommunityToolkit.Mvvm ObservableObject + RelayCommand per page
├── Views/            # WPF UserControl pages (.xaml + .xaml.cs)
├── Converters/       # IValueConverter implementations for the UI
├── App.xaml.cs       # Startup: wires services, creates MainWindow, sets up tray icon
└── MainWindow.xaml.cs  # Shell: nav sidebar routing, page host, minimize-to-tray
```

All long-running operations are `async`/`await`. The main view model polls Tailscale status every 5 seconds using `PeriodicTimer` (which skips missed ticks, so a slow response won't queue up overlapping refreshes). Admin page view models are lazy — they only load when the user first opens the page and clicks **Load** (or whenever they navigate to the page after entering an API key).

---

## Tech stack

| Package | Version | Purpose |
| --- | --- | --- |
| .NET 8.0 (WPF) | 8.0 | Framework |
| CommunityToolkit.Mvvm | 8.3.2 | MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`) |
| MaterialDesignThemes | 5.1.0 | UI controls and icons (MDI icon set) |
| Hardcodet.NotifyIcon.Wpf | 2.0.1 | System tray icon and context menu |

---

## License

ScalyTails is free software released under the **[GNU General Public License v3.0](LICENSE)**.

You are free to use, copy, modify, and distribute this software under the terms of the GPL v3. See the [LICENSE](LICENSE) file for the full license text.
