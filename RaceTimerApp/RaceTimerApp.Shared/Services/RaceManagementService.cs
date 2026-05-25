using RaceTimer.Shared.Http;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Rennverwaltung und Business-Logik
/// Verwendet lokal das IRaceRepository (offline-first) und optional Server-Sync via SignalR
/// </summary>
public class RaceManagementService
{
    private readonly IRaceRepository _repository;
    private readonly SignalRSyncService? _signalRSync;

    /// <summary>
    /// Offline-Modus (nur lokales Repository)
    /// </summary>
    public RaceManagementService(IRaceRepository repository)
    {
        _repository = repository;
        _signalRSync = null;
    }

    /// <summary>
    /// Online-Modus (mit optionalem Server-Sync)
    /// </summary>
    public RaceManagementService(IRaceRepository repository, SignalRSyncService signalRSync)
    {
        _repository = repository;
        _signalRSync = signalRSync;
    }

    // Rennen abrufen
    public async Task<IEnumerable<Race>> GetAllRacesAsync()
    {
        return await _repository.GetAllRacesAsync();
    }

    public async Task<Race?> GetRaceAsync(Guid id)
    {
        return await _repository.GetRaceAsync(id);
    }

    // Rennen filtern nach Status
    public async Task<IEnumerable<Race>> GetRunningRacesAsync()
    {
        return await _repository.GetRacesByStatusAsync(RaceStatus.Running);
    }

    public async Task<IEnumerable<Race>> GetPlannedRacesAsync()
    {
        return await _repository.GetRacesByStatusAsync(RaceStatus.Planned);
    }

    public async Task<IEnumerable<Race>> GetFinishedRacesAsync()
    {
        return await _repository.GetRacesByStatusAsync(RaceStatus.Finished);
    }

    // Rennen erstellen
    public async Task<Race?> CreateRaceAsync(string name)
    {
        return await _repository.AddRaceAsync(name);
    }

    // Rennen als Kopie erstellen
    public async Task<Race?> CreateRaceFromCopyAsync(Race originalRace)
    {
        var newRace = await _repository.AddRaceAsync(originalRace.Name + " - Kopie");

        if (newRace == null) return null;

        await _repository.CopyRaceTimePointsAsync(originalRace.Id, newRace.Id);

        return newRace;
    }

    // Rennen löschen
    public async Task<bool> DeleteRaceAsync(Guid raceId)
    {
        return await _repository.DeleteRaceAsync(raceId);
    }

    // Rennen starten
    public async Task<bool> StartRaceAsync(Guid raceId, params IEnumerable<Guid> participantIds)
    {
        return await _repository.StartRaceAsync(raceId, DateTime.UtcNow, participantIds.ToList());
    }

    // Zeitpunkte verwalten
    public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
    {
        IEnumerable<RaceTimePoint> timePoints = await _repository.GetRaceTimePointsAsync(raceId);
        return timePoints.OrderBy(tp => tp.Index);
    }

    public async Task<RaceTimePoint?> AddRaceTimePointAsync(Guid raceId, string displayName)
    {
        return await _repository.CreateRaceTimePointAsync(raceId, displayName);
    }

    public async Task<bool> DeleteRaceTimePointAsync(Guid raceId, Guid timePointId)
    {
        return await _repository.DeleteRaceTimePointAsync(timePointId);
    }

    // Status berechnen
    public RaceStatus GetRaceStatus(Race race)
    {
        if (race.FinishDateTimeUTC.HasValue)
            return RaceStatus.Finished;
        if (race.StartTimeUTC.HasValue)
            return RaceStatus.Running;
        return RaceStatus.Planned;
    }

}

