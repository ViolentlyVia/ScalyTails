using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class PolicyViewModel : ObservableObject
{
    private readonly ITailscaleApiService _api;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private string _policyText = "";
    [ObservableProperty] private bool _isDirty;

    public bool HasApiKey => _api.IsConfigured;

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
            var policy = await _api.GetPolicyAsync(ct);
            if (policy is null) { StatusMessage = "Failed to load policy."; return; }

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
        StatusMessage = "Saving policy…";
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
