using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using ScalyTails.ViewModels;

namespace ScalyTails.Views;

public partial class PeersPage : UserControl
{
    public PeersPage() => InitializeComponent();

    private void SshButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PeerViewModel peer }) return;
        if (DataContext is not MainViewModel vm) return;
        vm.SshToPeerCommand.Execute(peer);
    }

    private async void SendFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PeerViewModel peer }) return;
        if (DataContext is not MainViewModel vm) return;

        var dialog = new OpenFileDialog
        {
            Title = $"Send file to {peer.DisplayName}",
            Multiselect = false,
        };

        if (dialog.ShowDialog() != true) return;

        await vm.SendFileToPeerAsync(peer, dialog.FileName);
    }
}
