using System.IO;
using System.Text.Json;
using ScalyTails.Models;

namespace ScalyTails.Services;

public class AppSettingsService : IAppSettingsService
{
    // %APPDATA%\ScalyTails\settings.json — survives app reinstalls
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ScalyTails", "settings.json");

    public AppSettings Settings { get; private set; } = new();
    public bool HasApiKey => !string.IsNullOrWhiteSpace(Settings.ApiKey);

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
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
