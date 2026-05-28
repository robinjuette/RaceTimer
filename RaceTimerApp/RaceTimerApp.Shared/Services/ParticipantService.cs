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
        Participant? participant = null)
    {
        // Versuchen zu laden falls keiner provided
        participant ??= await _repository.TryFindParticipantAsync(displayName);
        // Erstellen wenn keiner gefunden
        participant ??= await _repository.CreateParticipantAsync(displayName);

        if (participant == null) return null;

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

}
