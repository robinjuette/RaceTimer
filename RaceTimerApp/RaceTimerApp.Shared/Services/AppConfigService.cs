using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaceTimer.Shared.Http;
using RaceTimer.Shared.Services;
using RaceTimerApp.Shared.Models;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für App-Konfiguration und Server-Verbindungsverwaltung
/// Nutzt ConfiguredConnectionRepository für nahtlose Offline-/Online-Umschaltung
/// </summary>
public class AppConfigService
{
    private SignalRSyncService? _signalRSync;
    private AppSettings _settings;
    private readonly Action<AppSettings>? _savePersistence;
    private readonly Func<AppSettings>? _loadPersistence;
    private readonly ILogger<AppConfigService>? _logger;
    private readonly IServiceProvider? _serviceProvider;
    private readonly ConfiguredConnectionRepository _configuredRepository;
    private readonly SettingsService? _settingsService;
    private Task? _initializationTask;

    public AppConfigService(
        ConfiguredConnectionRepository configuredRepository,
        Action<AppSettings>? savePersistence = null, 
        Func<AppSettings>? loadPersistence = null,
        ILogger<AppConfigService>? logger = null,
        IServiceProvider? serviceProvider = null,
        SettingsService? settingsService = null)
    {
        _savePersistence = savePersistence;
        _loadPersistence = loadPersistence;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuredRepository = configuredRepository;
        _settingsService = settingsService;
        _settings = _loadPersistence?.Invoke() ?? _settingsService?.GetSettings() ?? new AppSettings();

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

    public Task InitializeAsync()
    {
        return _initializationTask ??= InitFromSettingsAsync();
    }

    private async Task InitFromSettingsAsync()
    {
        if (_settings.Mode.Equals("Online"))
        {
            await ConnectToServerAsync(_settings.ServerUrl);
        }
        else
        {
            await DisconnectFromServerAsync();
        }
    }

    /// <summary>
    /// Einstellungen speichern
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        _settings = settings;
        _savePersistence?.Invoke(settings);
        if (_settingsService is not null)
        {
            _ = _settingsService.SaveSettingsAsync(settings);
        }
    }

    // ===== Server-Verbindung =====

    /// <summary>
    /// Mit optionalem Server verbinden und Repository umschalten
    /// Nutzt ConfiguredConnectionRepository wenn verfügbar für nahtlose Umschaltung
    /// </summary>
    public async Task<bool> ConnectToServerAsync(string serverUrl)
    {
        if (string.IsNullOrEmpty(serverUrl))
            return false;

        try
        {
            // Versuche über ConfiguredConnectionRepository zu wechseln (bevorzugt)
            if (_configuredRepository != null && _serviceProvider != null)
            {
                return await SwitchToServerRepositoryAsync(serverUrl);
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
    /// Wechselt zu einem Server-basierten Repository via ConfiguredConnectionRepository
    /// </summary>
    private async Task<bool> SwitchToServerRepositoryAsync(string serverUrl)
    {
        if (_configuredRepository == null || _serviceProvider == null)
            return false;

        try
        {
            ServerRaceRepository? serverRaceRepository = _serviceProvider.GetService<ServerRaceRepository>();
            if(serverRaceRepository == null)
            {
                System.Diagnostics.Debug.WriteLine("ServerRaceRepository not registered in DI");
                return false;
            }

            serverRaceRepository.ServerUri = new(serverUrl);
            _signalRSync = _serviceProvider.GetService<SignalRSyncService>();

            // Schalte um zu Server-Repository
            await _configuredRepository.SwitchRepositoryAsync(serverRaceRepository, serverRaceRepository);

            _settings.ServerUrl = serverUrl;
            _settings.Mode = "Online";
            SaveSettings(_settings);

            System.Diagnostics.Debug.WriteLine("Successfully switched to server repository");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to switch to server repository: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Von Server trennen und zu lokalem Repository zurückschalten
    /// </summary>
    public async Task DisconnectFromServerAsync()
    {
        try
        {
            // Wenn ConfiguredConnectionRepository vorhanden, wechsle zurück zu lokal
            if (_configuredRepository != null && _serviceProvider != null)
            {
                await SwitchToLocalRepositoryAsync();
            }

            _settings.Mode = "Offline";
            _settings.ServerUrl = null;
            SaveSettings(_settings);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error disconnecting from server: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Wechselt zu lokalem Repository via ConfiguredConnectionRepository
    /// </summary>
    private async Task SwitchToLocalRepositoryAsync()
    {
        if (_configuredRepository == null || _serviceProvider == null)
            return;

        try
        {
            var coreRepository = _serviceProvider.GetRequiredService<CoreRaceRepository>();

            // Schalte um zu lokalem Repository
            await _configuredRepository.SwitchRepositoryAsync(coreRepository, coreRepository);

            System.Diagnostics.Debug.WriteLine("Successfully switched to local repository");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to switch to local repository: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Ein Rennen abonnieren für Server-Updates
    /// </summary>
    public async Task SubscribeToRaceUpdatesAsync(Guid raceId)
    {
        if (_signalRSync?.IsConnected ?? false)
        {
            await _signalRSync.SubscribeToRaceChangesAsync(raceId);
        }
    }

    /// <summary>
    /// Abonnement für ein Rennen beenden
    /// </summary>
    public async Task UnsubscribeFromRaceUpdatesAsync(Guid raceId)
    {
        if (_signalRSync?.IsConnected ?? false)
        {
            await _signalRSync.UnsubscribeFromRaceChangesAsync(raceId);
        }
    }
}

/// <summary>
/// Simple fallback logger implementation for testing/fallback scenarios.
/// </summary>
internal class SimpleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}


