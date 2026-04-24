using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class DevicesAdminViewModel : ObservableObject
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private ObservableCollection<ApiDevice> _devices = [];
    [ObservableProperty] private ApiDevice? _selectedDevice;
    [ObservableProperty] private string _filterText = "";

    public bool HasApiKey => _api.IsConfigured;

    private List<ApiDevice> _allDevices = [];

    public DevicesAdminViewModel(ITailscaleApiService api)
    {
        _api = api;
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Devices.Clear();
        var filter = FilterText.Trim().ToLower();
        foreach (var d in _allDevices.Where(d =>
            string.IsNullOrEmpty(filter)
            || d.ShortName.ToLower().Contains(filter)
            || d.User.ToLower().Contains(filter)
            || d.PrimaryAddress.Contains(filter)))
        {
            Devices.Add(d);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!_api.IsConfigured) { StatusMessage = "No API key configured. Go to Settings."; return; }
        IsBusy = true;
        try
        {
            var list = await _api.GetDevicesAsync(ct);
            _allDevices = (list?.Devices ?? [])
                .OrderByDescending(d => d.LastSeen)
                .ToList();
            ApplyFilter();
            IsLoaded = true;
            StatusMessage = "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AuthorizeDeviceAsync(ApiDevice device)
    {
        IsBusy = true;
        try
        {
            var ok = await _api.AuthorizeDeviceAsync(device.Id, true);
            StatusMessage = ok ? $"Authorized {device.ShortName}." : "Authorization failed.";
            if (ok) await RefreshAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExpireKeyAsync(ApiDevice device)
    {
        IsBusy = true;
        try
        {
            var ok = await _api.ExpireDeviceKeyAsync(device.Id);
            StatusMessage = ok ? $"Key expired for {device.ShortName}." : "Expire key failed.";
            if (ok) await RefreshAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteDeviceAsync(ApiDevice device)
    {
        IsBusy = true;
        try
        {
            var ok = await _api.DeleteDeviceAsync(device.Id);
            StatusMessage = ok ? $"Removed {device.ShortName}." : "Remove failed.";
            if (ok) await RefreshAsync();
        }
        finally { IsBusy = false; }
    }
}
