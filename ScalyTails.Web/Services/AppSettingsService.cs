using System.IO;
using System.Text.Json;
using ScalyTails.Web.Models;

namespace ScalyTails.Web.Services;

public class AppSettingsService : IAppSettingsService
{
    // %APPDATA%\ScalyTails\settings.json — survives app reinstalls
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScalyTails", "settings.json");

    public AppSettings Settings { get; private set; } = new();
    public bool HasApiKey => !string.IsNullOrWhiteSpace(Settings.ApiKey);
    public event Action? Changed;

    public AppSettingsService() => Load();

    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { }
        // Silently swallow: a missing or corrupted settings file falls back to defaults
        // rather than crashing on startup. The user can re-enter their key in Settings.
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
            Changed?.Invoke();
        }
        catch { }
        // Silently swallow: a disk write failure (permissions, full disk) is not fatal.
    }
}
