using System.Text.Json.Serialization;

namespace ScalyTails.Models;

public class NetCheckResult
{
    [JsonPropertyName("UDP")]
    public bool UDP { get; set; }

    [JsonPropertyName("IPv4")]
    public string? IPv4 { get; set; }

    [JsonPropertyName("IPv6")]
    public string? IPv6 { get; set; }

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

    public bool HasIPv4 => !string.IsNullOrEmpty(IPv4) || !string.IsNullOrEmpty(GlobalV4);
    public bool HasIPv6 => !string.IsNullOrEmpty(IPv6) || !string.IsNullOrEmpty(GlobalV6);
    public string PublicIPv4 => GlobalV4 ?? IPv4 ?? "—";
    public string PublicIPv6 => GlobalV6 ?? IPv6 ?? "—";

    public IEnumerable<DerpLatency> SortedDerpLatencies()
    {
        if (RegionLatency is null) return [];
        return RegionLatency
            .Select(kv => new DerpLatency(kv.Key, kv.Value / 1_000_000.0))
            .OrderBy(d => d.LatencyMs);
    }
}

public record DerpLatency(string RegionId, double LatencyMs)
{
    public string Display => $"{LatencyMs:F0} ms";
}
