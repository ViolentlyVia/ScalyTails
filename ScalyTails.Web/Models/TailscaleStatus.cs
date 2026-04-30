using System.Text.Json.Serialization;

namespace ScalyTails.Web.Models;

public class TailscaleStatus
{
    [JsonPropertyName("Version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("BackendState")]
    public string BackendState { get; set; } = "";

    [JsonPropertyName("AuthURL")]
    public string? AuthURL { get; set; }

    [JsonPropertyName("TailscaleIPs")]
    public List<string> TailscaleIPs { get; set; } = [];

    [JsonPropertyName("Self")]
    public TailscalePeer? Self { get; set; }

    [JsonPropertyName("Peer")]
    public Dictionary<string, TailscalePeer>? Peer { get; set; }

    [JsonPropertyName("CurrentTailnet")]
    public TailnetInfo? CurrentTailnet { get; set; }

    [JsonPropertyName("MagicDNSSuffix")]
    public string? MagicDNSSuffix { get; set; }

    [JsonPropertyName("CertDomains")]
    public List<string>? CertDomains { get; set; }

    // "Starting" is treated as running so the UI shows a connected state during the
    // brief transition period rather than flickering to "Disconnected".
    public bool IsRunning => BackendState is "Running" or "Starting";
    public bool NeedsLogin => BackendState == "NeedsLogin";

    // The Peer dictionary is entirely absent (not just empty) when Tailscale is
    // disconnected, so the null-coalescing guard is load-bearing.
    public IEnumerable<TailscalePeer> AllPeers =>
        Peer?.Values ?? Enumerable.Empty<TailscalePeer>();

    public TailscalePeer? ActiveExitNode =>
        Peer?.Values.FirstOrDefault(p => p.ExitNode);
}

public class TailscalePeer
{
    [JsonPropertyName("ID")]
    public string ID { get; set; } = "";

    [JsonPropertyName("PublicKey")]
    public string PublicKey { get; set; } = "";

    [JsonPropertyName("HostName")]
    public string HostName { get; set; } = "";

    [JsonPropertyName("DNSName")]
    public string DNSName { get; set; } = "";

    [JsonPropertyName("OS")]
    public string OS { get; set; } = "";

    [JsonPropertyName("UserID")]
    public long UserID { get; set; }

    [JsonPropertyName("TailscaleIPs")]
    public List<string>? TailscaleIPs { get; set; }

    [JsonPropertyName("AllowedIPs")]
    public List<string>? AllowedIPs { get; set; }

    [JsonPropertyName("PrimaryRoutes")]
    public List<string>? PrimaryRoutes { get; set; }

    [JsonPropertyName("Addrs")]
    public List<string>? Addrs { get; set; }

    [JsonPropertyName("Online")]
    public bool Online { get; set; }

    [JsonPropertyName("Active")]
    public bool Active { get; set; }

    [JsonPropertyName("ExitNode")]
    public bool ExitNode { get; set; }

    [JsonPropertyName("ExitNodeOption")]
    public bool ExitNodeOption { get; set; }

    [JsonPropertyName("Relay")]
    public string? Relay { get; set; }

    [JsonPropertyName("RxBytes")]
    public long RxBytes { get; set; }

    [JsonPropertyName("TxBytes")]
    public long TxBytes { get; set; }

    [JsonPropertyName("Created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("LastSeen")]
    public DateTime? LastSeen { get; set; }

    [JsonPropertyName("LastWrite")]
    public DateTime? LastWrite { get; set; }

    [JsonPropertyName("Tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("Capabilities")]
    public List<string>? Capabilities { get; set; }

    [JsonPropertyName("InNetworkMap")]
    public bool InNetworkMap { get; set; }

    [JsonPropertyName("InMagicSock")]
    public bool InMagicSock { get; set; }

    [JsonPropertyName("InEngine")]
    public bool InEngine { get; set; }

    [JsonPropertyName("AdvertisedRoutes")]
    public List<string>? AdvertisedRoutes { get; set; }

    public string PrimaryIP => TailscaleIPs?.FirstOrDefault() ?? "";
    public string ShortDNS => DNSName.TrimEnd('.');
    public string DisplayName => string.IsNullOrEmpty(HostName) ? ShortDNS : HostName;
    public string PingDisplayName => PrimaryIP.Length > 0 ? $"{DisplayName}  —  {PrimaryIP}" : DisplayName;
}

public class TailnetInfo
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("MagicDNSSuffix")]
    public string MagicDNSSuffix { get; set; } = "";

    [JsonPropertyName("MagicDNSEnabled")]
    public bool MagicDNSEnabled { get; set; }
}
