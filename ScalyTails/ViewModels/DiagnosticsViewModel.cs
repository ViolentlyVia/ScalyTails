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
    [ObservableProperty] private string _pingTarget = "";
    [ObservableProperty] private string _pingOutput = "";
    [ObservableProperty] private bool _isPinging;

    // Whois
    [ObservableProperty] private string _whoisTarget = "";
    [ObservableProperty] private string _whoisOutput = "";

    // Bug report
    [ObservableProperty] private string _bugReportId = "";

    // Update
    [ObservableProperty] private string _updateStatus = "";
    [ObservableProperty] private bool _updateAvailable;

    public DiagnosticsViewModel(ITailscaleService tailscale)
    {
        _tailscale = tailscale;
    }

    [RelayCommand]
    private async Task RunNetCheckAsync()
    {
        IsBusy = true;
        StatusMessage = "Running network check…";
        try
        {
            var result = await _tailscale.NetCheckAsync();
            if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
            {
                StatusMessage = "netcheck failed: " + result.Stderr;
                return;
            }

            var report = JsonSerializer.Deserialize<NetCheckResult>(result.Stdout,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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
