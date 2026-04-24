using System.Windows.Controls;
using ScalyTails.ViewModels;

namespace ScalyTails.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage() => InitializeComponent();

    private void ApiKeyBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
            vm.ApiKey = pb.Password;
    }
}
