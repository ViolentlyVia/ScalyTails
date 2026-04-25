using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITailscaleService _tailscale;
    private PeriodicTimer? _refreshTimer;
    private CancellationTokenSource _cts = new();

    // ── Status ──────────────────────────────────────────────────────────────
    [ObservableProperty] private string _backendState = "Unknown";
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _busyMessage = "";
    [ObservableProperty] private string _tailnetName = "";
    [ObservableProperty] private string _selfHostName = "";
    [ObservableProperty] private string _selfIPs = "";
    [ObservableProperty] private string _version = "";
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _tailscaleInstalled;
    [ObservableProperty] private bool _needsLogin;

    // ── Peers ────────────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PeerViewModel> _peers = [];
    [ObservableProperty] private int _onlinePeerCount;

    // ── Exit Nodes ───────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ExitNodeViewModel> _exitNodes = [];
    [ObservableProperty] private ExitNodeViewModel? _selectedExitNode;
    [ObservableProperty] private bool _isExitNodeActive;
    [ObservableProperty] private bool _allowLanAccess;
    [ObservableProperty] private string _activeExitNodeName = "None";

    partial void OnSelectedExitNodeChanged(ExitNodeViewModel? value) =>
        IsExitNodeActive = value is { IsNone: false };

    // ── Subnet Routes ────────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<SubnetRouteViewModel> _advertisedRoutes = [];
    [ObservableProperty] private string _newRouteInput = "";
    [ObservableProperty] private bool _acceptRoutes;
    [ObservableProperty] private bool _advertiseExitNode;

    // ── Settings / Prefs ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _shieldsUp;
    [ObservableProperty] private bool _magicDnsEnabled;
    [ObservableProperty] private bool _acceptDns;
    [ObservableProperty] private bool _sshServerEnabled;

    // ── Serve / Funnel ───────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ServeEntryViewModel> _serveEntries = [];
    [ObservableProperty] private string _newServePort = "443";
    [ObservableProperty] private string _newServeTarget = "";
    [ObservableProperty] private string _newServeProtocol = "HTTPS";
    [ObservableProperty] private bool _isLoadingServe;

    public string[] ServeProtocols { get; } = ["HTTPS", "HTTP", "HTTPS Funnel"];

    public MainViewModel(ITailscaleService tailscale)
    {
        _tailscale = tailscale;
        TailscaleInstalled = tailscale.IsTailscaleInstalled;
    }

    // Fire-and-forget is intentional — async constructors aren't possible in WPF
    public void StartRefresh()
    {
        _cts = new CancellationTokenSource();
        _ = RefreshLoopAsync(_cts.Token);
    }

    public void StopRefresh() => _cts.Cancel();

    private async Task RefreshLoopAsync(CancellationToken ct)
    {
        await RefreshAsync(ct);
        // PeriodicTimer skips missed ticks, so a slow refresh won't queue up multiple overlapping calls
        _refreshTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        try
        {
            while (await _refreshTimer.WaitForNextTickAsync(ct))
                await RefreshAsync(ct);
        }
        catch (OperationCanceledException) { }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!TailscaleInstalled) return;

        IsLoading = true;
        try
        {
            var statusTask = _tailscale.GetStatusAsync(ct);
            var prefsTask = _tailscale.GetPrefsAsync(ct);
            var serveTask = _tailscale.GetServeStatusAsync(ct);

            await Task.WhenAll(statusTask, prefsTask, serveTask);

            var status = await statusTask;
            var prefs = await prefsTask;
            var serve = await serveTask;

            // Task.WhenAll continuations can run on a thread-pool thread; marshal back to UI for ObservableCollection writes
            await Application.Current.Dispatcher.InvokeAsync(() =>
                ApplyStatus(status, prefs, serve));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyStatus(TailscaleStatus? status, TailscalePrefs? prefs, ServeStatus? serve)
    {
        if (status is null)
        {
            BackendState = "Unavailable";
            IsRunning = false;
            StatusMessage = "Cannot reach Tailscale daemon";
            return;
        }

        BackendState = status.BackendState;
        IsRunning = status.IsRunning;
        NeedsLogin = status.NeedsLogin;
        Version = status.Version;
        TailnetName = status.CurrentTailnet?.Name ?? "";
        MagicDnsEnabled = status.CurrentTailnet?.MagicDNSEnabled ?? false;

        if (status.Self is { } self)
        {
            SelfHostName = self.DisplayName;
            SelfIPs = string.Join("  /  ", self.TailscaleIPs ?? []);
        }

        StatusMessage = status.BackendState switch
        {
            "Running" => "Connected",
            "Starting" => "Connecting…",
            "Stopped" => "Disconnected",
            "NeedsLogin" => "Login required",
            "NeedsMachineAuth" => "Awaiting machine auth",
            _ => status.BackendState
        };

        // Peers
        var peerList = status.AllPeers
            .Select(p => PeerViewModel.FromPeer(p.ID, p))
            .OrderByDescending(p => p.Online)
            .ThenBy(p => p.DisplayName)
            .ToList();

        Peers.Clear();
        foreach (var p in peerList)
            Peers.Add(p);
        OnlinePeerCount = Peers.Count(p => p.Online);

        // Exit nodes — always prepend a "None" sentinel so SelectedExitNode is never null
        var activeExitPeer = status.ActiveExitNode;
        ActiveExitNodeName = activeExitPeer?.DisplayName ?? "None";

        var noneItem = ExitNodeViewModel.None();
        var exitList = status.AllPeers
            .Where(p => p.ExitNodeOption)
            .Select(p => ExitNodeViewModel.FromPeer(p, p.ExitNode))
            .OrderByDescending(e => e.Online)
            .ThenBy(e => e.DisplayName)
            .ToList();

        ExitNodes.Clear();
        ExitNodes.Add(noneItem);
        foreach (var e in exitList)
            ExitNodes.Add(e);

        SelectedExitNode = activeExitPeer is null
            ? noneItem
            : ExitNodes.FirstOrDefault(e => e.IsSelected) ?? noneItem;

        // Prefs
        if (prefs is not null)
        {
            AcceptRoutes = prefs.RouteAll;
            ShieldsUp = prefs.ShieldsUp;
            AllowLanAccess = prefs.ExitNodeAllowLANAccess;
            AcceptDns = prefs.CorpDNS;
            SshServerEnabled = prefs.RunSSH;

            AdvertisedRoutes.Clear();
            foreach (var route in prefs.AdvertiseRoutes ?? [])
                AdvertisedRoutes.Add(new SubnetRouteViewModel(route));
        }

        // Serve / Funnel
        if (serve is not null)
        {
            ServeEntries.Clear();
            foreach (var entry in serve.ToEntries())
                ServeEntries.Add(ServeEntryViewModel.FromEntry(entry));
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanToggleConnection))]
    private async Task ToggleConnectionAsync()
    {
        await RunBusyAsync(
            IsRunning ? "Disconnecting…" : "Connecting…",
            IsRunning ? _tailscale.DisconnectAsync : _tailscale.ConnectAsync);
    }

    private bool CanToggleConnection() => TailscaleInstalled && !IsBusy;

    [RelayCommand]
    private async Task ApplyExitNodeAsync()
    {
        if (SelectedExitNode is null || SelectedExitNode.IsNone)
            await RunBusyAsync("Clearing exit node…", ct => _tailscale.ClearExitNodeAsync(ct));
        else
            await RunBusyAsync(
                $"Setting exit node to {SelectedExitNode.DisplayName}…",
                ct => _tailscale.SetExitNodeAsync(SelectedExitNode.PrimaryIP, AllowLanAccess, ct));
    }

    [RelayCommand]
    private async Task ClearExitNodeAsync()
    {
        SelectedExitNode = null;
        await RunBusyAsync("Clearing exit node…", ct => _tailscale.ClearExitNodeAsync(ct));
    }

    [RelayCommand]
    private async Task ToggleAcceptRoutesAsync() =>
        await RunBusyAsync(
            AcceptRoutes ? "Accepting routes…" : "Stopping route acceptance…",
            ct => _tailscale.SetAcceptRoutesAsync(AcceptRoutes, ct));

    [RelayCommand]
    private async Task ToggleShieldsUpAsync() =>
        await RunBusyAsync(
            ShieldsUp ? "Enabling shields up…" : "Disabling shields up…",
            ct => _tailscale.SetShieldsUpAsync(ShieldsUp, ct));

    [RelayCommand]
    private async Task ToggleAdvertiseExitNodeAsync() =>
        await RunBusyAsync(
            AdvertiseExitNode ? "Advertising as exit node…" : "Stopping exit node advertisement…",
            ct => _tailscale.AdvertiseExitNodeAsync(AdvertiseExitNode, ct));

    [RelayCommand]
    private async Task ToggleAcceptDnsAsync() =>
        await RunBusyAsync(
            AcceptDns ? "Enabling MagicDNS…" : "Disabling MagicDNS…",
            ct => _tailscale.SetAcceptDnsAsync(AcceptDns, ct));

    [RelayCommand]
    private async Task ToggleSshServerAsync() =>
        await RunBusyAsync(
            SshServerEnabled ? "Enabling SSH server…" : "Disabling SSH server…",
            ct => _tailscale.SetSshServerAsync(SshServerEnabled, ct));

    [RelayCommand]
    private void AddRoute()
    {
        var cidr = NewRouteInput.Trim();
        if (string.IsNullOrWhiteSpace(cidr)) return;
        if (AdvertisedRoutes.Any(r => r.Cidr == cidr)) return;
        AdvertisedRoutes.Add(new SubnetRouteViewModel(cidr));
        NewRouteInput = "";
    }

    [RelayCommand]
    private void RemoveRoute(SubnetRouteViewModel route) =>
        AdvertisedRoutes.Remove(route);

    [RelayCommand]
    private async Task ApplyRoutesAsync() =>
        await RunBusyAsync(
            "Applying subnet routes…",
            ct => _tailscale.AdvertiseRoutesAsync(AdvertisedRoutes.Select(r => r.Cidr), ct));

    // ── Serve / Funnel Commands ───────────────────────────────────────────────

    [RelayCommand]
    private async Task AddServeAsync()
    {
        var portStr = NewServePort.Trim();
        var target = NewServeTarget.Trim();
        if (string.IsNullOrWhiteSpace(portStr) || string.IsNullOrWhiteSpace(target)) return;
        if (!int.TryParse(portStr, out var port)) return;

        await RunBusyAsync(
            $"Adding {NewServeProtocol} serve on :{port}…",
            ct => _tailscale.AddServeAsync(NewServeProtocol, port, target, ct));

        NewServeTarget = "";
    }

    [RelayCommand]
    private async Task RemoveServeEntryAsync(ServeEntryViewModel entry)
    {
        if (!int.TryParse(entry.Port, out var port)) return;
        await RunBusyAsync(
            $"Removing serve on :{entry.Port}…",
            ct => _tailscale.RemoveServeAsync(entry.Protocol, port, ct));
    }

    [RelayCommand]
    private async Task ResetServeAsync() =>
        await RunBusyAsync("Resetting all serve configs…", ct => _tailscale.ResetServeAsync(ct));

    // ── SSH / Taildrop ────────────────────────────────────────────────────────

    [RelayCommand]
    private void SshToPeer(PeerViewModel peer)
    {
        var hostname = string.IsNullOrWhiteSpace(peer.DnsName) ? peer.PrimaryIP : peer.DnsName;
        if (string.IsNullOrWhiteSpace(hostname)) return;

        var tailscalePath = _tailscale.TailscalePath ?? "tailscale";
        var sshCommand = $"\"{tailscalePath}\" ssh {hostname}";

        try
        {
            // Cascade: Windows Terminal → PowerShell → cmd.exe
            var started = TryStartTerminal("wt.exe", sshCommand)
                       || TryStartTerminal("powershell.exe", $"-NoExit -Command {sshCommand}")
                       || TryStartTerminal("cmd.exe", $"/K {sshCommand}");

            if (!started)
                StatusMessage = "Could not open a terminal window.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"SSH launch failed: {ex.Message}";
        }
    }

    private static bool TryStartTerminal(string executable, string args)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = args,
                UseShellExecute = true,
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task SendFileToPeerAsync(PeerViewModel peer, string filePath)
    {
        var target = string.IsNullOrWhiteSpace(peer.DnsName) ? peer.PrimaryIP : peer.DnsName;
        await RunBusyAsync(
            $"Sending file to {peer.DisplayName}…",
            ct => _tailscale.SendFileAsync(filePath, target, ct));
    }

    // ── Account ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LogoutAsync() =>
        await RunBusyAsync("Logging out…", ct => _tailscale.LogoutAsync(ct));

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task RunBusyAsync(string message, Func<CancellationToken, Task<CliResult>> action)
    {
        IsBusy = true;
        BusyMessage = message;
        try
        {
            var result = await action(_cts.Token);
            if (!result.Success)
                StatusMessage = $"Error: {result.Stderr}";
            await RefreshAsync(_cts.Token);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = "";
        }
    }
}
