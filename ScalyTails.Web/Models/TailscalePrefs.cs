using System.Text.Json.Serialization;

namespace ScalyTails.Web.Models;

public class TailscalePrefs
{
    [JsonPropertyName("ControlURL")]
    public string ControlURL { get; set; } = "";

    [JsonPropertyName("RouteAll")]
    public bool RouteAll { get; set; }

    [JsonPropertyName("AllowSingleHosts")]
    public bool AllowSingleHosts { get; set; }

    [JsonPropertyName("ExitNodeID")]
    public string? ExitNodeID { get; set; }

    [JsonPropertyName("ExitNodeIP")]
    public string? ExitNodeIP { get; set; }

    [JsonPropertyName("ExitNodeAllowLANAccess")]
    public bool ExitNodeAllowLANAccess { get; set; }

    [JsonPropertyName("CorpDNS")]
    public bool CorpDNS { get; set; }

    [JsonPropertyName("WantRunning")]
    public bool WantRunning { get; set; }

    [JsonPropertyName("LoggedOut")]
    public bool LoggedOut { get; set; }

    [JsonPropertyName("ShieldsUp")]
    public bool ShieldsUp { get; set; }

    [JsonPropertyName("AdvertiseTags")]
    public List<string>? AdvertiseTags { get; set; }

    [JsonPropertyName("Hostname")]
    public string? Hostname { get; set; }

    [JsonPropertyName("AdvertiseRoutes")]
    public List<string>? AdvertiseRoutes { get; set; }

    [JsonPropertyName("NoSNAT")]
    public bool NoSNAT { get; set; }

    [JsonPropertyName("NetfilterMode")]
    public int NetfilterMode { get; set; }

    [JsonPropertyName("RunSSH")]
    public bool RunSSH { get; set; }

    [JsonPropertyName("AutoUpdate")]
    public AutoUpdatePrefs? AutoUpdate { get; set; }

    public bool HasExitNode =>
        !string.IsNullOrEmpty(ExitNodeID) || !string.IsNullOrEmpty(ExitNodeIP);

    public string AdvertisedRoutesDisplay =>
        AdvertiseRoutes is { Count: > 0 }
            ? string.Join(", ", AdvertiseRoutes)
            : "None";
}

public class AutoUpdatePrefs
{
    [JsonPropertyName("Check")]
    public bool Check { get; set; }

    [JsonPropertyName("Apply")]
    public bool? Apply { get; set; }
}
