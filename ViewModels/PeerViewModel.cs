using CommunityToolkit.Mvvm.ComponentModel;
using ScalyTails.Models;

namespace ScalyTails.ViewModels;

public partial class PeerViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private string _primaryIP = "";
    [ObservableProperty] private string _dnsName = "";
    [ObservableProperty] private string _os = "";
    [ObservableProperty] private bool _online;
    [ObservableProperty] private bool _active;
    [ObservableProperty] private bool _exitNode;
    [ObservableProperty] private bool _exitNodeOption;
    [ObservableProperty] private string _relay = "";
    [ObservableProperty] private long _rxBytes;
    [ObservableProperty] private long _txBytes;
    [ObservableProperty] private string? _lastSeen;
    [ObservableProperty] private List<string> _routes = [];
    [ObservableProperty] private List<string> _tags = [];

    public string NodeKey { get; init; } = "";
    public string NodeID { get; init; } = "";

    public bool HasTags => Tags.Count > 0;
    public string TagsDisplay => string.Join(", ", Tags.Select(t => t.StartsWith("tag:") ? t[4..] : t));

    public string StatusIcon => Online ? "CheckCircle" : "CircleOffOutline";
    public string OsIcon => OS.ToLower() switch
    {
        "windows" => "MicrosoftWindows",
        "linux" => "Linux",
        "darwin" or "macos" => "Apple",
        "android" => "Android",
        "ios" or "iphone" => "AppleIos",
        _ => "Devices"
    };

    public string OS { get; init; } = "";

    public static PeerViewModel FromPeer(string key, TailscalePeer peer) => new()
    {
        NodeKey = key,
        NodeID = peer.ID,
        DisplayName = peer.DisplayName,
        PrimaryIP = peer.PrimaryIP,
        DnsName = peer.ShortDNS,
        Os = peer.OS,
        OS = peer.OS,
        Online = peer.Online,
        Active = peer.Active,
        ExitNode = peer.ExitNode,
        ExitNodeOption = peer.ExitNodeOption,
        Relay = peer.Relay ?? "direct",
        RxBytes = peer.RxBytes,
        TxBytes = peer.TxBytes,
        LastSeen = peer.LastSeen?.ToLocalTime().ToString("g"),
        Routes = peer.PrimaryRoutes ?? [],
        Tags = peer.Tags ?? [],
    };
}
