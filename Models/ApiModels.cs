using System.Text.Json.Serialization;

namespace ScalyTails.Models;

// ── Devices ──────────────────────────────────────────────────────────────────

public class ApiDeviceList
{
    [JsonPropertyName("devices")]
    public List<ApiDevice> Devices { get; set; } = [];
}

public class ApiDevice
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = "";

    [JsonPropertyName("user")]
    public string User { get; set; } = "";

    [JsonPropertyName("os")]
    public string OS { get; set; } = "";

    [JsonPropertyName("clientVersion")]
    public string ClientVersion { get; set; } = "";

    [JsonPropertyName("updateAvailable")]
    public bool UpdateAvailable { get; set; }

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("lastSeen")]
    public DateTime? LastSeen { get; set; }

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }

    [JsonPropertyName("keyExpiryDisabled")]
    public bool KeyExpiryDisabled { get; set; }

    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("isExternal")]
    public bool IsExternal { get; set; }

    [JsonPropertyName("blocksIncomingConnections")]
    public bool BlocksIncomingConnections { get; set; }

    [JsonPropertyName("addresses")]
    public List<string> Addresses { get; set; } = [];

    [JsonPropertyName("advertisedRoutes")]
    public List<string> AdvertisedRoutes { get; set; } = [];

    [JsonPropertyName("enabledRoutes")]
    public List<string> EnabledRoutes { get; set; } = [];

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }

    [JsonPropertyName("nodeKey")]
    public string NodeKey { get; set; } = "";

    public string PrimaryAddress => Addresses.FirstOrDefault() ?? "";
    public string ShortName => Hostname.Length > 0 ? Hostname : Name.Split('.').FirstOrDefault() ?? Name;
    // ACL tags are stored with a mandatory "tag:" prefix in the API response, but
    // showing "tag:server" vs "server" in the UI is redundant noise.
    public string TagsDisplay => Tags is { Count: > 0 }
        ? string.Join(", ", Tags.Select(t => t.StartsWith("tag:") ? t[4..] : t))
        : "";

    public bool IsExpiringSoon => Expires.HasValue && !KeyExpiryDisabled
        && (Expires.Value - DateTime.UtcNow).TotalDays < 14;

    public bool IsExpired => Expires.HasValue && !KeyExpiryDisabled
        && Expires.Value < DateTime.UtcNow;

    public string ExpiryStatus
    {
        get
        {
            if (KeyExpiryDisabled) return "No expiry";
            if (!Expires.HasValue) return "";
            if (IsExpired) return "Expired";
            var days = (int)(Expires.Value - DateTime.UtcNow).TotalDays;
            return days <= 0 ? "Expired" : $"{days}d";
        }
    }
}

// ── Users ─────────────────────────────────────────────────────────────────────

public class ApiUserList
{
    [JsonPropertyName("users")]
    public List<ApiUser> Users { get; set; } = [];
}

public class ApiUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("loginName")]
    public string LoginName { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("profilePicUrl")]
    public string ProfilePicUrl { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("lastSeen")]
    public DateTime? LastSeen { get; set; }

    [JsonPropertyName("currentlyConnected")]
    public bool CurrentlyConnected { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("deviceCount")]
    public int DeviceCount { get; set; }
}

// ── DNS ───────────────────────────────────────────────────────────────────────

public class ApiDnsNameservers
{
    [JsonPropertyName("dns")]
    public List<string> Dns { get; set; } = [];
}

public class ApiDnsSearchPaths
{
    [JsonPropertyName("searchPaths")]
    public List<string> SearchPaths { get; set; } = [];
}

public class ApiDnsPreferences
{
    [JsonPropertyName("magicDNS")]
    public bool MagicDNS { get; set; }
}

// ── Network Logs ─────────────────────────────────────────────────────────────

public class ApiNetworkLogList
{
    [JsonPropertyName("logs")]
    public List<ApiNetworkLog> Logs { get; set; } = [];
}

public class ApiNetworkLog
{
    [JsonPropertyName("logged")]
    public DateTime? Logged { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("virtualTraffic")]
    public ApiTrafficEntry? VirtualTraffic { get; set; }

    [JsonPropertyName("subnetTraffic")]
    public ApiTrafficEntry? SubnetTraffic { get; set; }

    private ApiTrafficEntry? Traffic => VirtualTraffic ?? SubnetTraffic;
    public string TrafficSrc => Traffic?.Src ?? "";
    public string TrafficDst => Traffic?.Dst ?? "";
}

public class ApiTrafficEntry
{
    [JsonPropertyName("src")]
    public string Src { get; set; } = "";

    [JsonPropertyName("dst")]
    public string Dst { get; set; } = "";

    [JsonPropertyName("proto")]
    public int Proto { get; set; }

    [JsonPropertyName("rxBytes")]
    public long RxBytes { get; set; }

    [JsonPropertyName("txBytes")]
    public long TxBytes { get; set; }
}

// ── Auth Keys ─────────────────────────────────────────────────────────────────

public class ApiKeyList
{
    [JsonPropertyName("keys")]
    public List<ApiAuthKey> Keys { get; set; } = [];
}

public class ApiAuthKey
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }

    [JsonPropertyName("expires")]
    public DateTime? Expires { get; set; }

    [JsonPropertyName("revoked")]
    public DateTime? Revoked { get; set; }

    [JsonPropertyName("invalid")]
    public bool Invalid { get; set; }

    [JsonPropertyName("capabilities")]
    public ApiKeyCapabilities? Capabilities { get; set; }

    public bool IsActive => Revoked is null && !Invalid && (Expires is null || Expires > DateTime.UtcNow);
    public string StatusDisplay => !IsActive ? "Revoked/Expired" : "Active";
    public string ExpiryDisplay => Expires.HasValue
        ? Expires.Value.ToLocalTime().ToString("yyyy-MM-dd")
        : "Never";
}

public class ApiKeyCapabilities
{
    [JsonPropertyName("devices")]
    public ApiKeyDeviceCapabilities? Devices { get; set; }
}

public class ApiKeyDeviceCapabilities
{
    [JsonPropertyName("create")]
    public ApiKeyCreateParams? Create { get; set; }
}

public class ApiKeyCreateParams
{
    [JsonPropertyName("reusable")]
    public bool Reusable { get; set; }

    [JsonPropertyName("ephemeral")]
    public bool Ephemeral { get; set; }

    [JsonPropertyName("preauthorized")]
    public bool Preauthorized { get; set; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; set; }
}

// ── Create Key Request ────────────────────────────────────────────────────────

public class CreateKeyRequest
{
    [JsonPropertyName("capabilities")]
    public ApiKeyCapabilities Capabilities { get; set; } = new();

    // 90 days (7,776,000 s) is the maximum the Tailscale API accepts for auth key expiry.
    // Sending a larger value returns HTTP 400.
    [JsonPropertyName("expirySeconds")]
    public int ExpirySeconds { get; set; } = 7776000;

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
}
