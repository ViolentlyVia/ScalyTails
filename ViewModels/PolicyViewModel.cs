using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class PolicyViewModel : ObservableObject, IApiKeyAware
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _policyText = "";
    [ObservableProperty] private bool _isDirty;

    public bool HasApiKey => _api.IsConfigured;
    public void OnApiKeyChanged() => OnPropertyChanged(nameof(HasApiKey));

    // Snapshot of the last successfully loaded/saved text — used to detect unsaved edits.
    // IsDirty drives the Save/Revert button visibility in the XAML.
    private string _savedText = "";

    public PolicyViewModel(ITailscaleApiService api)
    {
        _api = api;
    }

    partial void OnPolicyTextChanged(string value) =>
        IsDirty = value != _savedText;

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct = default)
    {
        if (!_api.IsConfigured) { StatusMessage = "No API key configured. Go to Settings."; return; }
        IsBusy = true;
        try
        {
            var result = await _api.GetPolicyAsync(ct);
            if (!result.Success) { StatusMessage = $"Failed to load policy: {result.Error}"; return; }

            var policy = result.Data ?? "";

            // Pretty-print if valid JSON
            try
            {
                var doc = System.Text.Json.JsonDocument.Parse(policy);
                policy = System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            }
            catch { }

            _savedText = policy;
            PolicyText = policy;
            IsDirty = false;
            IsLoaded = true;
            StatusMessage = "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SavePolicyAsync()
    {
        IsBusy = true;
        StatusMessage = "Saving policy...";
        try
        {
            var ok = await _api.SetPolicyAsync(PolicyText);
            if (ok)
            {
                _savedText = PolicyText;
                IsDirty = false;
                StatusMessage = "Policy saved successfully.";
            }
            else
            {
                StatusMessage = "Failed to save policy. Check JSON syntax.";
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void RevertPolicy()
    {
        PolicyText = _savedText;
        IsDirty = false;
    }
}
