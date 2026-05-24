namespace RaceTimerApp.Shared.Models;

/// <summary>
/// Konfiguration für die App
/// </summary>
public class AppSettings
{
    public string Mode { get; set; } = "StandaloneServer"; // StandaloneServer oder RemoteServer
    public string ServerUrl { get; set; } = "http://localhost:5000"; // Für RemoteServer
    public int ServerPort { get; set; } = 5000; // Für StandaloneServer
    public string DatabaseType { get; set; } = "Sqlite"; // Sqlite oder SqlServer
    public string DatabasePath { get; set; } = "Data/racetimer.db"; // Für Sqlite
    public string SqlServerConnectionString { get; set; } = ""; // Für SqlServer
    public int StatusUpdateIntervalSeconds { get; set; } = 60; // Update-Intervall für Zwischenstand
    public bool IsDatabaseMigrationNeeded { get; set; } = false;
}
