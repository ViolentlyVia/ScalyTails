using CommunityToolkit.Mvvm.ComponentModel;
using ScalyTails.Models;

namespace ScalyTails.ViewModels;

public partial class ExitNodeViewModel : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _online;

    public string NodeID { get; init; } = "";
    public string HostName { get; init; } = "";
    public string PrimaryIP { get; init; } = "";
    public string DnsName { get; init; } = "";
    public string OS { get; init; } = "";

    public string DisplayName => string.IsNullOrEmpty(HostName) ? DnsName : HostName;

    public string OsIcon => !string.IsNullOrEmpty(OsIconOverride) ? OsIconOverride : OS.ToLower() switch
    {
        "windows" => "MicrosoftWindows",
        "linux" => "Linux",
        "darwin" or "macos" => "Apple",
        "android" => "Android",
        "ios" => "AppleIos",
        _ => "ServerNetwork"
    };

    public bool IsNone { get; init; }

    public static ExitNodeViewModel None() => new()
    {
        IsNone = true,
        HostName = "None (direct)",
        Online = true,
        OsIconOverride = "Close",
    };

    public string OsIconOverride { get; init; } = "";

    public static ExitNodeViewModel FromPeer(TailscalePeer peer, bool isCurrentExitNode) => new()
    {
        NodeID = peer.ID,
        HostName = peer.HostName,
        PrimaryIP = peer.PrimaryIP,
        DnsName = peer.ShortDNS,
        OS = peer.OS,
        Online = peer.Online,
        IsSelected = isCurrentExitNode,
    };
}
