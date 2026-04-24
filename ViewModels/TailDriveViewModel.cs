using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScalyTails.Services;

namespace ScalyTails.ViewModels;

public partial class TailDriveViewModel : ObservableObject
{
    private readonly ITailscaleService _tailscale;

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty] private ObservableCollection<DriveShareEntry> _shares = [];
    [ObservableProperty] private string _newShareName = "";
    [ObservableProperty] private string _newSharePath = "";

    public TailDriveViewModel(ITailscaleService tailscale)
    {
        _tailscale = tailscale;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _tailscale.DriveListAsync();
            Shares.Clear();
            if (result.Success && !string.IsNullOrWhiteSpace(result.Stdout))
            {
                foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
                {
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                        Shares.Add(new DriveShareEntry(parts[0].Trim(), parts[1].Trim()));
                    else if (parts.Length == 1)
                    {
                        var segments = line.Trim().Split(new[] { "  " }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (segments.Length >= 2)
                            Shares.Add(new DriveShareEntry(segments[0].Trim(), segments[1].Trim()));
                    }
                }
            }
            StatusMessage = Shares.Count == 0 ? "No drives shared." : "";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task AddShareAsync()
    {
        var name = NewShareName.Trim();
        var path = NewSharePath.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path)) return;

        IsBusy = true;
        try
        {
            var result = await _tailscale.DriveShareAsync(name, path);
            StatusMessage = result.Success ? $"Shared '{name}'." : $"Error: {result.Stderr}";
            if (result.Success)
            {
                NewShareName = "";
                NewSharePath = "";
                await RefreshAsync();
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task RemoveShareAsync(DriveShareEntry share)
    {
        IsBusy = true;
        try
        {
            var result = await _tailscale.DriveUnshareAsync(share.Name);
            StatusMessage = result.Success ? $"Removed '{share.Name}'." : $"Error: {result.Stderr}";
            if (result.Success)
                await RefreshAsync();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void BrowsePath()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder to share",
            UseDescriptionForTitle = true,
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            NewSharePath = dialog.SelectedPath;
    }
}

public record DriveShareEntry(string Name, string Path);
