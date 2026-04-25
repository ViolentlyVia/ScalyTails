using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class DiagnosticsViewModel : ObservableObject
{
    private readonly ITailscaleService _tailscale;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    // NetCheck
    [ObservableProperty] private bool _netCheckDone;
    [ObservableProperty] private string _netCheckUdp = "—";
    [ObservableProperty] private string _netCheckIPv4 = "—";
    [ObservableProperty] private string _netCheckIPv6 = "—";
    [ObservableProperty] private string _netCheckNat = "—";
    [ObservableProperty] private string _netCheckPreferredDerp = "—";
    [ObservableProperty] private ObservableCollection<DerpLatency> _derpLatencies = [];

    // Ping
    [ObservableProperty] private ObservableCollection<TailscalePeer> _peers = [];
    [ObservableProperty] private TailscalePeer? _selectedPeer;
    [ObservableProperty] private string _pingTarget = "";
    [ObservableProperty] private string _pingOutput = "";
    [ObservableProperty] private bool _isPinging;

    // CommunityToolkit.Mvvm auto-generates the SelectedPeer property setter and calls this
    // partial method after the value changes, so the TextBox below always reflects the selection.
    partial void OnSelectedPeerChanged(TailscalePeer? value)
    {
        if (value is not null)
            PingTarget = value.PrimaryIP;
    }

    // Whois
    [ObservableProperty] private string _whoisTarget = "";
    [ObservableProperty] private string _whoisOutput = "";

    // Bug report
    [ObservableProperty] private string _bugReportId = "";

    // Update
    [ObservableProperty] private string _updateStatus = "";
    [ObservableProperty] private bool _updateAvailable;

    // Cached to avoid allocating a new instance on every netcheck invocation
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public DiagnosticsViewModel(ITailscaleService tailscale)
    {
        _tailscale = tailscale;
        // Fire-and-forget: async constructors aren't possible; discard warns the compiler the
        // omission is intentional rather than a forgotten await.
        _ = RefreshPeersAsync();
    }

    [RelayCommand]
    private async Task RefreshPeersAsync()
    {
        var status = await _tailscale.GetStatusAsync();
        Peers.Clear();
        if (status is null) return;
        foreach (var peer in status.AllPeers.OrderBy(p => p.DisplayName))
            Peers.Add(peer);
    }

    [RelayCommand]
    private async Task RunNetCheckAsync()
    {
        IsBusy = true;
        StatusMessage = "Running network check…";
        try
        {
            var result = await _tailscale.NetCheckAsync();
            // Some Tailscale versions write netcheck JSON to stderr rather than stdout
            var json = string.IsNullOrWhiteSpace(result.Stdout) ? result.Stderr : result.Stdout;
            if (string.IsNullOrWhiteSpace(json))
            {
                StatusMessage = "netcheck produced no output. Is Tailscale running?";
                return;
            }

            NetCheckResult? report;
            try
            {
                report = JsonSerializer.Deserialize<NetCheckResult>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to parse netcheck output: {ex.Message}";
                return;
            }

            if (report is null) { StatusMessage = "Failed to parse netcheck output."; return; }

            NetCheckUdp = report.UDP ? "✓ Available" : "✗ Blocked";
            NetCheckIPv4 = report.HasIPv4 ? $"✓ {report.PublicIPv4}" : "✗ Unavailable";
            NetCheckIPv6 = report.HasIPv6 ? $"✓ {report.PublicIPv6}" : "✗ Unavailable";
            NetCheckNat = report.MappingVariesByDestIP == true ? "Strict (varies by dest)" : "Easy";
            NetCheckPreferredDerp = report.PreferredDERP > 0 ? $"Region {report.PreferredDERP}" : "None";

            DerpLatencies.Clear();
            foreach (var d in report.SortedDerpLatencies().Take(12))
                DerpLatencies.Add(d);

            NetCheckDone = true;
            StatusMessage = "Network check complete.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task PingAsync()
    {
        var host = PingTarget.Trim();
        if (string.IsNullOrWhiteSpace(host)) return;
        IsPinging = true;
        PingOutput = "Pinging…";
        try
        {
            var result = await _tailscale.PingAsync(host);
            PingOutput = string.IsNullOrWhiteSpace(result.Stdout) ? result.Stderr : result.Stdout;
        }
        finally { IsPinging = false; }
    }

    [RelayCommand]
    private async Task WhoisAsync()
    {
        var target = WhoisTarget.Trim();
        if (string.IsNullOrWhiteSpace(target)) return;
        IsBusy = true;
        WhoisOutput = "Looking up…";
        try
        {
            var result = await _tailscale.WhoisAsync(target);
            WhoisOutput = string.IsNullOrWhiteSpace(result.Stdout) ? result.Stderr : result.Stdout;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task GenerateBugReportAsync()
    {
        IsBusy = true;
        BugReportId = "Generating…";
        try
        {
            var result = await _tailscale.BugReportAsync();
            BugReportId = result.Success ? result.Stdout.Trim() : $"Error: {result.Stderr}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        IsBusy = true;
        UpdateStatus = "Checking…";
        try
        {
            var result = await _tailscale.UpdateCheckAsync();
            var text = (result.Stdout + " " + result.Stderr).Trim();
            UpdateAvailable = text.Contains("update", StringComparison.OrdinalIgnoreCase)
                           && !text.Contains("up to date", StringComparison.OrdinalIgnoreCase);
            UpdateStatus = text;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ApplyUpdateAsync()
    {
        IsBusy = true;
        UpdateStatus = "Updating…";
        try
        {
            var result = await _tailscale.UpdateApplyAsync();
            UpdateStatus = result.Success ? "Update applied. Restart may be required." : $"Error: {result.Stderr}";
        }
        finally { IsBusy = false; }
    }
}
