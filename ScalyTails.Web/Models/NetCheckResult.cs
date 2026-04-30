using System.Text.Json.Serialization;

namespace ScalyTails.Web.Models;

public class NetCheckResult
{
    [JsonPropertyName("UDP")]
    public bool UDP { get; set; }

    [JsonPropertyName("IPv4")]
    public bool IPv4 { get; set; }

    [JsonPropertyName("IPv6")]
    public bool IPv6 { get; set; }

    [JsonPropertyName("MappingVariesByDestIP")]
    public bool? MappingVariesByDestIP { get; set; }

    [JsonPropertyName("HairPinning")]
    public bool? HairPinning { get; set; }

    [JsonPropertyName("UPnP")]
    public bool? UPnP { get; set; }

    [JsonPropertyName("PMP")]
    public bool? PMP { get; set; }

    [JsonPropertyName("PCP")]
    public bool? PCP { get; set; }

    [JsonPropertyName("PreferredDERP")]
    public int PreferredDERP { get; set; }

    [JsonPropertyName("RegionLatency")]
    public Dictionary<string, long>? RegionLatency { get; set; }

    [JsonPropertyName("GlobalV4")]
    public string? GlobalV4 { get; set; }

    [JsonPropertyName("GlobalV6")]
    public string? GlobalV6 { get; set; }

    public bool HasIPv4 => IPv4 || !string.IsNullOrEmpty(GlobalV4);
    public bool HasIPv6 => IPv6 || !string.IsNullOrEmpty(GlobalV6);
    // GlobalV4 is serialized as "ip:port" (Go's netip.AddrPort); strip the port suffix
    public string PublicIPv4 => string.IsNullOrEmpty(GlobalV4) ? (IPv4 ? "Available" : "—") : GlobalV4.Split(':')[0];
    public string PublicIPv6 => string.IsNullOrEmpty(GlobalV6) ? (IPv6 ? "Available" : "—") : GlobalV6;

    public IEnumerable<DerpLatency> SortedDerpLatencies()
    {
        if (RegionLatency is null) return [];
        return RegionLatency
            // RegionLatency values are nanoseconds (Go's time.Duration); convert to ms
            .Select(kv => new DerpLatency(kv.Key, kv.Value / 1_000_000.0))
            .OrderBy(d => d.LatencyMs);
    }
}

public record DerpLatency(string RegionId, double LatencyMs)
{
    public string Display => $"{LatencyMs:F0} ms";
}
