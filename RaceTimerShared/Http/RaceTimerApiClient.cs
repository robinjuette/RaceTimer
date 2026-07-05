using RaceTimer.Shared.Models;
using System.Net.Http.Json;

namespace RaceTimer.Shared.Http;

/// <summary>
/// High-level wrapper around the RaceTimer REST API client.
/// Provides strongly-typed methods for all API operations.
/// The underlying HTTP client is configured via dependency injection.
/// </summary>
public interface IRaceTimerApiClient
{
    // Races
    Task<IEnumerable<Race>> GetRacesAsync(CancellationToken cancellationToken = default);
    Task<Race?> GetRaceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Race>> GetRacesByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<Race?> CreateRaceAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> UpdateRaceAsync(Race race, CancellationToken cancellationToken = default);
    Task<bool> DeleteRaceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> StartRaceAsync(Guid raceId, IEnumerable<Guid> participantIds, DateTime? timePointUtc = null, CancellationToken cancellationToken = default);
    Task<bool> FinishRaceAsync(Guid raceId, CancellationToken cancellationToken = default);

    // Participants
    Task<IEnumerable<Participant>> GetParticipantsAsync(CancellationToken cancellationToken = default);
    Task<Participant?> GetParticipantAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Participant?> CreateParticipantAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> UpdateParticipantAsync(Participant participant, CancellationToken cancellationToken = default);
    Task<bool> DeleteParticipantAsync(Guid id, CancellationToken cancellationToken = default);

    // Race Participants
    Task<IEnumerable<RaceParticipant>> GetRaceParticipantsAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<RaceParticipant?> AssignParticipantToRaceAsync(Guid raceId, Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId, CancellationToken cancellationToken = default);

    // Race Time Points
    Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name, CancellationToken cancellationToken = default);
    Task<bool> UpdateRaceTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints, CancellationToken cancellationToken = default);
    Task<bool> DeleteRaceTimePointAsync(Guid raceId, Guid timePointId, CancellationToken cancellationToken = default);
    Task<bool> CopyTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo, CancellationToken cancellationToken = default);

    // Race Participant Time Points
    Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsAsync(Guid raceId, CancellationToken cancellationToken = default);
    Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RaceParticipantTimePoint>> GetUnassignedTimePointsAsync(CancellationToken cancellationToken = default);
    Task<RaceParticipantTimePoint?> CreateUnassignedTimePointAsync(DateTime timePointUtc, CancellationToken cancellationToken = default);
    Task<bool> AssignTimePointToParticipantAsync(Guid timePointId, Guid participantId, Guid raceId, CancellationToken cancellationToken = default);
    Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId, CancellationToken cancellationToken = default);
    Task<bool> SetTimePointPenaltyAsync(Guid timePointId, TimeSpan penaltyTime, CancellationToken cancellationToken = default);
    Task<bool> CorrectTimePointAsync(Guid timePointId, DateTime correctedTimeUtc, string reason, string? correctedByUser, CancellationToken cancellationToken = default);
    Task<bool> UndoTimePointCorrectionAsync(Guid timePointId, CancellationToken cancellationToken = default);

    // Change Tracking
    Task<object?> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of IRaceTimerApiClient using a standard HttpClient.
/// This class provides a wrapper around REST API calls with proper error handling and serialization.
/// </summary>
internal class RaceTimerApiClient : IRaceTimerApiClient
{
    private readonly HttpClient _http;
    private const string BasePath = "api/racetimer";

    public RaceTimerApiClient(HttpClient http)
    {
        _http = http;
    }

    #region Races

    public async Task<IEnumerable<Race>> GetRacesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<Race>>($"{BasePath}/races", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<Race?> GetRaceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<Race?>($"{BasePath}/races/{id}", cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Race>> GetRacesByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<Race>>($"{BasePath}/races/status/{status}", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<Race?> CreateRaceAsync(string name, CancellationToken cancellationToken = default)
    {
        var request = new { Name = name };
        var response = await _http.PostAsJsonAsync($"{BasePath}/races", request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<Race?>(cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateRaceAsync(Race race, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"{BasePath}/races/{race.Id}", race, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRaceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"{BasePath}/races/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> StartRaceAsync(Guid raceId, IEnumerable<Guid> participantIds, DateTime? timePointUtc = null, CancellationToken cancellationToken = default)
    {
        var request = new { ParticipantIds = participantIds, TimePointUtc = timePointUtc };
        var response = await _http.PostAsJsonAsync($"{BasePath}/races/{raceId}/start", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> FinishRaceAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"{BasePath}/races/{raceId}/finish", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Participants

    public async Task<IEnumerable<Participant>> GetParticipantsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<Participant>>($"{BasePath}/participants", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<Participant?> GetParticipantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<Participant?>($"{BasePath}/participants/{id}", cancellationToken: cancellationToken);
    }

    public async Task<Participant?> CreateParticipantAsync(string name, CancellationToken cancellationToken = default)
    {
        var request = new { Name = name };
        var response = await _http.PostAsJsonAsync($"{BasePath}/participants", request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<Participant?>(cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateParticipantAsync(Participant participant, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"{BasePath}/participants/{participant.Id}", participant, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteParticipantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"{BasePath}/participants/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Race Participants

    public async Task<IEnumerable<RaceParticipant>> GetRaceParticipantsAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<RaceParticipant>>($"{BasePath}/races/{raceId}/participants", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<RaceParticipant?> AssignParticipantToRaceAsync(Guid raceId, Guid participantId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"{BasePath}/races/{raceId}/participants/{participantId}", null, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RaceParticipant?>(cancellationToken: cancellationToken);
    }

    public async Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"{BasePath}/races/{raceId}/participants/{participantId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Race Time Points

    public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<RaceTimePoint>>($"{BasePath}/races/{raceId}/timepoints", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name, CancellationToken cancellationToken = default)
    {
        var request = new { Name = name };
        var response = await _http.PostAsJsonAsync($"{BasePath}/races/{raceId}/timepoint", request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RaceTimePoint?>(cancellationToken: cancellationToken);
    }

    public async Task<bool> UpdateRaceTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"{BasePath}/races/{raceId}/timepoints", timePoints, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRaceTimePointAsync(Guid raceId, Guid timePointId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"{BasePath}/races/{raceId}/timepoints/{timePointId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CopyTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"{BasePath}/races/{raceIdCopyFrom}/timepoints/copy-to/{raceIdCopyTo}", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Race Participant Time Points

    public async Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<RaceParticipantTimePoint>>($"{BasePath}/timepoints/race/{raceId}", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<RaceParticipantTimePoint?>($"{BasePath}/timepoints/{id}", cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>> GetUnassignedTimePointsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _http.GetFromJsonAsync<IEnumerable<RaceParticipantTimePoint>>($"{BasePath}/timepoints/unassigned", cancellationToken: cancellationToken);
        return result ?? [];
    }

    public async Task<RaceParticipantTimePoint?> CreateUnassignedTimePointAsync(DateTime timePointUtc, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"{BasePath}/timepoints", timePointUtc, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RaceParticipantTimePoint?>(cancellationToken: cancellationToken);
    }

    public async Task<bool> AssignTimePointToParticipantAsync(Guid timePointId, Guid participantId, Guid raceId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"{BasePath}/timepoints/{timePointId}/assign/{participantId}?raceId={raceId}", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"{BasePath}/timepoints/{timePointId}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetTimePointPenaltyAsync(Guid timePointId, TimeSpan penaltyTime, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync($"{BasePath}/timepoints/{timePointId}/penalty", penaltyTime, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> CorrectTimePointAsync(Guid timePointId, DateTime correctedTimeUtc, string reason, string? correctedByUser, CancellationToken cancellationToken = default)
    {
        var request = new { CorrectedTimeUtc = correctedTimeUtc, Reason = reason, CorrectedByUser = correctedByUser };
        var response = await _http.PostAsJsonAsync($"{BasePath}/timepoints/{timePointId}/correct", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UndoTimePointCorrectionAsync(Guid timePointId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsync($"{BasePath}/timepoints/{timePointId}/undo", null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    #endregion

    #region Change Tracking

    public async Task<object?> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc, CancellationToken cancellationToken = default)
    {
        return await _http.GetFromJsonAsync<object?>($"{BasePath}/races/{raceId}/changes?sinceUtc={sinceUtc:O}", cancellationToken: cancellationToken);
    }

    #endregion
}
