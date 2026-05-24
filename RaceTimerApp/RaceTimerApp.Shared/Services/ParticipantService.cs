using RaceTimer.Shared.Http;
using RaceTimer.Shared.Models;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Teilnehmerverwaltung
/// </summary>
public class ParticipantService
{
    private readonly RaceTimerApiClient _apiClient;

    public ParticipantService(RaceTimerApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Alle Teilnehmer abrufen
    public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
    {
        return await _apiClient.GetParticipantsAsync() ?? Enumerable.Empty<Participant>();
    }

    // Teilnehmer abrufen
    public async Task<Participant?> GetParticipantAsync(Guid id)
    {
        return await _apiClient.GetParticipantAsync(id);
    }

    // Neuen Teilnehmer erstellen oder aus bestehendem hinzufügen
    public async Task<RaceParticipant?> AddParticipantToRaceAsync(
        Guid raceId,
        string displayName,
        Participant? existingParticipant = null)
    {
        Participant? participant;

        if (existingParticipant is not null)
        {
            participant = existingParticipant;
        }
        else
        {
            // Neuen Teilnehmer erstellen
            participant = new Participant
            {
                Id = Guid.NewGuid(),
                DisplayName = displayName
            };

            var created = await _apiClient.CreateParticipantAsync(participant);
            if (created is null) return null;

            participant = created;
        }

        // Zum Rennen hinzufügen
        return await _apiClient.CreateRaceParticipantAsync(raceId, participant.Id);
    }

    // Teilnehmer aus Rennen entfernen
    public async Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
    {
        return await _apiClient.DeleteRaceParticipantAsync(raceId, participantId);
    }

    // Rennteilnehmer eines Rennens abrufen
    public async Task<IEnumerable<RaceParticipant>> GetRaceParticipantsAsync(Guid raceId)
    {
        return await _apiClient.GetRaceParticipantsAsync(raceId);
    }

    // Nicht zugeordnete Teilnehmer abrufen
    public async Task<IEnumerable<Participant>> GetUnassignedParticipantsAsync(Guid raceId)
    {
        var allParticipants = await _apiClient.GetParticipantsAsync();
        var raceParticipants = await _apiClient.GetRaceParticipantsAsync(raceId);

        var assignedIds = raceParticipants.Select(rp => rp.ParticipantID).ToHashSet();
        return allParticipants.Where(p => !assignedIds.Contains(p.Id));
    }

    // Suche Teilnehmer nach Name
    public async Task<IEnumerable<Participant>> SearchParticipantsAsync(string searchTerm)
    {
        var allParticipants = await _apiClient.GetParticipantsAsync();
        return allParticipants.Where(p =>
            p.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
    }

    // Prüfe ob Rennen aktive Teilnehmer hat (nicht beendet)
    public IEnumerable<RaceParticipant> GetActiveParticipants(
        IEnumerable<RaceParticipant> raceParticipants)
    {
        return raceParticipants.Where(rp =>
            rp.StartTime.HasValue && !rp.FinishDateTimeUTC.HasValue);
    }

    // Prüfe ob Rennen noch nicht gestartete Teilnehmer hat
    public IEnumerable<RaceParticipant> GetNotStartedParticipants(
        IEnumerable<RaceParticipant> raceParticipants)
    {
        return raceParticipants.Where(rp => !rp.StartTime.HasValue);
    }

    // Prüfe ob Rennen beendete Teilnehmer hat
    public IEnumerable<RaceParticipant> GetFinishedParticipants(
        IEnumerable<RaceParticipant> raceParticipants)
    {
        return raceParticipants.Where(rp => rp.FinishDateTimeUTC.HasValue);
    }
}
