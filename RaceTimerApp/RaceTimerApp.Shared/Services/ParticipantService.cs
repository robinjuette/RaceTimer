using RaceTimer.Shared.Http;
using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Teilnehmerverwaltung
/// Verwendet lokal das IRaceRepository (offline-first)
/// </summary>
public class ParticipantService
{
    private readonly IRaceRepository _repository;

    public ParticipantService(IRaceRepository repository)
    {
        _repository = repository;
    }

    // Alle Teilnehmer abrufen
    public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
    {
        return await _repository.GetAllParticipantsAsync();
    }

    // Teilnehmer abrufen
    public async Task<Participant?> GetParticipantAsync(Guid id)
    {
        return await _repository.GetParticipantAsync(id);
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
            var created = await _repository.CreateParticipantAsync(displayName);
            participant = created;
        }

        // Zum Rennen hinzufügen
        return await _repository.AssignParticipantToRaceAsync(raceId, participant.Id);
    }

    // Teilnehmer aus Rennen entfernen
    public async Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
    {
        return await _repository.RemoveParticipantFromRaceAsync(raceId, participantId);
    }

    // Rennteilnehmer eines Rennens abrufen
    public async Task<IEnumerable<RaceParticipant>> GetRaceParticipantsAsync(Guid raceId)
    {
        return await _repository.GetRacesParticipantsAsync(raceId);
    }

    // Nicht zugeordnete Teilnehmer abrufen
    public async Task<IEnumerable<Participant>> GetUnassignedParticipantsAsync(Guid raceId)
    {
        var allParticipants = await _repository.GetAllParticipantsAsync();
        var raceParticipants = await _repository.GetRacesParticipantsAsync(raceId);

        var assignedIds = raceParticipants.Select(rp => rp.ParticipantID).ToHashSet();
        return allParticipants.Where(p => !assignedIds.Contains(p.Id));
    }

    // Suche Teilnehmer nach Name
    public async Task<IEnumerable<Participant>> SearchParticipantsAsync(string searchTerm)
    {
        var allParticipants = await _repository.GetAllParticipantsAsync();
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
