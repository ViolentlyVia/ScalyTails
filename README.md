# ScalyTails

A web interface for [Tailscale](https://tailscale.com), built with Blazor Server and .NET 10. ScalyTails wraps the Tailscale CLI and REST API into a browser-based dashboard with a Material Design UI, designed to be accessed securely over your Tailnet from any device.

---

## Modes

ScalyTails has two modes, switchable from **Settings**:

| | Basic (default) | Advanced |
| --- | --- | --- |
| Target user | Anyone | Tailnet admins |
| API key required | No | For admin pages |
| Pages shown | Local only | All pages |
| Page names | Friendly | Technical |

---

## Pages

### Local pages — no API key needed

| Advanced name | Basic name | What it does |
| --- | --- | --- |
| Overview | Dashboard | Connection status, Tailnet name, IPs, version. Connect/disconnect. |
| Peers | My Network | All devices sorted by online status. One-click SSH and file send. |
| Exit Nodes | Exit Nodes | Browse and activate exit nodes; toggle LAN access. |
| Subnet Routes | Network Sharing | Advertise CIDR routes; accept routes from peers. |
| Serve & Funnel | Share Services | Expose local services over HTTP/HTTPS or public Funnel. |
| Diagnostics | Troubleshooting | Network check, ping, whois, bug report, update check. |
| Taildrive | File Sharing | Manage Taildrive folder shares. |
| Settings | Settings | API key, tailnet name, Basic/Advanced toggle. |

### Admin pages — API key required, Advanced mode only

| Page | What it does |
| --- | --- |
| DNS | Nameservers, search paths, MagicDNS toggle. |
| Devices | Full device list with authorize / expire key / delete actions. |
| Users | Member roster with roles, status, and device count. |
| Policy | Edit ACL policy with a graphical editor or raw JSON. |
| Logs | Network flow logs for the last 1–168 hours. |
| Keys | Create, copy, and revoke auth keys. |

---

## Requirements

- [Tailscale](https://tailscale.com/download) installed and running on the host machine
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (not needed for pre-built releases)

---

## Run from source

```sh
git clone https://github.com/ViolentlyVia/ScalyTails
cd ScalyTails/ScalyTails.Web
dotnet run
```

Then open `http://localhost:5169` in your browser.

---

## Install from release

### Windows

1. Download `ScalyTails-Web-vX.X.X-win-x64.zip` from [Releases](https://github.com/ViolentlyVia/ScalyTails/releases)
2. Extract and run `ScalyTails.Web.exe`
3. Open `http://localhost:5169`

.NET is bundled — no separate install needed.

### Debian / Ubuntu

```sh
sudo apt install ./ScalyTails-Web-vX.X.X-linux-amd64.deb
```

This installs a systemd service (`scalytails-web`) that starts automatically and listens on `http://localhost:5169`.

```sh
# Service management
sudo systemctl status scalytails-web
sudo systemctl restart scalytails-web
sudo systemctl stop scalytails-web
```

---

## API key

An API key is only needed for the admin pages in Advanced mode. Get one from the [Tailscale admin console](https://login.tailscale.com/admin/settings/keys) and paste it into **Settings**. The key is stored locally in `%APPDATA%\ScalyTails\settings.json` (Windows) or `~/.config/ScalyTails/settings.json` (Linux).

---

## License

[GPLv3](https://www.gnu.org/licenses/gpl-3.0.html)
