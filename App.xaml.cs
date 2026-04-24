using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using ScalyTails.Services;
using ScalyTails.ViewModels;

namespace ScalyTails;

public partial class App : System.Windows.Application
{
    private MainWindow? _mainWindow;
    private NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        PresentationTraceSources.ResourceDictionarySource.Switch.Level = SourceLevels.Critical;

        var service = new TailscaleService();
        var vm = new MainViewModel(service);

        _mainWindow = new MainWindow(vm);

        SetupTrayIcon(vm);

        _mainWindow.Show();
    }

    private void SetupTrayIcon(MainViewModel vm)
    {
        var contextMenu = new ContextMenuStrip();

        var openItem = new ToolStripMenuItem("Open ScalyTails");
        openItem.Font = new Font(openItem.Font, System.Drawing.FontStyle.Bold);
        openItem.Click += (_, _) => ShowMainWindow();

        var connectItem = new ToolStripMenuItem("Connect");
        connectItem.Click += async (_, _) =>
            await vm.ToggleConnectionCommand.ExecuteAsync(null);

        var refreshItem = new ToolStripMenuItem("Refresh Status");
        refreshItem.Click += async (_, _) => await vm.RefreshCommand.ExecuteAsync(null);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            _mainWindow?.ExitApplication();
            _trayIcon?.Dispose();
            Shutdown();
        };

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(connectItem);
        contextMenu.Items.Add(refreshItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "ScalyTails",
            Visible = true,
            ContextMenuStrip = contextMenu,
        };

        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();

        // Update tray icon text on status change
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(MainViewModel.StatusMessage)
                or nameof(MainViewModel.SelfIPs))
            {
                if (_trayIcon is not null)
                {
                    var tip = $"ScalyTails — {vm.StatusMessage}";
                    if (!string.IsNullOrEmpty(vm.SelfIPs))
                        tip += $"\n{vm.SelfIPs}";
                    _trayIcon.Text = tip.Length > 127 ? tip[..127] : tip;
                }

                Dispatcher.InvokeAsync(() =>
                    connectItem.Text = vm.IsRunning ? "Disconnect" : "Connect");
            }
        };
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
