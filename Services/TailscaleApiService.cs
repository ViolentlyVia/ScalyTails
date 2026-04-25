using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ScalyTails.Models;

namespace ScalyTails.Services;

public class TailscaleApiService : ITailscaleApiService
{
    private const string BaseUrl = "https://api.tailscale.com/api/v2";
    private readonly IAppSettingsService _settings;
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public bool IsConfigured => _settings.HasApiKey;

    public TailscaleApiService(IAppSettingsService settings)
    {
        _settings = settings;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // "-" is the Tailscale API shorthand for the authenticated user's own tailnet
    private string Tailnet => _settings.Settings.Tailnet;

    private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, $"{BaseUrl}{path}");
        // Auth header set per-request so a key change in Settings takes effect immediately
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Settings.ApiKey);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, path);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        catch { return null; }
    }

    private async Task<bool> SendAsync(HttpMethod method, string path, object? body = null, CancellationToken ct = default)
    {
        try
        {
            using var request = BuildRequest(method, path, body);
            using var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Devices ────────────────────────────────────────────────────────────────

    public Task<ApiDeviceList?> GetDevicesAsync(CancellationToken ct = default) =>
        GetAsync<ApiDeviceList>($"/tailnet/{Tailnet}/devices", ct);

    public Task<ApiDevice?> GetDeviceAsync(string deviceId, CancellationToken ct = default) =>
        GetAsync<ApiDevice>($"/device/{deviceId}", ct);

    public Task<bool> AuthorizeDeviceAsync(string deviceId, bool authorized, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/device/{deviceId}/authorized",
            new { authorized }, ct);

    public Task<bool> ExpireDeviceKeyAsync(string deviceId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/device/{deviceId}/expire", null, ct);

    public Task<bool> DeleteDeviceAsync(string deviceId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/device/{deviceId}", null, ct);

    public Task<bool> SetDeviceTagsAsync(string deviceId, List<string> tags, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/device/{deviceId}/tags", new { tags }, ct);

    public Task<bool> SetDeviceRoutesAsync(string deviceId, List<string> routes, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/device/{deviceId}/routes",
            new { routes }, ct);

    // ── Users ──────────────────────────────────────────────────────────────────

    public Task<ApiUserList?> GetUsersAsync(CancellationToken ct = default) =>
        GetAsync<ApiUserList>($"/tailnet/{Tailnet}/users", ct);

    // ── DNS ────────────────────────────────────────────────────────────────────

    public Task<ApiDnsNameservers?> GetNameserversAsync(CancellationToken ct = default) =>
        GetAsync<ApiDnsNameservers>($"/tailnet/{Tailnet}/dns/nameservers", ct);

    public Task<bool> SetNameserversAsync(List<string> nameservers, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/dns/nameservers",
            new { dns = nameservers }, ct);

    public Task<ApiDnsSearchPaths?> GetSearchPathsAsync(CancellationToken ct = default) =>
        GetAsync<ApiDnsSearchPaths>($"/tailnet/{Tailnet}/dns/searchpaths", ct);

    public Task<bool> SetSearchPathsAsync(List<string> searchPaths, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/dns/searchpaths",
            new { searchPaths }, ct);

    public Task<ApiDnsPreferences?> GetDnsPreferencesAsync(CancellationToken ct = default) =>
        GetAsync<ApiDnsPreferences>($"/tailnet/{Tailnet}/dns/preferences", ct);

    public Task<bool> SetMagicDnsAsync(bool enabled, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/dns/preferences",
            new { magicDNS = enabled }, ct);

    // ── Policy / ACL ───────────────────────────────────────────────────────────

    public async Task<string?> GetPolicyAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"/tailnet/{Tailnet}/acl");
            // Override Accept: the API returns HuJSON if Accept includes */*; force JSON
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch { return null; }
    }

    public async Task<bool> SetPolicyAsync(string policyJson, CancellationToken ct = default)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Post, $"/tailnet/{Tailnet}/acl");
            request.Content = new StringContent(policyJson, Encoding.UTF8, "application/json");
            using var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ── Logs ───────────────────────────────────────────────────────────────────

    public async Task<ApiNetworkLogList?> GetNetworkLogsAsync(DateTime? start = null, CancellationToken ct = default)
    {
        var startParam = (start ?? DateTime.UtcNow.AddHours(-24)).ToString("o");
        return await GetAsync<ApiNetworkLogList>(
            $"/tailnet/{Tailnet}/logs/network?start={Uri.EscapeDataString(startParam)}", ct);
    }

    // ── Auth Keys ──────────────────────────────────────────────────────────────

    public Task<ApiKeyList?> GetKeysAsync(CancellationToken ct = default) =>
        GetAsync<ApiKeyList>($"/tailnet/{Tailnet}/keys", ct);

    public async Task<ApiAuthKey?> CreateKeyAsync(CreateKeyRequest request, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"/tailnet/{Tailnet}/keys", request);
            using var response = await _http.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ApiAuthKey>(JsonOpts, ct);
        }
        catch { return null; }
    }

    public Task<bool> DeleteKeyAsync(string keyId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/tailnet/{Tailnet}/keys/{keyId}", null, ct);
}
