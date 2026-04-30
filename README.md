# ScalyTails Web

A full-featured web interface for [Tailscale](https://tailscale.com), built with Blazor Server and .NET 10. ScalyTails wraps the Tailscale CLI and REST API into a polished web application with a Material Design interface (via MudBlazor) that can be accessed securely over your Tailnet.

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

---

## Prerequisites

- **[Tailscale](https://tailscale.com/download)** installed on the host machine.
- **[.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**

---

## Build and Run

```sh
git clone [https://github.com/ViolentlyVia/ScalyTails](https://github.com/ViolentlyVia/ScalyTails)
cd ScalyTails/ScalyTails.Web
dotnet run
