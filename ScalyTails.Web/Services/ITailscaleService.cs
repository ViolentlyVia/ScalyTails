using ScalyTails.Web.Models;

namespace ScalyTails.Web.Services;

public interface ITailscaleService
{
    Task<TailscaleStatus?> GetStatusAsync(CancellationToken ct = default);
    Task<TailscalePrefs?> GetPrefsAsync(CancellationToken ct = default);
    Task<CliResult> ConnectAsync(CancellationToken ct = default);
    Task<CliResult> DisconnectAsync(CancellationToken ct = default);
    Task<CliResult> SetExitNodeAsync(string? nodeIP, bool allowLanAccess = false, CancellationToken ct = default);
    Task<CliResult> ClearExitNodeAsync(CancellationToken ct = default);
    Task<CliResult> AdvertiseRoutesAsync(IEnumerable<string> routes, CancellationToken ct = default);
    Task<CliResult> SetAcceptRoutesAsync(bool accept, CancellationToken ct = default);
    Task<CliResult> SetShieldsUpAsync(bool enabled, CancellationToken ct = default);
    Task<CliResult> SetExitNodeAllowLanAsync(bool allow, CancellationToken ct = default);
    Task<CliResult> AdvertiseExitNodeAsync(bool advertise, CancellationToken ct = default);
    Task<CliResult> SetAcceptDnsAsync(bool accept, CancellationToken ct = default);
    Task<CliResult> SetSshServerAsync(bool enabled, CancellationToken ct = default);
    Task<ServeStatus?> GetServeStatusAsync(CancellationToken ct = default);
    Task<CliResult> AddServeAsync(string protocol, int port, string target, CancellationToken ct = default);
    Task<CliResult> RemoveServeAsync(string protocol, int port, CancellationToken ct = default);
    Task<CliResult> ResetServeAsync(CancellationToken ct = default);
    Task<CliResult> SetFunnelAsync(int port, bool enable, string target, CancellationToken ct = default);
    Task<CliResult> SendFileAsync(string filePath, string target, CancellationToken ct = default);
    Task<CliResult> LoginAsync(string? authKey = null, CancellationToken ct = default);
    Task<CliResult> LogoutAsync(CancellationToken ct = default);

    // Diagnostics
    Task<CliResult> NetCheckAsync(CancellationToken ct = default);
    Task<CliResult> PingAsync(string host, int count = 3, CancellationToken ct = default);
    Task<CliResult> WhoisAsync(string ip, CancellationToken ct = default);
    Task<CliResult> BugReportAsync(CancellationToken ct = default);

    // Taildrive
    Task<CliResult> DriveListAsync(CancellationToken ct = default);
    Task<CliResult> DriveShareAsync(string name, string path, CancellationToken ct = default);
    Task<CliResult> DriveUnshareAsync(string name, CancellationToken ct = default);

    // Exit nodes
    Task<CliResult> ExitNodeListAsync(CancellationToken ct = default);
    Task<CliResult> ExitNodeSuggestAsync(CancellationToken ct = default);

    // Update
    Task<CliResult> UpdateCheckAsync(CancellationToken ct = default);
    Task<CliResult> UpdateApplyAsync(CancellationToken ct = default);

    // Account switching
    Task<CliResult> SwitchListAsync(CancellationToken ct = default);
    Task<CliResult> SwitchAccountAsync(string account, CancellationToken ct = default);

    string? TailscalePath { get; }
    bool IsTailscaleInstalled { get; }
}
