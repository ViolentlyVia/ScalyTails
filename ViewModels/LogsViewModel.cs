using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class LogsViewModel : ObservableObject
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private ObservableCollection<ApiNetworkLog> _logs = [];
    [ObservableProperty] private int _hoursBack = 24;

    public bool HasApiKey => _api.IsConfigured;
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
            var list = await _api.GetNetworkLogsAsync(start, ct);
            Logs.Clear();
            foreach (var log in (list?.Logs ?? []).OrderByDescending(l => l.Logged).Take(500))
                Logs.Add(log);
            IsLoaded = true;
            StatusMessage = Logs.Count == 0 ? "No logs in selected time range." : $"{Logs.Count} log entries.";
        }
        finally { IsBusy = false; }
    }
}
