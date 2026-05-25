namespace RaceTimerApp.Shared.Models;

/// <summary>
/// Konfiguration für die MAUI App
/// </summary>
public class AppSettings
{
    /// <summary>
    /// App-Modus: "Offline" (nur lokal) oder "Online" (mit Server-Sync)
    /// </summary>
    public string Mode { get; set; } = "Offline";

    /// <summary>
    /// Server URL für optionalen Remote-Sync (z.B. https://raceserver.example.com)
    /// Nur relevant wenn Mode = "Online"
    /// </summary>
    public string? ServerUrl { get; set; }

    /// <summary>
    /// Pfad zur lokalen SQLite-Datenbank
    /// </summary>
    public string DatabasePath { get; set; } = "racetimer.db";

    /// <summary>
    /// Zeitintervall (Sekunden) für Auto-Sync mit Server
    /// </summary>
    public int SyncIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Zeitintervall (Sekunden) für Status-Updates
    /// </summary>
    public int StatusUpdateIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Automatische Migration bei Startversuch
    /// </summary>
    public bool AutoMigrateDatabase { get; set; } = true;
}
