using CommunityToolkit.Mvvm.ComponentModel;

namespace ScalyTails.ViewModels;

public partial class SubnetRouteViewModel : ObservableObject
{
    [ObservableProperty] private string _cidr = "";
    [ObservableProperty] private bool _isAdvertised;
    [ObservableProperty] private string _source = "";

    public SubnetRouteViewModel() { }

    public SubnetRouteViewModel(string cidr, bool advertised = true, string source = "local")
    {
        Cidr = cidr;
        IsAdvertised = advertised;
        Source = source;
    }
}
