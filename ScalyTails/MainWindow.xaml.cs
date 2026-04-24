using System.Windows;
using System.Windows.Controls;
using ScalyTails.ViewModels;
using ScalyTails.Views;

namespace ScalyTails;

public partial class MainWindow : Window
{
    private readonly OverviewPage _overviewPage = new();
    private readonly PeersPage _peersPage = new();
    private readonly ExitNodesPage _exitNodesPage = new();
    private readonly SubnetRoutesPage _subnetRoutesPage = new();
    private readonly ServePage _servePage = new();

    private bool _closeToTray = true;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;

        _overviewPage.DataContext = vm;
        _peersPage.DataContext = vm;
        _exitNodesPage.DataContext = vm;
        _subnetRoutesPage.DataContext = vm;
        _servePage.DataContext = vm;

        PageHost.Content = _overviewPage;

        vm.StartRefresh();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn) return;
        PageHost.Content = btn.Tag?.ToString() switch
        {
            "Overview" => _overviewPage,
            "Peers" => _peersPage,
            "ExitNodes" => _exitNodesPage,
            "SubnetRoutes" => _subnetRoutesPage,
            "Serve" => _servePage,
            _ => _overviewPage
        };
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && _closeToTray)
            Hide();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_closeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ExitApplication()
    {
        _closeToTray = false;
        if (DataContext is MainViewModel vm)
            vm.StopRefresh();
        Close();
    }
}
