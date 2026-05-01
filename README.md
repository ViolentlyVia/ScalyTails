# ScalyTails Web

A full-featured web interface for [Tailscale](https://tailscale.com), built with Blazor Server and .NET 10. ScalyTails wraps the Tailscale CLI and REST API into a polished web application with a Material Design interface (via MudBlazor) that can be accessed securely over your Tailnet.

---

## Features

### Basic and Advanced modes

ScalyTails supports two display modes, switchable from **Settings**:

- **Basic mode** (default) — shows only local pages with plain, user-friendly names. No API key required.
- **Advanced mode** — shows all pages including API-backed admin tools, with technical names.

Page data loads automatically when you navigate to a page. Every page includes a link to the relevant Tailscale documentation.

### CLI-backed pages (no API key required)

| Page | Basic name | Description |
| --- | --- | --- |
| **Overview** | Dashboard | Connection status, your Tailnet name, local IPs, client version. Connect/disconnect toggle. |
| **Peers** | My Network | All devices on your network sorted by online status. One-click SSH and Taildrop file send. |
| **Exit Nodes** | Exit Nodes | Browse and activate available exit nodes; toggle LAN access while routed. |
| **Subnet Routes** | Network Sharing | Add/remove advertised CIDR routes and toggle route acceptance from other devices. |
| **Serve & Funnel** | Share Services | Expose local services over HTTP, HTTPS, or public Funnel — with a live serve config table. |
| **Diagnostics** | Troubleshooting | Run `netcheck` (UDP, IPv4/IPv6, NAT type, DERP latencies), ping peers by dropdown or IP, whois, bug report, and update check. |
| **Taildrive** | File Sharing | Manage Taildrive shares: browse for a local folder, set a share name, and list active shares. |
| **Settings** | Settings | Save your Tailscale API key and tailnet name. Switch between Basic and Advanced mode. |

### API-backed pages (require a Tailscale API key — Advanced mode only)

| Page | Description |
| --- | --- |
| **DNS** | View and edit tailnet nameservers, search paths, and toggle MagicDNS. |
| **Devices (Admin)** | Full device list with filter, per-device authorize/expire key/delete actions. |
| **Users** | Tailnet member roster with online indicator, role, status, and device count. |
| **Policy** | View and edit your tailnet ACL/policy file with a graphical editor and JSON fallback. |
| **Logs** | Tail network logs for the last 1–168 hours with source and destination columns. |
| **Keys** | Create reusable/ephemeral/preauthorized auth keys, copy the key value, and revoke existing keys. |

---

## Prerequisites

- **[Tailscale](https://tailscale.com/download)** installed on the host machine.
- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**

---

## Build and Run

```sh
git clone https://github.com/ViolentlyVia/ScalyTails
cd ScalyTails/ScalyTails.Web
dotnet run
```

Then open `http://localhost:5000` in your browser (or whatever port is shown in the terminal).

---

## API key

An API key is only needed for the Advanced mode admin pages (DNS, Devices, Users, Policy, Logs, Keys). Get one from the [Tailscale admin console](https://login.tailscale.com/admin/settings/keys) and enter it in **Settings**.
