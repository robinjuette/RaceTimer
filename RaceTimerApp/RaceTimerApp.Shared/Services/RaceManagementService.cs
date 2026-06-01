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

    /// <summary>
    /// Erstellt ein neues Rennen mit dem angegebenen Namen. Generiert einen Namen wenn keiner übergeben wird.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<Race?> CreateRaceAsync(string? name = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Neues Rennen {DateTime.Now.ToString("yyyyMMdd_HHmmss")}";
        }

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

    // Rennen umbenennen
    public async Task<bool> UpdateRaceNameAsync(Guid raceId, string newName)
    {
        var race = await _repository.GetRaceAsync(raceId);
        if (race is null) return false;

        race.Name = newName;
        await _repository.UpdateRaceAsync(race);
        return true;
    }

    // RaceTimePoint aktualisieren (Name und HasPenaltyTime)
    public async Task<bool> UpdateRaceTimePointAsync(Guid raceId, Guid timePointId, string displayName, bool hasPenaltyTime)
    {
        var race = await _repository.GetRaceAsync(raceId);
        if (race is null) return false;

        var timePoint = race.RaceTimePoints.FirstOrDefault(tp => tp.Id == timePointId);
        if (timePoint is null) return false;

        timePoint.DisplayName = displayName;
        timePoint.HasPenaltyTime = hasPenaltyTime;

        await _repository.UpdateTimePointsAsync(raceId, race.RaceTimePoints.OrderBy(rtp => rtp.Index).ToList());
        return true;
    }

    // RaceTimePoints umsortieren (Reorder)
    public async Task<bool> ReorderRaceTimePointsAsync(Guid raceId, List<Guid> orderedTimePointIds)
    {
        var race = await _repository.GetRaceAsync(raceId);
        if (race is null) return false;

        // Erstelle neue Liste mit aktualisierten Index-Werten
        var reorderedTimePoints = new List<RaceTimePoint>();
        for (uint i = 0; i < orderedTimePointIds.Count; i++)
        {
            var tp = race.RaceTimePoints.FirstOrDefault(rtp => rtp.Id == orderedTimePointIds[(int)i]);
            if (tp is not null)
            {
                tp.Index = i + 1; // Index beginnt bei 1
                reorderedTimePoints.Add(tp);
            }
        }

        if (reorderedTimePoints.Count != orderedTimePointIds.Count)
            return false;

        await _repository.UpdateTimePointsAsync(raceId, reorderedTimePoints);
        return true;
    }

    // Überprüfung und Beendigung eines Rennens
    public async Task CheckForRaceCompletionAsync(Guid raceId)
    {
        await _repository.CheckForRaceCompletionAsync(raceId);
    }
}


