using CommunityToolkit.Mvvm.ComponentModel;
using ScalyTails.Models;

namespace ScalyTails.ViewModels;

public partial class ServeEntryViewModel : ObservableObject
{
    [ObservableProperty] private bool _funnelEnabled;

    public string Protocol { get; init; } = "";
    public string Port { get; init; } = "";
    public string Path { get; init; } = "";
    public string Target { get; init; } = "";
    public string HostPort { get; init; } = "";

    public string DisplayLabel => (string.IsNullOrEmpty(Path) || Path == "/")
        ? $":{Port} → {Target}"
        : $":{Port}{Path} → {Target}";

    public static ServeEntryViewModel FromEntry(ServeEntry e) => new()
    {
        Protocol = e.Protocol,
        Port = e.Port,
        Path = e.Path,
        Target = e.Target,
        FunnelEnabled = e.FunnelEnabled,
        HostPort = e.HostPort,
    };
}
