using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceTimer.Shared.Data;

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

        services.AddScoped<IRaceRepository, CoreRaceRepository>();

        return services;
    }

    /*
    /// <summary>
    /// Registriert SignalR Sync Service mit optionalem Server.
    /// </summary>
    public static IServiceCollection AddSignalRSync(
        this IServiceCollection services,
        string? serverUrl = null)
    {
        if (!string.IsNullOrEmpty(serverUrl))
        {
            services.AddSingleton(new SignalRSyncService(serverUrl));
        }

        return services;
    }*/

    /// <summary>
    /// Registriert alle lokalen Services für MAUI-App.
    /// </summary>
    public static IServiceCollection AddMauiRaceServices(
        this IServiceCollection services,
        string? dbPath = null,
        string? serverUrl = null)
    {
        services.AddLocalRaceServices(dbPath);
        //services.AddSignalRSync(serverUrl);

        return services;
    }
}
