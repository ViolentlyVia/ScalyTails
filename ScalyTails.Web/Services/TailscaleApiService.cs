using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ScalyTails.Web.Models;

namespace ScalyTails.Web.Services;

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
        // Tailscale API uses HTTP Basic auth: key as username, empty password (matches the official Go client)
        // Auth header set per-request so a key change in Settings takes effect immediately
        var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes(_settings.Settings.ApiKey + ":"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);
        if (body != null)
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return request;
    }

    private async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, path);
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                var hint = (int)response.StatusCode switch
                {
                    401 => " — invalid or expired API key",
                    403 => " — API key lacks permission, or this feature requires a higher plan",
                    404 => " — endpoint not found; check tailnet name in Settings",
                    _   => ""
                };
                return ApiResult<T>.Fail($"HTTP {(int)response.StatusCode}{hint}");
            }
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
            if (data is null)
                return ApiResult<T>.Fail("API returned an empty response");
            return ApiResult<T>.Ok(data);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return ApiResult<T>.Fail(ex.Message); }
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

    public Task<ApiResult<ApiDeviceList>> GetDevicesAsync(CancellationToken ct = default) =>
        GetAsync<ApiDeviceList>($"/tailnet/{Tailnet}/devices", ct);

    public Task<ApiResult<ApiDevice>> GetDeviceAsync(string deviceId, CancellationToken ct = default) =>
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

    public Task<ApiResult<ApiUserList>> GetUsersAsync(CancellationToken ct = default) =>
        GetAsync<ApiUserList>($"/tailnet/{Tailnet}/users", ct);

    public Task<bool> UpdateUserRoleAsync(string userId, string role, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/users/{userId}/role", new { role }, ct);

    public Task<bool> SuspendUserAsync(string userId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/users/{userId}/suspend", null, ct);

    public Task<bool> RestoreUserAsync(string userId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/users/{userId}/restore", null, ct);

    public Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/tailnet/{Tailnet}/users/{userId}", null, ct);

    public Task<ApiResult<ApiUserInviteList>> GetUserInvitesAsync(CancellationToken ct = default) =>
        GetAsync<ApiUserInviteList>($"/tailnet/{Tailnet}/user-invites", ct);

    public Task<bool> CreateUserInviteAsync(string email, string role, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/user-invites",
            new[] { new { role, email } }, ct);

    public Task<bool> DeleteUserInviteAsync(string inviteId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/tailnet/{Tailnet}/user-invites/{inviteId}", null, ct);

    public Task<bool> ResendUserInviteAsync(string inviteId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/user-invites/{inviteId}/resend", null, ct);

    // ── DNS ────────────────────────────────────────────────────────────────────

    public Task<ApiResult<ApiDnsNameservers>> GetNameserversAsync(CancellationToken ct = default) =>
        GetAsync<ApiDnsNameservers>($"/tailnet/{Tailnet}/dns/nameservers", ct);

    public Task<bool> SetNameserversAsync(List<string> nameservers, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/dns/nameservers",
            new { dns = nameservers }, ct);

    public Task<ApiResult<ApiDnsSearchPaths>> GetSearchPathsAsync(CancellationToken ct = default) =>
        GetAsync<ApiDnsSearchPaths>($"/tailnet/{Tailnet}/dns/searchpaths", ct);

    public Task<bool> SetSearchPathsAsync(List<string> searchPaths, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/dns/searchpaths",
            new { searchPaths }, ct);

    public Task<ApiResult<ApiDnsPreferences>> GetDnsPreferencesAsync(CancellationToken ct = default) =>
        GetAsync<ApiDnsPreferences>($"/tailnet/{Tailnet}/dns/preferences", ct);

    public Task<bool> SetMagicDnsAsync(bool enabled, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Post, $"/tailnet/{Tailnet}/dns/preferences",
            new { magicDNS = enabled }, ct);

    // ── Policy / ACL ───────────────────────────────────────────────────────────

    public async Task<ApiResult<string>> GetPolicyAsync(CancellationToken ct = default)
    {
        try
        {
            using var request = BuildRequest(HttpMethod.Get, $"/tailnet/{Tailnet}/acl");
            // Override Accept: the API returns HuJSON if Accept includes */*; force JSON
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return ApiResult<string>.Fail($"HTTP {(int)response.StatusCode}");
            var text = await response.Content.ReadAsStringAsync(ct);
            return ApiResult<string>.Ok(text);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return ApiResult<string>.Fail(ex.Message); }
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

    public Task<ApiResult<ApiNetworkLogList>> GetNetworkLogsAsync(DateTime? start = null, CancellationToken ct = default)
    {
        var startParam = (start ?? DateTime.UtcNow.AddHours(-24))
            .ToUniversalTime()
            .ToString("yyyy-MM-ddTHH:mm:ssZ");
        return GetAsync<ApiNetworkLogList>(
            $"/tailnet/{Tailnet}/logs/network?start={Uri.EscapeDataString(startParam)}", ct);
    }

    // ── Auth Keys ──────────────────────────────────────────────────────────────

    public Task<ApiResult<ApiKeyList>> GetKeysAsync(CancellationToken ct = default) =>
        GetAsync<ApiKeyList>($"/tailnet/{Tailnet}/keys", ct);

    public async Task<ApiResult<ApiAuthKey>> CreateKeyAsync(CreateKeyRequest request, CancellationToken ct = default)
    {
        try
        {
            using var req = BuildRequest(HttpMethod.Post, $"/tailnet/{Tailnet}/keys", request);
            using var response = await _http.SendAsync(req, ct);
            if (!response.IsSuccessStatusCode)
                return ApiResult<ApiAuthKey>.Fail($"HTTP {(int)response.StatusCode}");
            var key = await response.Content.ReadFromJsonAsync<ApiAuthKey>(JsonOpts, ct);
            if (key is null)
                return ApiResult<ApiAuthKey>.Fail("API returned an empty response");
            return ApiResult<ApiAuthKey>.Ok(key);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return ApiResult<ApiAuthKey>.Fail(ex.Message); }
    }

    public Task<bool> DeleteKeyAsync(string keyId, CancellationToken ct = default) =>
        SendAsync(HttpMethod.Delete, $"/tailnet/{Tailnet}/keys/{keyId}", null, ct);
}
