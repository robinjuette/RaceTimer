using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RaceTimer.Shared.Data;
using RaceTimer.Shared.Http;

namespace RaceTimer.Shared.Services;

/// <summary>
/// Erweiterungsmethoden für Dependency Injection.
/// Registriert alle Shared Services für MAUI-App oder Server.
/// </summary>
public static class SharedServiceCollectionExtensions
{
    /// <summary>
    /// Registriert lokale (offline) Race Services mit SQLite.
    /// Für MAUI-App und Desktop-Anwendungen.
    /// </summary>
    public static IServiceCollection AddLocalRaceServices(
        this IServiceCollection services,
        string? dbPath = null)
    {
        // Standard-Pfad für lokale Datenbank
        if (string.IsNullOrEmpty(dbPath))
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var raceTimerPath = Path.Combine(appDataPath, "RaceTimer");
            Directory.CreateDirectory(raceTimerPath);
            dbPath = Path.Combine(raceTimerPath, "racetimer.db");
        }

        services.AddDbContextFactory<RaceTimerDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<CoreRaceRepository>();

        return services;
    }

    /// <summary>
    /// Registriert Server-basierte Race Services mit REST-API und SignalR.
    /// Für Online-Betrieb mit Server-Synchronisation.
    /// </summary>
    public static IServiceCollection AddServerRaceServices(
        this IServiceCollection services)
    {
        // Registriere HTTP-Client für die API
        services.AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>();

        // Registriere SignalR Sync Service als Singleton (eine Verbindung pro App-Instanz)
        services.AddSingleton<SignalRSyncService>();

        // Registriere Server-basiertes Repository
        services.AddScoped<ServerRaceRepository>();

        return services;
    }

    /// <summary>
    /// Registriert ConfiguredConnectionRepository als Runtime-switcher Singleton.
    /// Dies erlaubt der App, zur Laufzeit zwischen lokalen und Server-basierten Repositories zu wechseln.
    /// Wird meist mit AddLocalRaceServices() initialisiert, kann aber später umgeschaltet werden.
    /// </summary>
    public static IServiceCollection AddConfiguredConnectionRepository(
        this IServiceCollection services,
        IRaceRepository? initialRepository = null)
    {
        // Wenn kein initiales Repository bereitgestellt, nutze das zuletzt registrierte
        services.AddScoped<ConfiguredConnectionRepository>();

        // Registriere ConfiguredConnectionRepository auch als IRaceRepository
        // Damit erhalten Consumers automatisch den Switcher
        services.AddScoped<IRaceRepository>(provider =>
            provider.GetRequiredService<ConfiguredConnectionRepository>());

        // Registriere auch als IRepositoryChangeNotifier
        services.AddScoped<IRepositoryChangeNotifier>(provider =>
            provider.GetRequiredService<ConfiguredConnectionRepository>());

        return services;
    }

    /// <summary>
    /// Registriert alle Services für MAUI-App mit Runtime-Umschaltung (Offline zu Online).
    /// Nutzt ConfiguredConnectionRepository als zentralen Proxy.
    /// </summary>
    public static IServiceCollection AddMauiRaceServicesWithSwitch(
        this IServiceCollection services,
        string? dbPath = null)
    {
        // Registriere lokale Services als Initial-Setup
        services.AddLocalRaceServices(dbPath);

        // Registriere ConfiguredConnectionRepository als Runtime-Switcher
        services.AddConfiguredConnectionRepository();

        return services;
    }
}
