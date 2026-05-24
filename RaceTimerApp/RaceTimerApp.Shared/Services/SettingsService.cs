using RaceTimerApp.Shared.Models;
using System.Net.Http.Json;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für App-Konfiguration und Einstellungen
/// </summary>
public class SettingsService
{
    private AppSettings _settings;
    private readonly string _settingsPath;

    public SettingsService()
    {
        _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RaceTimer",
            "settings.json");

        _settings = LoadSettings();
    }

    public AppSettings GetSettings() => _settings;

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _settings = settings;

        var directory = Path.GetDirectoryName(_settingsPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = System.Text.Json.JsonSerializer.Serialize(_settings, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_settingsPath, json);
    }

    private AppSettings LoadSettings()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    // Verbindung zum Server testen
    public async Task<bool> TestServerConnectionAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.GetAsync($"{url}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Datenbankverbindung testen
    public async Task<bool> TestDatabaseConnectionAsync(AppSettings settings)
    {
        try
        {
            // Dies würde einen API-Endpoint benötigen
            // POST /api/health/database
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await client.PostAsJsonAsync(
                $"{settings.ServerUrl}/api/health/database",
                settings);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Datenbankmigrationen ausführen
    public async Task<bool> RunDatabaseMigrationAsync(AppSettings settings)
    {
        try
        {
            // POST /api/health/migrate
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var response = await client.PostAsJsonAsync(
                $"{settings.ServerUrl}/api/health/migrate",
                new { });
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
