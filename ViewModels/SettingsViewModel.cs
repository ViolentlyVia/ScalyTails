using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
    [ObservableProperty] private string _testOutput = "";

    // API Key
    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _tailnet = "-";
    [ObservableProperty] private bool _apiKeySet;
    [ObservableProperty] private string _keyTypeWarning = "";

    // Called automatically by the generated ApiKey setter via CommunityToolkit.Mvvm source gen.
    // Catches the common mistake of pasting a device auth key (tskey-auth-) instead of an API
    // access token (tskey-api-) — both look similar but the auth key will always return HTTP 401.
    partial void OnApiKeyChanged(string value)
    {
        var key = value.Trim();
        if (key.StartsWith("tskey-auth-"))
            KeyTypeWarning = "This looks like a device auth key (tskey-auth-...). API features require an access token (tskey-api-...) — generate one at tailscale.com/admin/settings/keys.";
        else
            KeyTypeWarning = "";
    }

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
        TestOutput = "";
    }

    // Tests all three auth formats against the real Tailscale API and shows raw results.
    // Helps diagnose which format Tailscale actually accepts for the current key type.
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        var key = ApiKey.Trim();
        if (string.IsNullOrEmpty(key))
        {
            StatusMessage = "Enter an API key first.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Testing...";
        TestOutput = "";

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            const string url = "https://api.tailscale.com/api/v2/tailnet/-/devices";

            var sb = new StringBuilder();
            sb.AppendLine($"Key prefix : {(key.Length > 16 ? key[..16] + "..." : key)}");
            sb.AppendLine($"Key length : {key.Length} chars");
            sb.AppendLine();

            // Try 1: Basic auth, key as username (official Go client format)
            var r1 = await TryRequest(http, url, "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(key + ":")));
            sb.AppendLine($"[1] Basic key:  → HTTP {r1.Status}  {Trim(r1.Body)}");

            // Try 2: Basic auth, key as password (empty username)
            var r2 = await TryRequest(http, url, "Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + key)));
            sb.AppendLine($"[2] Basic :key  → HTTP {r2.Status}  {Trim(r2.Body)}");

            // Try 3: Bearer token
            var r3 = await TryRequest(http, url, "Bearer", key);
            sb.AppendLine($"[3] Bearer      → HTTP {r3.Status}  {Trim(r3.Body)}");

            var working = r1.Status == 200 ? "[1]" : r2.Status == 200 ? "[2]" : r3.Status == 200 ? "[3]" : "none";
            sb.AppendLine();
            sb.AppendLine(working == "none"
                ? "No format returned 200. Key may be invalid, wrong type, or network blocked."
                : $"Format {working} works.");

            TestOutput = sb.ToString();
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Test failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    private static async Task<(int Status, string Body)> TryRequest(
        HttpClient http, string url, string scheme, string credentials)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue(scheme, credentials);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var resp = await http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            return ((int)resp.StatusCode, body);
        }
        catch (Exception ex) { return (0, ex.Message); }
    }

    private static string Trim(string s, int max = 100) =>
        s.Length <= max ? s.Trim() : s[..max].Trim() + "...";

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
                // First line is a header row; active account is prefixed with '*'
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
        StatusMessage = $"Switching to {SelectedAccount}...";
        try
        {
            var result = await _tailscale.SwitchAccountAsync(SelectedAccount);
            StatusMessage = result.Success ? $"Switched to {SelectedAccount}." : $"Error: {result.Stderr}";
        }
        finally { IsBusy = false; }
    }
}
