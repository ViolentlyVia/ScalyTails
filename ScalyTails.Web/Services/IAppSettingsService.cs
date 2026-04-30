using ScalyTails.Web.Models;

namespace ScalyTails.Web.Services;

public interface IAppSettingsService
{
    AppSettings Settings { get; }
    void Save();
    bool HasApiKey { get; }
}
