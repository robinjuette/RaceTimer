using RaceTimer.Shared.Models;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Sortierung und Ranglistenberechnung
/// </summary>
public class RankingService
{
    /// <summary>
    /// Sortiere Teilnehmer nach Zwischenstand für die Übersichtsseite
    /// </summary>
    public IEnumerable<(RaceParticipant Participant, TimeSpan? Time)> GetSortedParticipants(
        Race race,
        IEnumerable<RaceParticipant> participants,
        TimingService timingService)
    {
        var timingService_param = timingService;
        var result = new List<(RaceParticipant Participant, TimeSpan? Time)>();

        // 1. Beendete Teilnehmer (nach Gesamtzeit sortiert)
        var finishedParticipants = participants
            .Where(p => p.FinishDateTimeUTC.HasValue)
            .OrderBy(p =>
            {
                var totalTime = timingService_param.CalculateTotalTime(
                    p,
                    race.RaceParticipantTimePoints);
                return totalTime ?? TimeSpan.MaxValue;
            })
            .ToList();

        foreach (var p in finishedParticipants)
        {
            var totalTime = timingService_param.CalculateTotalTime(p, race.RaceParticipantTimePoints);
            result.Add((p, totalTime));
        }

        // 2. Aktive Teilnehmer (nach Progress sortiert)
        var activeParticipants = participants
            .Where(p => p.StartTime.HasValue && !p.FinishDateTimeUTC.HasValue)
            .OrderBy(p =>
            {
                var progressTime = timingService_param.CalculateProgressTime(
                    race,
                    p,
                    race.RaceParticipantTimePoints);
                return progressTime ?? TimeSpan.MaxValue;
            })
            .ToList();

        // Berechne die Position basierend auf vergleichbarem Fortschritt mit beendeten Teilnehmern
        foreach (var activeParticipant in activeParticipants)
        {
            var progressTime = timingService_param.CalculateProgressTime(
                race,
                activeParticipant,
                race.RaceParticipantTimePoints);

            if (!progressTime.HasValue)
            {
                result.Add((activeParticipant, progressTime));
                continue;
            }

            // Finde die erste beendete Teilnehmerin, die länger für den gleichen Fortschritt brauchte
            var insertIndex = result.Count;
            for (int i = 0; i < result.Count; i++)
            {
                if (result[i].Time.HasValue && result[i].Time > progressTime)
                {
                    insertIndex = i;
                    break;
                }
            }

            result.Insert(insertIndex, (activeParticipant, progressTime));
        }

        return result;
    }

    /// <summary>
    /// Berechne Rennposition für einen Teilnehmer
    /// </summary>
    public int GetParticipantPosition(
        RaceParticipant participant,
        IEnumerable<(RaceParticipant, TimeSpan?)> sortedParticipants)
    {
        var position = 1;
        foreach (var (p, _) in sortedParticipants)
        {
            if (p.ParticipantID == participant.ParticipantID)
                return position;
            position++;
        }

        return position;
    }

    /// <summary>
    /// Berechne den Fortschritt eines Teilnehmers in Prozent
    /// </summary>
    public double GetParticipantProgress(
        RaceParticipant participant,
        Race race,
        IEnumerable<RaceParticipantTimePoint> participantTimePoints)
    {
        if (!participant.StartTime.HasValue)
            return 0;

        var totalTimePoints = race.RaceTimePoints.Count;
        if (totalTimePoints == 0)
            return 0;

        var completedTimePoints = participantTimePoints
            .Where(rptp => rptp.ParticipantID == participant.ParticipantID)
            .Count();

        return (double)completedTimePoints / totalTimePoints * 100;
    }

    /// <summary>
    /// Prüfe ob ein Teilnehmer das nächste Zeitpunkt erreicht hat
    /// </summary>
    public bool HasReachedTimePoint(
        RaceParticipant participant,
        RaceTimePoint timePoint,
        IEnumerable<RaceParticipantTimePoint> participantTimePoints)
    {
        return participantTimePoints.Any(rptp =>
            rptp.ParticipantID == participant.ParticipantID &&
            rptp.RaceTimePoint?.Id == timePoint.Id);
    }
}
