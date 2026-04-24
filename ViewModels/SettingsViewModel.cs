using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ITailscaleService _tailscale;
    private readonly IAppSettingsService _appSettings;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";

    // API Key
    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _tailnet = "-";
    [ObservableProperty] private bool _apiKeySet;

    // Accounts
    [ObservableProperty] private ObservableCollection<string> _accounts = [];
    [ObservableProperty] private string _selectedAccount = "";

    public SettingsViewModel(ITailscaleService tailscale, IAppSettingsService appSettings)
    {
        _tailscale = tailscale;
        _appSettings = appSettings;
        ApiKey = appSettings.Settings.ApiKey;
        Tailnet = appSettings.Settings.Tailnet;
        ApiKeySet = appSettings.HasApiKey;
    }

    [RelayCommand]
    private void SaveApiKey()
    {
        _appSettings.Settings.ApiKey = ApiKey.Trim();
        _appSettings.Settings.Tailnet = string.IsNullOrWhiteSpace(Tailnet) ? "-" : Tailnet.Trim();
        _appSettings.Save();
        ApiKeySet = _appSettings.HasApiKey;
        StatusMessage = ApiKeySet ? "API key saved." : "API key cleared.";
    }

    [RelayCommand]
    private void ClearApiKey()
    {
        ApiKey = "";
        _appSettings.Settings.ApiKey = "";
        _appSettings.Save();
        ApiKeySet = false;
        StatusMessage = "API key cleared.";
    }

    [RelayCommand]
    private async Task RefreshAccountsAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _tailscale.SwitchListAsync();
            Accounts.Clear();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Stdout))
            {
                foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
                {
                    var name = line.Trim().TrimStart('*').Trim();
                    if (!string.IsNullOrWhiteSpace(name))
                        Accounts.Add(name);
                }
            }
            StatusMessage = Accounts.Count == 0 ? "Only one account found." : "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SwitchAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedAccount)) return;
        IsBusy = true;
        StatusMessage = $"Switching to {SelectedAccount}…";
        try
        {
            var result = await _tailscale.SwitchAccountAsync(SelectedAccount);
            StatusMessage = result.Success ? $"Switched to {SelectedAccount}." : $"Error: {result.Stderr}";
        }
        finally { IsBusy = false; }
    }
}
