using ScalyTails.Models;

namespace ScalyTails.Services;

public interface ITailscaleApiService
{
    bool IsConfigured { get; }

    // Devices
    Task<ApiDeviceList?> GetDevicesAsync(CancellationToken ct = default);
    Task<ApiDevice?> GetDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<bool> AuthorizeDeviceAsync(string deviceId, bool authorized, CancellationToken ct = default);
    Task<bool> ExpireDeviceKeyAsync(string deviceId, CancellationToken ct = default);
    Task<bool> DeleteDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<bool> SetDeviceTagsAsync(string deviceId, List<string> tags, CancellationToken ct = default);
    Task<bool> SetDeviceRoutesAsync(string deviceId, List<string> routes, CancellationToken ct = default);

    // Users
    Task<ApiUserList?> GetUsersAsync(CancellationToken ct = default);

    // DNS
    Task<ApiDnsNameservers?> GetNameserversAsync(CancellationToken ct = default);
    Task<bool> SetNameserversAsync(List<string> nameservers, CancellationToken ct = default);
    Task<ApiDnsSearchPaths?> GetSearchPathsAsync(CancellationToken ct = default);
    Task<bool> SetSearchPathsAsync(List<string> searchPaths, CancellationToken ct = default);
    Task<ApiDnsPreferences?> GetDnsPreferencesAsync(CancellationToken ct = default);
    Task<bool> SetMagicDnsAsync(bool enabled, CancellationToken ct = default);

    // Policy / ACL
    Task<string?> GetPolicyAsync(CancellationToken ct = default);
    Task<bool> SetPolicyAsync(string policyJson, CancellationToken ct = default);

    // Logs
    Task<ApiNetworkLogList?> GetNetworkLogsAsync(DateTime? start = null, CancellationToken ct = default);

    // Auth Keys
    Task<ApiKeyList?> GetKeysAsync(CancellationToken ct = default);
    Task<ApiAuthKey?> CreateKeyAsync(CreateKeyRequest request, CancellationToken ct = default);
    Task<bool> DeleteKeyAsync(string keyId, CancellationToken ct = default);
}
