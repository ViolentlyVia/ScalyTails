using System.Diagnostics;
using System.IO;
using System.Text.Json;
using ScalyTails.Models;

namespace ScalyTails.Services;

public class TailscaleService : ITailscaleService
{
    private static readonly string[] SearchPaths =
    [
        @"C:\Program Files\Tailscale\tailscale.exe",
        @"C:\Program Files (x86)\Tailscale\tailscale.exe",
    ];

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public string? TailscalePath { get; } = FindTailscale();
    public bool IsTailscaleInstalled => TailscalePath is not null;

    private static string? FindTailscale()
    {
        foreach (var path in SearchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        try
        {
            using var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tailscale",
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            probe.Start();
            probe.WaitForExit(3000);
            return probe.ExitCode == 0 ? "tailscale" : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<CliResult> RunAsync(string arguments, CancellationToken ct = default)
    {
        if (TailscalePath is null)
            return new CliResult("", "Tailscale not found", 1);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = TailscalePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return new CliResult(stdout.Trim(), stderr.Trim(), process.ExitCode);
    }

    public async Task<TailscaleStatus?> GetStatusAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("status --json --peers", ct);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TailscaleStatus>(result.Stdout, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<TailscalePrefs?> GetPrefsAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("debug prefs", ct);
        if (!result.Success || string.IsNullOrWhiteSpace(result.Stdout))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TailscalePrefs>(result.Stdout, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public Task<CliResult> ConnectAsync(CancellationToken ct = default) =>
        RunAsync("up", ct);

    public Task<CliResult> DisconnectAsync(CancellationToken ct = default) =>
        RunAsync("down", ct);

    public Task<CliResult> SetExitNodeAsync(string? nodeIP, bool allowLanAccess = false, CancellationToken ct = default)
    {
        var args = $"set --exit-node={nodeIP}";
        if (allowLanAccess)
            args += " --exit-node-allow-lan-access";
        return RunAsync(args, ct);
    }

    public Task<CliResult> ClearExitNodeAsync(CancellationToken ct = default) =>
        RunAsync("set --exit-node=", ct);

    public Task<CliResult> AdvertiseRoutesAsync(IEnumerable<string> routes, CancellationToken ct = default)
    {
        var routeList = string.Join(",", routes);
        return RunAsync($"set --advertise-routes={routeList}", ct);
    }

    public Task<CliResult> SetAcceptRoutesAsync(bool accept, CancellationToken ct = default) =>
        RunAsync(accept ? "set --accept-routes" : "set --accept-routes=false", ct);

    public Task<CliResult> SetShieldsUpAsync(bool enabled, CancellationToken ct = default) =>
        RunAsync(enabled ? "set --shields-up" : "set --shields-up=false", ct);

    public Task<CliResult> SetExitNodeAllowLanAsync(bool allow, CancellationToken ct = default) =>
        RunAsync(allow ? "set --exit-node-allow-lan-access" : "set --exit-node-allow-lan-access=false", ct);

    public Task<CliResult> AdvertiseExitNodeAsync(bool advertise, CancellationToken ct = default) =>
        RunAsync(advertise ? "set --advertise-exit-node" : "set --advertise-exit-node=false", ct);

    public Task<CliResult> SetAcceptDnsAsync(bool accept, CancellationToken ct = default) =>
        RunAsync(accept ? "set --accept-dns" : "set --accept-dns=false", ct);

    public Task<CliResult> SetSshServerAsync(bool enabled, CancellationToken ct = default) =>
        RunAsync(enabled ? "set --ssh" : "set --ssh=false", ct);

    public async Task<ServeStatus?> GetServeStatusAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("serve status --json", ct);
        if (string.IsNullOrWhiteSpace(result.Stdout))
            return new ServeStatus();

        try
        {
            return JsonSerializer.Deserialize<ServeStatus>(result.Stdout, JsonOpts) ?? new ServeStatus();
        }
        catch
        {
            return new ServeStatus();
        }
    }

    public Task<CliResult> AddServeAsync(string protocol, int port, string target, CancellationToken ct = default)
    {
        var flag = protocol switch
        {
            "HTTPS Funnel" => $"funnel --https={port} {target}",
            "HTTP"         => $"serve --http={port} {target}",
            _              => $"serve --https={port} {target}",
        };
        return RunAsync(flag, ct);
    }

    public Task<CliResult> RemoveServeAsync(string protocol, int port, CancellationToken ct = default)
    {
        var flag = protocol switch
        {
            "HTTP" => $"serve --http={port} off",
            "TCP"  => $"serve --tcp={port} off",
            _      => $"serve --https={port} off",
        };
        return RunAsync(flag, ct);
    }

    public Task<CliResult> ResetServeAsync(CancellationToken ct = default) =>
        RunAsync("serve reset", ct);

    public Task<CliResult> SetFunnelAsync(int port, bool enable, string target, CancellationToken ct = default) =>
        enable
            ? RunAsync($"funnel --https={port} {target}", ct)
            : RunAsync($"serve --https={port} {target}", ct);

    public Task<CliResult> SendFileAsync(string filePath, string target, CancellationToken ct = default) =>
        RunAsync($"file cp \"{filePath}\" {target}:", ct);

    public Task<CliResult> LoginAsync(string? authKey = null, CancellationToken ct = default)
    {
        var args = "up";
        if (!string.IsNullOrWhiteSpace(authKey))
            args += $" --auth-key={authKey}";
        return RunAsync(args, ct);
    }

    public Task<CliResult> LogoutAsync(CancellationToken ct = default) =>
        RunAsync("logout", ct);

    // ── Diagnostics ───────────────────────────────────────────────────────────

    public Task<CliResult> NetCheckAsync(CancellationToken ct = default) =>
        RunAsync("netcheck --format json", ct);

    public Task<CliResult> PingAsync(string host, int count = 3, CancellationToken ct = default) =>
        RunAsync($"ping --c {count} --timeout 5s {host}", ct);

    public Task<CliResult> WhoisAsync(string ip, CancellationToken ct = default) =>
        RunAsync($"whois {ip}", ct);

    public Task<CliResult> BugReportAsync(CancellationToken ct = default) =>
        RunAsync("bugreport", ct);

    // ── Taildrive ─────────────────────────────────────────────────────────────

    public Task<CliResult> DriveListAsync(CancellationToken ct = default) =>
        RunAsync("drive list", ct);

    public Task<CliResult> DriveShareAsync(string name, string path, CancellationToken ct = default) =>
        RunAsync($"drive share \"{name}\" \"{path}\"", ct);

    public Task<CliResult> DriveUnshareAsync(string name, CancellationToken ct = default) =>
        RunAsync($"drive unshare \"{name}\"", ct);

    // ── Exit Nodes ────────────────────────────────────────────────────────────

    public Task<CliResult> ExitNodeListAsync(CancellationToken ct = default) =>
        RunAsync("exit-node list", ct);

    public Task<CliResult> ExitNodeSuggestAsync(CancellationToken ct = default) =>
        RunAsync("exit-node suggest", ct);

    // ── Update ────────────────────────────────────────────────────────────────

    public Task<CliResult> UpdateCheckAsync(CancellationToken ct = default) =>
        RunAsync("update --dry-run", ct);

    public Task<CliResult> UpdateApplyAsync(CancellationToken ct = default) =>
        RunAsync("update", ct);

    // ── Account Switching ─────────────────────────────────────────────────────

    public Task<CliResult> SwitchListAsync(CancellationToken ct = default) =>
        RunAsync("switch --list", ct);

    public Task<CliResult> SwitchAccountAsync(string account, CancellationToken ct = default) =>
        RunAsync($"switch {account}", ct);
}
