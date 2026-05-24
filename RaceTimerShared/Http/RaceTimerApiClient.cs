using RaceTimer.Shared.Models;
using System.Diagnostics;
using System.Net.Http.Json;

namespace RaceTimer.Shared.Http;

public class RaceTimerApiClient
{
    private readonly HttpClient _http;

    public RaceTimerApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<IEnumerable<Race>> GetRacesAsync() => _http.GetFromJsonAsync<IEnumerable<Race>>("api/races") ?? Task.FromResult(Enumerable.Empty<Race>());

    public async Task<Race?> GetRaceAsync(Guid id) => await _http.GetFromJsonAsync<Race>($"api/races/{id}");

    public async Task<Race?> CreateRaceAsync(Race race)
    {
        var res = await _http.PostAsJsonAsync("api/races", race);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Race>();
    }

    public async Task<bool> UpdateRaceAsync(Race race)
    {
        HttpResponseMessage response = await _http.PutAsJsonAsync($"api/races/{race.Id}", race);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRaceAsync(Guid id)
    {
        HttpResponseMessage response = await _http.DeleteAsync($"api/races/{id}");

        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<Participant>?> GetParticipantsAsync()
    {
        return await _http.GetFromJsonAsync<IEnumerable<Participant>>($"api/participants/");
    }

    public async Task<Participant?> GetParticipantAsync(Guid id)
    {
        return await _http.GetFromJsonAsync<Participant>($"api/participants/{id}");
    }

    public async Task<Participant?> CreateParticipantAsync(Participant participant)
    {
        var res = await _http.PostAsJsonAsync("api/participants", participant);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<Participant>();
    }

    public async Task<RaceParticipant?> CreateRaceParticipantAsync(Guid raceId, Guid participantID)
    {
        var res = await _http.PostAsync($"api/races/{raceId}/participants/{participantID}", null);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<RaceParticipant>();
    }

    public async Task<bool> DeleteRaceParticipantAsync(Guid raceId, Guid participantId)
    {
        var res = await _http.DeleteAsync($"api/races/{raceId}/participants/{participantId}");
        if (!res.IsSuccessStatusCode) return false;
        return true;
    }

    public async Task<IEnumerable<RaceParticipant>?> GetRaceParticipantsAsync(Guid raceId)
    {
        var res = await _http.GetAsync($"api/races/{raceId}/participants/");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<IEnumerable<RaceParticipant>>();
    }

    public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
    {
        var res = await _http.GetAsync($"api/races/{raceId}/timepoints");
        if (!res.IsSuccessStatusCode) return [];
        return await res.Content.ReadFromJsonAsync<IEnumerable<RaceTimePoint>>() ?? [];
    }

    public async Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, RaceTimePoint timePoint)
    {
        var res = await _http.PostAsJsonAsync($"api/races/{raceId}/timepoint", timePoint);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<RaceTimePoint>();
    }

    public async Task<bool> DeleteRaceTimePointAsync(Guid raceId, Guid timePointId)
    {
        var res = await _http.DeleteAsync($"api/races/{raceId}/timepoints/{timePointId}");
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> SetPenaltyTime(Guid timePointId, TimeSpan penaltyTime)
    {
        var res = await _http.PostAsJsonAsync($"api/timepoints/{timePointId}/penalty", penaltyTime);
        if (!res.IsSuccessStatusCode) return false;
        return true;
    }

    public async Task<RaceParticipantTimePoint?> CreateRaceParticipantTimePointAsync(DateTime dateTimeUTC)
    {
        var res = await _http.PostAsJsonAsync($"api/timepoints/", dateTimeUTC);
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<RaceParticipantTimePoint>();
    }

    public async Task<bool> AssignRaceParticipantTimePointAsync(Guid timePointId, Guid participantId)
    {
        var res = await _http.PostAsync($"api/timepoints/{timePointId}/assign/{participantId}", null);
        if (!res.IsSuccessStatusCode) return false;
        return true;
    }

    public async Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId)
    {
        var res = await _http.DeleteAsync($"api/timepoints/{timePointId}");
        if (!res.IsSuccessStatusCode) return false;
        return true;
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>?> GetRaceParticipantTimePointsAsync(Guid raceId)
    {
        var res = await _http.GetAsync($"api/timepoints/race/{raceId}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<IEnumerable<RaceParticipantTimePoint>>();
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimePointsAsync()
    {
        var res = await _http.GetAsync($"api/timepoints/unassigned/");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<IEnumerable<RaceParticipantTimePoint>>();
    }

    public async Task<bool> StartRaceAsync(Guid raceId, IEnumerable<Guid> participantIds)
    {
        var res = await _http.PostAsJsonAsync($"api/races/{raceId}/start/", participantIds);
        if (!res.IsSuccessStatusCode) return false;
        return true;
    }

    public async Task<bool> FinishRaceAsync(Guid raceId)
    {
        var res = await _http.PostAsync($"api/races/{raceId}/finish", null);
        if (!res.IsSuccessStatusCode) return false;
        return true;
    }

    public async Task<IEnumerable<Race>?> GetRunningRacesAsync()
    {
        var res = await _http.GetAsync("api/races/status/running");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<IEnumerable<Race>>();
    }

    public async Task<IEnumerable<Race>?> GetRacesByStatusAsync(string status)
    {
        var res = await _http.GetAsync($"api/races/status/{status}");
        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<IEnumerable<Race>>();
    }
}
