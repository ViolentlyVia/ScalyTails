using ScalyTails.Models;

namespace ScalyTails.Services;

public interface IAppSettingsService
{
    AppSettings Settings { get; }
    void Save();
    bool HasApiKey { get; }
}
