using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class LogsViewModel : ObservableObject, IApiKeyAware
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private ObservableCollection<ApiNetworkLog> _logs = [];
    [ObservableProperty] private int _hoursBack = 24;

    public bool HasApiKey => _api.IsConfigured;
    public void OnApiKeyChanged() => OnPropertyChanged(nameof(HasApiKey));
    public int[] HourOptions { get; } = [1, 6, 24, 48, 168];

    public LogsViewModel(ITailscaleApiService api)
    {
        _api = api;
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!_api.IsConfigured) { StatusMessage = "No API key configured. Go to Settings."; return; }
        IsBusy = true;
        try
        {
            var start = DateTime.UtcNow.AddHours(-HoursBack);
            var result = await _api.GetNetworkLogsAsync(start, ct);

            if (!result.Success)
            {
                // Network flow logs require the feature to be enabled in the Tailscale admin
                // console (Settings > Network flow logging) and may need a paid plan.
                StatusMessage = $"Failed to load logs: {result.Error}. " +
                                "If this is HTTP 404/403, verify that Network flow logging is " +
                                "enabled in the Tailscale admin console (requires a paid plan).";
                return;
            }

            Logs.Clear();
            // Cap at 500 entries — the API can return thousands of rows and the DataGrid
            // will freeze the UI thread if it tries to render them all at once.
            foreach (var log in (result.Data?.Logs ?? []).OrderByDescending(l => l.Logged).Take(500))
                Logs.Add(log);
            IsLoaded = true;
            StatusMessage = Logs.Count == 0 ? "No logs in selected time range." : $"{Logs.Count} log entries.";
        }
        finally { IsBusy = false; }
    }
}
