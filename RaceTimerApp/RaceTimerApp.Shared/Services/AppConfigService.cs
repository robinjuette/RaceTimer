using RaceTimer.Shared.Services;
using RaceTimerApp.Shared.Models;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für App-Konfiguration und Server-Verbindungsverwaltung
/// </summary>
public class AppConfigService
{
    private SignalRSyncService? _signalRSync;
    private AppSettings _settings;
    private readonly Action<AppSettings>? _savePersistence;
    private readonly Func<AppSettings>? _loadPersistence;

    public AppConfigService(Action<AppSettings>? savePersistence = null, Func<AppSettings>? loadPersistence = null)
    {
        _savePersistence = savePersistence;
        _loadPersistence = loadPersistence;
        _settings = _loadPersistence?.Invoke() ?? new AppSettings();
    }

    /// <summary>
    /// Aktuelle App-Einstellungen abrufen
    /// </summary>
    public AppSettings Settings => _settings;

    /// <summary>
    /// Gibt an, ob die App mit einem Server verbunden ist
    /// </summary>
    public bool IsServerConnected => _signalRSync?.IsConnected ?? false;

    // ===== Konfiguration =====

    /// <summary>
    /// Einstellungen speichern
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        _savePersistence?.Invoke(settings);
    }

    // ===== Server-Verbindung =====

    /// <summary>
    /// Mit optionalem Server verbinden
    /// </summary>
    public async Task<bool> ConnectToServerAsync(string serverUrl)
    {
        if (string.IsNullOrEmpty(serverUrl))
            return false;

        try
        {
            _signalRSync = new SignalRSyncService(serverUrl);
            var connected = await _signalRSync.ConnectAsync();

            if (connected)
            {
                _settings.ServerUrl = serverUrl;
                _settings.Mode = "Online";
                SaveSettings(_settings);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to connect to server: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Von Server trennen
    /// </summary>
    public async Task DisconnectFromServerAsync()
    {
        if (_signalRSync != null)
        {
            await _signalRSync.DisconnectAsync();
            _signalRSync = null;
        }

        _settings.Mode = "Offline";
        _settings.ServerUrl = null;
        SaveSettings(_settings);
    }

    /// <summary>
    /// SignalR-Service abrufen (wenn verbunden)
    /// </summary>
    public SignalRSyncService? GetSignalRSync() => _signalRSync;

    /// <summary>
    /// Ein Rennen abonnieren für Server-Updates
    /// </summary>
    public async Task SubscribeToRaceUpdatesAsync(Guid raceId)
    {
        if (_signalRSync?.IsConnected ?? false)
        {
            await _signalRSync.SubscribeToRaceAsync(raceId);
        }
    }

    /// <summary>
    /// Abonnement für ein Rennen beenden
    /// </summary>
    public async Task UnsubscribeFromRaceUpdatesAsync(Guid raceId)
    {
        if (_signalRSync?.IsConnected ?? false)
        {
            await _signalRSync.UnsubscribeFromRaceAsync(raceId);
        }
    }
}
