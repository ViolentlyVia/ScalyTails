using System.Windows;
using System.Windows.Controls;
using ScalyTails.Services;
using ScalyTails.ViewModels;
using ScalyTails.Views;

namespace ScalyTails;

public partial class MainWindow : Window
{
    // Pages are pre-allocated so switching between them is instant and their state
    // (scroll position, loaded data) persists for the session.
    private readonly OverviewPage _overviewPage = new();
    private readonly PeersPage _peersPage = new();
    private readonly ExitNodesPage _exitNodesPage = new();
    private readonly SubnetRoutesPage _subnetRoutesPage = new();
    private readonly ServePage _servePage = new();
    private readonly DiagnosticsPage _diagnosticsPage = new();
    private readonly TailDrivePage _tailDrivePage = new();
    private readonly SettingsPage _settingsPage = new();
    private readonly DnsPage _dnsPage = new();
    private readonly DevicesAdminPage _devicesPage = new();
    private readonly UsersPage _usersPage = new();
    private readonly PolicyPage _policyPage = new();
    private readonly LogsPage _logsPage = new();
    private readonly KeysPage _keysPage = new();

    private bool _closeToTray = true;

    public MainWindow(MainViewModel mainVm, ITailscaleService cliService,
                      IAppSettingsService settingsService, ITailscaleApiService apiService)
    {
        InitializeComponent();

        var diagVm     = new DiagnosticsViewModel(cliService);
        var driveVm    = new TailDriveViewModel(cliService);
        var settingsVm = new SettingsViewModel(cliService, settingsService);
        var dnsVm      = new DnsViewModel(apiService);
        var devicesVm  = new DevicesAdminViewModel(apiService);
        var usersVm    = new UsersViewModel(apiService);
        var policyVm   = new PolicyViewModel(apiService);
        var logsVm     = new LogsViewModel(apiService);
        var keysVm     = new KeysViewModel(apiService);

        DataContext = mainVm;

        _overviewPage.DataContext    = mainVm;
        _peersPage.DataContext       = mainVm;
        _exitNodesPage.DataContext   = mainVm;
        _subnetRoutesPage.DataContext = mainVm;
        _servePage.DataContext       = mainVm;
        _diagnosticsPage.DataContext = diagVm;
        _tailDrivePage.DataContext   = driveVm;
        _settingsPage.DataContext    = settingsVm;
        _dnsPage.DataContext         = dnsVm;
        _devicesPage.DataContext     = devicesVm;
        _usersPage.DataContext       = usersVm;
        _policyPage.DataContext      = policyVm;
        _logsPage.DataContext        = logsVm;
        _keysPage.DataContext        = keysVm;

        PageHost.Content = _overviewPage;

        mainVm.StartRefresh();
    }

    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn) return;
        var page = btn.Tag?.ToString() switch
        {
            "Overview"     => (object)_overviewPage,
            "Peers"        => _peersPage,
            "ExitNodes"    => _exitNodesPage,
            "SubnetRoutes" => _subnetRoutesPage,
            "Serve"        => _servePage,
            "Diagnostics"  => _diagnosticsPage,
            "TailDrive"    => _tailDrivePage,
            "Settings"     => _settingsPage,
            "DNS"          => _dnsPage,
            "Devices"      => _devicesPage,
            "Users"        => _usersPage,
            "Policy"       => _policyPage,
            "Logs"         => _logsPage,
            "Keys"         => _keysPage,
            _              => _overviewPage
        };
        PageHost.Content = page;
        // Admin pages (DNS, Devices, Users, etc.) don't auto-load on startup — they
        // wait until the user navigates to them. OnApiKeyChanged() re-evaluates HasApiKey
        // so the "No API key" banner updates immediately when a key is saved in Settings.
        if (page is FrameworkElement { DataContext: IApiKeyAware aware })
            aware.OnApiKeyChanged();
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
        // Disable the tray-minimize guard before closing so Window_Closing doesn't cancel it
        _closeToTray = false;
        if (DataContext is MainViewModel vm)
            vm.StopRefresh();
        Close();
    }
}
