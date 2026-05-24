using RaceTimer.Shared.Http;
using RaceTimer.Shared.Models;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Rennverwaltung und Business-Logik
/// </summary>
public class RaceService
{
    private readonly RaceTimerApiClient _apiClient;
    private readonly TimingService _timingService;

    public RaceService(RaceTimerApiClient apiClient, TimingService timingService)
    {
        _apiClient = apiClient;
        _timingService = timingService;
    }

    // Rennen abrufen
    public async Task<IEnumerable<Race>> GetAllRacesAsync()
    {
        return await _apiClient.GetRacesAsync();
    }

    public async Task<Race?> GetRaceAsync(Guid id)
    {
        return await _apiClient.GetRaceAsync(id);
    }

    // Rennen filtern nach Status
    public async Task<IEnumerable<Race>> GetRunningRacesAsync()
    {
        var races = await _apiClient.GetRacesAsync();
        return races.Where(r => r.StartTimeUTC.HasValue && !r.FinishDateTimeUTC.HasValue);
    }

    public async Task<IEnumerable<Race>> GetPlannedRacesAsync()
    {
        var races = await _apiClient.GetRacesAsync();
        return races.Where(r => !r.StartTimeUTC.HasValue);
    }

    public async Task<IEnumerable<Race>> GetFinishedRacesAsync()
    {
        var races = await _apiClient.GetRacesAsync();
        return races.Where(r => r.FinishDateTimeUTC.HasValue);
    }

    // Rennen erstellen
    public async Task<Race?> CreateRaceAsync(string name)
    {
        var race = new Race
        {
            Id = Guid.NewGuid(),
            Name = name,
            RaceParticipants = [],
            RaceTimePoints = [],
            RaceParticipantTimePoints = []
        };

        return await _apiClient.CreateRaceAsync(race);
    }

    // Rennen als Kopie erstellen
    public async Task<Race?> CreateRaceFromCopyAsync(Race originalRace)
    {
        var newRace = new Race
        {
            Id = Guid.NewGuid(),
            Name = originalRace.Name + " - Kopie",
            RaceParticipants = [],
            RaceTimePoints = originalRace.RaceTimePoints
                .Select(rtp => new RaceTimePoint
                {
                    Id = Guid.NewGuid(),
                    DisplayName = rtp.DisplayName,
                    Index = rtp.Index,
                    RaceID = Guid.Empty,
                    Race = null!
                })
                .ToList(),
            RaceParticipantTimePoints = []
        };

        return await _apiClient.CreateRaceAsync(newRace);
    }

    // Rennen aktualisieren
    public async Task<bool> UpdateRaceAsync(Race race)
    {
        return await _apiClient.UpdateRaceAsync(race);
    }

    // Rennen löschen
    public async Task<bool> DeleteRaceAsync(Guid raceId)
    {
        return await _apiClient.DeleteRaceAsync(raceId);
    }

    // Rennen starten
    public async Task<bool> StartRaceAsync(Guid raceId, IEnumerable<Guid> participantIds)
    {
        return await _apiClient.StartRaceAsync(raceId, participantIds);
    }

    // Rennen beenden
    public async Task<bool> FinishRaceAsync(Guid raceId)
    {
        var race = await _apiClient.GetRaceAsync(raceId);
        if (race is null) return false;

        race.FinishDateTimeUTC = DateTime.UtcNow;
        return await _apiClient.UpdateRaceAsync(race);
    }

    // Zeitpunkte verwalten
    public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
    {
        var timePoints = await _apiClient.GetRaceTimePointsAsync(raceId);
        return timePoints.OrderBy(tp => tp.Index);
    }

    public async Task<RaceTimePoint?> AddRaceTimePointAsync(Guid raceId, string displayName)
    {
        var existingPoints = await _apiClient.GetRaceTimePointsAsync(raceId);
        var nextIndex = (uint)(existingPoints.Count() + 1);

        var timePoint = new RaceTimePoint
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName,
            Index = nextIndex,
            RaceID = raceId
        };

        return await _apiClient.CreateRaceTimePointAsync(raceId, timePoint);
    }

    public async Task<bool> DeleteRaceTimePointAsync(Guid raceId, Guid timePointId)
    {
        return await _apiClient.DeleteRaceTimePointAsync(raceId, timePointId);
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

    // Prüfe ob Rennen beendet werden sollte
    public async Task<bool> ShouldAutoFinishRaceAsync(Guid raceId)
    {
        var race = await _apiClient.GetRaceAsync(raceId);
        if (race is null || !race.StartTimeUTC.HasValue || race.FinishDateTimeUTC.HasValue)
            return false;

        var participants = await _apiClient.GetRaceParticipantsAsync(raceId);
        var timePoints = await _apiClient.GetRaceTimePointsAsync(raceId);

        // Rennen beenden wenn alle Teilnehmer das letzte Zeitpoint erreicht haben
        foreach (var participant in participants)
        {
            var participantTimes = race.RaceParticipantTimePoints
                .Where(rptp => rptp.ParticipantID == participant.ParticipantID)
                .OrderBy(rptp => rptp.TimePointUTC);

            if (!participantTimes.Any() || (!participantTimes.Last().RaceTimePoint?.Equals(timePoints.OrderBy(tp => tp.Index).Last()) ?? false))
                return false;
        }

        return true;
    }
}

public enum RaceStatus
{
    Planned,
    Running,
    Finished
}
