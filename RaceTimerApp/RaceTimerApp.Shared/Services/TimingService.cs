using RaceTimer.Shared.Http;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Zeiterfassung und Zeitmessungen
/// </summary>
public class TimingService
{
    private readonly IRaceRepository _repository;
    private List<WeakReference<TimingServiceUpdateEndpoint>> weakUpdateCallbacks = new();

    public TimingService(IRaceRepository repository)
    {
        _repository = repository;
    }

    public void RegisterForCallback(TimingServiceUpdateEndpoint callback)
    {
        weakUpdateCallbacks.Add(new(callback));
    }

    private void UpdateCallbacks(RaceParticipantTimePoint? updated)
    {
        foreach (var wr in weakUpdateCallbacks.ToList())
        {
            if (wr.TryGetTarget(out var handler))
            {
                handler.UpdateCallback(updated);
            }
            else
            {
                weakUpdateCallbacks.Remove(wr);
            }
        }
    }

    // Neuen Zeitpunkt erfassen
    public async Task<RaceParticipantTimePoint?> RecordTimePointAsync()
    {
        var created = await _repository.AddUnassignedTimePointAsync(DateTime.UtcNow);

        if (created is not null)
        {
            UpdateCallbacks(created);
        }

        return created;
    }

    // Unzugeordnete Zeitpunkte abrufen
    public async Task<List<RaceParticipantTimePoint>> GetUnassignedTimePointsAsync()
    {
        return (await _repository.GetUnassignedTimepointsAsync())?.ToList() ?? [];
    }

    // Zeitpunkt einem Teilnehmer zuordnen
    public async Task<bool> AssignTimePointAsync(
        Guid timePointId,
        Guid raceId,
        Guid participantId)
    {
        var success = await _repository.AssignTimePointToRaceParticipantAsync(timePointId, raceId, participantId);
        if (success)
        {
            UpdateCallbacks(await _repository.GetRaceParticipantTimePointAsync(timePointId));
        }

        return success;
    }

    // Zeitpunkt löschen
    public async Task<bool> DeleteTimePointAsync(Guid timePointId)
    {
        var success = await _repository.DeleteRaceParticipantTimePointAsync(timePointId);
        if (success)
        {
            UpdateCallbacks(null);
        }

        return success;
    }

    // Strafzeit aktualisieren
    public async Task<bool> UpdatePenaltyTimeAsync(Guid timePointId, TimeSpan penaltyTime)
    {
        bool success = await _repository.SetRaceParticipantTimePointPenaltyTime(timePointId, penaltyTime);
        if (success)
        {
            UpdateCallbacks(await _repository.GetRaceParticipantTimePointAsync(timePointId));
        }

        return success;
    }


    // Nächsten offenen Zeitpunkt für Teilnehmer finden
    public RaceTimePoint? GetNextTimePointForParticipant(
        Race race,
        RaceParticipant participant,
        IEnumerable<RaceParticipantTimePoint> participantTimePoints)
    {
        var timePoints = race.RaceTimePoints.OrderBy(tp => tp.Index).ToList();
        var participantTimes = participantTimePoints
            .Where(rptp => rptp.ParticipantID == participant.ParticipantID)
            .OrderBy(rptp => rptp.TimePointUTC)
            .ToList();

        if (!participantTimes.Any())
            return timePoints.FirstOrDefault(); // Erster Punkt

        var lastCompletedIndex = participantTimes.Last().RaceTimePoint?.Index ?? 0;
        return timePoints.FirstOrDefault(tp => tp.Index > lastCompletedIndex);
    }

    // Zeitpunkte mit Strafzeit abrufen
    public async Task<IEnumerable<RaceParticipantTimePoint>> GetTimePointsWithPenaltyAsync(Guid raceId)
    {
        var timePoints = await _repository.GetRaceParticipantTimePointsForRaceAsync(raceId);
        var raceTimePoints = await _repository.GetRaceTimePointsAsync(raceId);

        // Finde Zeitpunkte deren Rennzeitpunkt Strafzeit hat
        var raceTimePointsWithPenalty = raceTimePoints
            .Where(rtp => rtp.Id != Guid.Empty && rtp.HasPenaltyTime); // Hier würde eine Property "HasPenalty" helfen

        return timePoints.Where(rptp => raceTimePointsWithPenalty.Any(rtp => rtp.Index == rptp.RTPIndex));
    }

    // Zeitpunkte mit offener Strafzeit abrufen
    public async Task<IEnumerable<RaceParticipantTimePoint>> GetTimePointsWithOpenPenaltyAsync(Guid raceId)
    {
        var timePoints = await _repository.GetRaceParticipantTimePointsForRaceAsync(raceId);
        var raceTimePoints = await _repository.GetRaceTimePointsAsync(raceId);

        // Finde Zeitpunkte deren Rennzeitpunkt Strafzeit hat
        var raceTimePointsWithPenalty = raceTimePoints
            .Where(rtp => rtp.Id != Guid.Empty && rtp.HasPenaltyTime); // Hier würde eine Property "HasPenalty" helfen

        return timePoints.Where(rptp => raceTimePointsWithPenalty.Any(rtp => rtp.Id == rptp.Id) && rptp.PenaltyTime == null);
    }

}

public class TimingServiceUpdateEndpoint(Action<RaceParticipantTimePoint?> updateCallback)
{
    public Action<RaceParticipantTimePoint?> UpdateCallback => updateCallback;
}
