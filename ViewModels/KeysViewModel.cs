using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class KeysViewModel : ObservableObject, IApiKeyAware
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private ObservableCollection<ApiAuthKey> _keys = [];

    // Create form
    [ObservableProperty] private string _newKeyDescription = "";
    [ObservableProperty] private bool _newKeyReusable;
    [ObservableProperty] private bool _newKeyEphemeral;
    [ObservableProperty] private bool _newKeyPreauthorized = true;
    [ObservableProperty] private string _createdKeyValue = "";

    public bool HasApiKey => _api.IsConfigured;
    public void OnApiKeyChanged() => OnPropertyChanged(nameof(HasApiKey));

    public KeysViewModel(ITailscaleApiService api)
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
            var list = await _api.GetKeysAsync(ct);
            Keys.Clear();
            foreach (var k in (list?.Keys ?? []).OrderByDescending(k => k.Created))
                Keys.Add(k);
            IsLoaded = true;
            StatusMessage = "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateKeyAsync()
    {
        IsBusy = true;
        CreatedKeyValue = "";
        try
        {
            var request = new CreateKeyRequest
            {
                Description = NewKeyDescription.Trim(),
                Capabilities = new ApiKeyCapabilities
                {
                    Devices = new ApiKeyDeviceCapabilities
                    {
                        Create = new ApiKeyCreateParams
                        {
                            Reusable = NewKeyReusable,
                            Ephemeral = NewKeyEphemeral,
                            Preauthorized = NewKeyPreauthorized,
                        }
                    }
                }
            };

            var key = await _api.CreateKeyAsync(request);
            if (key is null)
            {
                StatusMessage = "Failed to create key.";
                return;
            }

            CreatedKeyValue = key.Key;
            StatusMessage = "Key created. Copy it now — it won't be shown again.";
            NewKeyDescription = "";
            await RefreshAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DeleteKeyAsync(ApiAuthKey key)
    {
        IsBusy = true;
        try
        {
            var ok = await _api.DeleteKeyAsync(key.Id);
            StatusMessage = ok ? "Key revoked." : "Failed to revoke key.";
            if (ok) await RefreshAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void CopyCreatedKey()
    {
        if (!string.IsNullOrWhiteSpace(CreatedKeyValue))
            System.Windows.Clipboard.SetText(CreatedKeyValue);
    }
}
