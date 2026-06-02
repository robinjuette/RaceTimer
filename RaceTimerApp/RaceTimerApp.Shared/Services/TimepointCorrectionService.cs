using RaceTimer.Shared.Services;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Korrektionen von Zeitpunkten
/// </summary>
public class TimepointCorrectionService(IRaceRepository raceRepository)
{
    public async Task<bool> CorrectTimePointAsync(
        Guid timePointId,
        DateTime correctedTimeUTC,
        string reason,
        string? correctedByUser = null)
    {
        return await raceRepository.CorrectTimePointAsync(
            timePointId,
            correctedTimeUTC,
            reason,
            correctedByUser);
    }

    public async Task<bool> UndoTimePointCorrectionAsync(Guid timePointId)
    {
        return await raceRepository.UndoTimePointCorrectionAsync(timePointId);
    }

    /// <summary>
    /// Gibt Korrektur-Gründe als Optionen zurück
    /// </summary>
    public List<string> GetCorrectionReasons()
    {
        return new()
        {
            "Sensor-Fehler",
            "Falscher Zeitstempel",
            "Doppelte Erfassung",
            "Zu frühzeitig erfasst",
            "Zu spät erfasst",
            "Manueller Fehler",
            "Systemausfall",
            "Sonstiges"
        };
    }
}
