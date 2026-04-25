using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class DnsViewModel : ObservableObject, IApiKeyAware
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private bool _magicDnsEnabled;
    [ObservableProperty] private ObservableCollection<string> _nameservers = [];
    [ObservableProperty] private ObservableCollection<string> _searchPaths = [];
    [ObservableProperty] private string _newNameserver = "";
    [ObservableProperty] private string _newSearchPath = "";

    public bool HasApiKey => _api.IsConfigured;
    public void OnApiKeyChanged() => OnPropertyChanged(nameof(HasApiKey));

    public DnsViewModel(ITailscaleApiService api)
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
            // Fire all three requests in parallel to minimize page-load latency
            var nsTask   = _api.GetNameserversAsync(ct);
            var spTask   = _api.GetSearchPathsAsync(ct);
            var prefTask = _api.GetDnsPreferencesAsync(ct);
            await Task.WhenAll(nsTask, spTask, prefTask);

            var nsResult   = await nsTask;
            var spResult   = await spTask;
            var prefResult = await prefTask;

            if (!nsResult.Success || !spResult.Success || !prefResult.Success)
            {
                var error = (!nsResult.Success   ? nsResult.Error :
                             !spResult.Success   ? spResult.Error :
                                                   prefResult.Error);
                StatusMessage = $"Failed to load DNS settings: {error}";
                return;
            }

            Nameservers.Clear();
            foreach (var ns in nsResult.Data?.Dns ?? [])
                Nameservers.Add(ns);

            SearchPaths.Clear();
            foreach (var sp in spResult.Data?.SearchPaths ?? [])
                SearchPaths.Add(sp);

            MagicDnsEnabled = prefResult.Data?.MagicDNS ?? false;
            IsLoaded = true;
            StatusMessage = "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ToggleMagicDnsAsync()
    {
        IsBusy = true;
        try
        {
            var ok = await _api.SetMagicDnsAsync(MagicDnsEnabled);
            StatusMessage = ok ? $"MagicDNS {(MagicDnsEnabled ? "enabled" : "disabled")}." : "Failed to update MagicDNS.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddNameserver()
    {
        var ns = NewNameserver.Trim();
        if (string.IsNullOrWhiteSpace(ns) || Nameservers.Contains(ns)) return;
        Nameservers.Add(ns);
        NewNameserver = "";
    }

    [RelayCommand]
    private void RemoveNameserver(string ns) => Nameservers.Remove(ns);

    [RelayCommand]
    private async Task ApplyNameserversAsync()
    {
        IsBusy = true;
        try
        {
            var ok = await _api.SetNameserversAsync([.. Nameservers]);
            StatusMessage = ok ? "Nameservers updated." : "Failed to update nameservers.";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddSearchPath()
    {
        var sp = NewSearchPath.Trim();
        if (string.IsNullOrWhiteSpace(sp) || SearchPaths.Contains(sp)) return;
        SearchPaths.Add(sp);
        NewSearchPath = "";
    }

    [RelayCommand]
    private void RemoveSearchPath(string sp) => SearchPaths.Remove(sp);

    [RelayCommand]
    private async Task ApplySearchPathsAsync()
    {
        IsBusy = true;
        try
        {
            var ok = await _api.SetSearchPathsAsync([.. SearchPaths]);
            StatusMessage = ok ? "Search paths updated." : "Failed to update search paths.";
        }
        finally { IsBusy = false; }
    }
}
