using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Models;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class UsersViewModel : ObservableObject, IApiKeyAware
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private ObservableCollection<ApiUser> _users = [];

    public bool HasApiKey => _api.IsConfigured;
    public void OnApiKeyChanged() => OnPropertyChanged(nameof(HasApiKey));

    public UsersViewModel(ITailscaleApiService api)
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
            var result = await _api.GetUsersAsync(ct);
            if (!result.Success) { StatusMessage = $"Failed to load users: {result.Error}"; return; }

            Users.Clear();
            foreach (var u in (result.Data?.Users ?? []).OrderByDescending(u => u.CurrentlyConnected).ThenBy(u => u.LoginName))
                Users.Add(u);
            IsLoaded = true;
            StatusMessage = "";
        }
        finally { IsBusy = false; }
    }
}
