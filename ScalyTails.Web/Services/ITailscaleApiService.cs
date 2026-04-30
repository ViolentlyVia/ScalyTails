using ScalyTails.Web.Models;

namespace ScalyTails.Web.Services;

public interface ITailscaleApiService
{
    bool IsConfigured { get; }

    // Devices
    Task<ApiResult<ApiDeviceList>>  GetDevicesAsync(CancellationToken ct = default);
    Task<ApiResult<ApiDevice>>      GetDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<bool> AuthorizeDeviceAsync(string deviceId, bool authorized, CancellationToken ct = default);
    Task<bool> ExpireDeviceKeyAsync(string deviceId, CancellationToken ct = default);
    Task<bool> DeleteDeviceAsync(string deviceId, CancellationToken ct = default);
    Task<bool> SetDeviceTagsAsync(string deviceId, List<string> tags, CancellationToken ct = default);
    Task<bool> SetDeviceRoutesAsync(string deviceId, List<string> routes, CancellationToken ct = default);

    // Users
    Task<ApiResult<ApiUserList>> GetUsersAsync(CancellationToken ct = default);
    Task<bool> UpdateUserRoleAsync(string userId, string role, CancellationToken ct = default);
    Task<bool> SuspendUserAsync(string userId, CancellationToken ct = default);
    Task<bool> RestoreUserAsync(string userId, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(string userId, CancellationToken ct = default);

    // User invites
    Task<ApiResult<ApiUserInviteList>> GetUserInvitesAsync(CancellationToken ct = default);
    Task<bool> CreateUserInviteAsync(string email, string role, CancellationToken ct = default);
    Task<bool> DeleteUserInviteAsync(string inviteId, CancellationToken ct = default);
    Task<bool> ResendUserInviteAsync(string inviteId, CancellationToken ct = default);

    // DNS
    Task<ApiResult<ApiDnsNameservers>>  GetNameserversAsync(CancellationToken ct = default);
    Task<bool>                          SetNameserversAsync(List<string> nameservers, CancellationToken ct = default);
    Task<ApiResult<ApiDnsSearchPaths>>  GetSearchPathsAsync(CancellationToken ct = default);
    Task<bool>                          SetSearchPathsAsync(List<string> searchPaths, CancellationToken ct = default);
    Task<ApiResult<ApiDnsPreferences>>  GetDnsPreferencesAsync(CancellationToken ct = default);
    Task<bool>                          SetMagicDnsAsync(bool enabled, CancellationToken ct = default);

    // Policy / ACL
    Task<ApiResult<string>> GetPolicyAsync(CancellationToken ct = default);
    Task<bool>              SetPolicyAsync(string policyJson, CancellationToken ct = default);

    // Logs
    Task<ApiResult<ApiNetworkLogList>> GetNetworkLogsAsync(DateTime? start = null, CancellationToken ct = default);

    // Auth Keys
    Task<ApiResult<ApiKeyList>>  GetKeysAsync(CancellationToken ct = default);
    Task<ApiResult<ApiAuthKey>>  CreateKeyAsync(CreateKeyRequest request, CancellationToken ct = default);
    Task<bool>                   DeleteKeyAsync(string keyId, CancellationToken ct = default);
}
