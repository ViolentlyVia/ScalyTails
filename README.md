Here is a complete README.md for your application based on the provided source code.

ScalyTails
ScalyTails is a modern Windows Presentation Foundation (WPF) graphical user interface (GUI) wrapper for the Tailscale command-line interface. It provides a user-friendly, responsive dashboard to manage your Tailnet connections, monitor peers, configure exit nodes, and utilize advanced features like Tailscale Serve and Funnel directly from your Windows desktop.

🚀 Features
Connection & Status Management: View your real-time backend state, active Tailnet name, client version, and local IP addresses. Easily connect or disconnect from Tailscale with a single click.

Peer Monitoring: See a list of all devices (peers) on your network, prioritized by online status.

Quick SSH & Taildrop: Instantly launch an SSH session into any connected peer (supports Windows Terminal, PowerShell, and CMD) or securely send files directly to other devices using Taildrop.

Exit Node Configuration: Quickly view available exit nodes, route your traffic through a selected exit node, and optionally allow local LAN access while connected.

Subnet Routing: Easily add, remove, and apply advertised subnet routes (CIDR) and toggle route acceptance.

Tailscale Serve & Funnel: Easily expose local services and ports to your Tailnet (via HTTP/HTTPS) or the public internet (via HTTPS Funnel) with a simple UI.

Advanced Settings Toggles: Enable or disable core Tailscale preferences on the fly, including:

Shields Up

MagicDNS / CorpDNS acceptance

SSH Server runtime

Exit Node advertisement

System Tray Integration: Built with tray icon support for easy background access.

🛠️ Built With
.NET 8.0 (Windows / WPF)

CommunityToolkit.Mvvm (v8.3.2) - For robust Model-View-ViewModel architecture and state management.

MaterialDesignThemes (v5.1.0) - For a sleek, modern UI.

Hardcodet.NotifyIcon.Wpf (v2.0.1) - For system tray icon capabilities.

📋 Prerequisites
To run ScalyTails, you will need the following installed on your Windows machine:

Tailscale: The official Tailscale Windows client must be installed (ScalyTails automatically searches for tailscale.exe in your Program Files or Program Files (x86) directories).

.NET 8.0 Desktop Runtime: Required to execute the WPF application.

⚙️ How It Works
ScalyTails acts as a frontend for the official Tailscale CLI. It uses a background timer that polls the Tailscale daemon every 5 seconds to fetch the latest network status, peer lists, active exit nodes, and local preferences. All actions taken in the UI (like adding a route or setting a Serve port) are translated into secure, invisible command-line calls to your local tailscale.exe process.
