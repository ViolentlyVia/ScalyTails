namespace ScalyTails.Web.Services;

// Implemented by admin ViewModels so MainWindow can trigger HasApiKey re-evaluation on navigation
public interface IApiKeyAware
{
    void OnApiKeyChanged();
}
