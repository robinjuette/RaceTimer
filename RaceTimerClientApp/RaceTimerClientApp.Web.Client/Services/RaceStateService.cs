using System.Text.Json;
using System.Net.Http.Json;
using RaceTimer.Shared.Models;
using Microsoft.JSInterop;

namespace RaceTimerClientApp.Web.Client.Services;

public class RaceStateService : IAsyncDisposable
{
    private readonly RaceSignalRService _signalR;
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    // in-memory cache of races by id
    public Dictionary<Guid, Race> Races { get; } = new();
    public List<RaceParticipantTimePoint> UnassignedTimePoints { get; } = new();

    private const string LastOpenedKey = "racetimer.lastopened";
    private const string LastKnownKeyPrefix = "racetimer.lastknown.";

    // track per-race last known modification timestamp
    private readonly Dictionary<Guid, DateTime> _lastKnown = new();

    public RaceStateService(RaceSignalRService signalR, HttpClient http, IJSRuntime js)
    {
        _signalR = signalR;
        _http = http;
        _js = js;

        _signalR.RaceUpdated += async (obj) => await OnRaceUpdated(obj);
        _signalR.RaceDeleted += async (id) => await OnRaceDeleted(id);
        _signalR.UnassignedTimePointAdded += async (obj) => await OnUnassignedTimePointAdded(obj);
        _signalR.TimePointAssigned += async (obj) => await OnTimePointAssigned(obj);
        _signalR.Reconnected += async (cid) => await OnReconnected(cid);
    }

    public async Task<IEnumerable<Race>> GetAvailableRacesAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<Race>>("api/races") ?? Enumerable.Empty<Race>();
        }
        catch
        {
            return Enumerable.Empty<Race>();
        }
    }

    public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<IEnumerable<Participant>>("api/participants") ?? Enumerable.Empty<Participant>();
        }
        catch
        {
            return Enumerable.Empty<Participant>();
        }
    }

    // Create a new race and subscribe to it
    public async Task<Race?> CreateRaceAsync(Race race)
    {
        var res = await _http.PostAsJsonAsync("api/races", race);
        if (!res.IsSuccessStatusCode) return null;
        var created = await res.Content.ReadFromJsonAsync<Race>();
        if (created is not null)
        {
            Races[created.Id] = created;
            await SubscribeAndFetchRaceAsync(created.Id);
        }
        return created;
    }

    public async Task<Participant?> CreateParticipantAsync(Participant p)
    {
        var res = await _http.PostAsJsonAsync("api/participants", p);
        if (!res.IsSuccessStatusCode) return null;
        var created = await res.Content.ReadFromJsonAsync<Participant>();
        return created;
    }

    public async Task<RaceParticipantTimePoint?> CreateUnassignedTimePointAsync(DateTime utc)
    {
        var res = await _http.PostAsJsonAsync("api/timepoints/unassigned", utc);
        if (!res.IsSuccessStatusCode) return null;
        var created = await res.Content.ReadFromJsonAsync<RaceParticipantTimePoint>();
        if (created is not null) UnassignedTimePoints.Add(created);
        return created;
    }

    public async Task<bool> AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
    {
        var res = await _http.PostAsync($"api/races/{raceId}/participants/{participantId}", null);
        if (res.IsSuccessStatusCode)
        {
            // local update will be applied via SignalR event; return true
            return true;
        }
        return false;
    }

    public async Task<bool> AssignTimePointToParticipantAsync(Guid timePointId, Guid participantId)
    {
        var res = await _http.PostAsync($"api/timepoints/{timePointId}/assign/participant/{participantId}", null);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> AssignTimePointToRaceAsync(Guid timePointId, Guid raceId)
    {
        var res = await _http.PostAsync($"api/timepoints/{timePointId}/assign/race/{raceId}", null);
        return res.IsSuccessStatusCode;
    }

    public async Task InitializeAsync()
    {
        await _signalR.StartAsync();
        // load last opened races from localStorage
        try
        {
            var json = await _js.InvokeAsync<string>("localStorage.getItem", LastOpenedKey);
            if (!string.IsNullOrEmpty(json))
            {
                var ids = JsonSerializer.Deserialize<Guid[]>(json) ?? Array.Empty<Guid>();
                foreach (var id in ids)
                {
                    await SubscribeAndFetchRaceAsync(id);
                }
            }
        }
        catch { }
    }

    public async Task SubscribeAndFetchRaceAsync(Guid id)
    {
        await _signalR.SubscribeRaceAsync(id);
        // on subscribe try to fetch only changes since last known
        var lastKnown = await GetLastKnownForAsync(id);
        if (lastKnown is not null)
        {
            var changes = await _http.GetFromJsonAsync<JsonElement>($"api/races/{id}/changes?sinceUtc={lastKnown.Value.ToString("o")}");
            if (changes.ValueKind != JsonValueKind.Undefined)
            {
                await ApplyChanges(id, changes);
            }
            else
            {
                var r = await _http.GetFromJsonAsync<Race>($"api/races/{id}");
                if (r is not null) Races[id] = r;
            }
        }
        else
        {
            var r = await _http.GetFromJsonAsync<Race>($"api/races/{id}");
            if (r is not null) Races[id] = r;
        }
        SaveLastOpened();
    }

    public async Task UnsubscribeRaceAsync(Guid id)
    {
        await _signalR.UnsubscribeRaceAsync(id);
        Races.Remove(id);
        SaveLastOpened();
    }

    private void SaveLastOpened()
    {
        var ids = Races.Keys.ToArray();
        var json = JsonSerializer.Serialize(ids);
        _js.InvokeVoidAsync("localStorage.setItem", LastOpenedKey, json);
    }

    private Task OnRaceUpdated(object payload)
    {
        try
        {
            var json = payload.ToString() ?? string.Empty;
            var el = (JsonElement)payload;
            var r = el.Deserialize<Race>();
            if (r is not null)
            {
                Races[r.Id] = r;
            }
        }
        catch { }
        return Task.CompletedTask;
    }

    private async Task OnReconnected(string connectionId)
    {
        // resync all subscribed races using change feed
        foreach (var id in Races.Keys.ToList())
        {
            var lastKnown = await GetLastKnownForAsync(id);
            var changes = await _http.GetFromJsonAsync<JsonElement>($"api/races/{id}/changes?sinceUtc={(lastKnown ?? DateTime.MinValue).ToString("o")}");
            if (changes.ValueKind != JsonValueKind.Undefined)
            {
                await ApplyChanges(id, changes);
            }
            else
            {
                var r = await _http.GetFromJsonAsync<Race>($"api/races/{id}");
                if (r is not null) Races[id] = r;
            }
        }
    }

    private async Task<DateTime?> GetLastKnownForAsync(Guid id)
    {
        if (_lastKnown.TryGetValue(id, out var dt)) return dt;
        try
        {
            var s = await _js.InvokeAsync<string>("localStorage.getItem", LastKnownKeyPrefix + id.ToString());
            if (!string.IsNullOrEmpty(s) && DateTime.TryParse(s, out var parsed))
            {
                _lastKnown[id] = parsed;
                return parsed;
            }
        }
        catch { }
        return null;
    }

    private async Task SetLastKnownForAsync(Guid id, DateTime dt)
    {
        _lastKnown[id] = dt;
        await _js.InvokeVoidAsync("localStorage.setItem", LastKnownKeyPrefix + id.ToString(), dt.ToString("o"));
    }

    private async Task ApplyChanges(Guid raceId, JsonElement changes)
    {
        try
        {
            // parse and apply changes
            if (changes.TryGetProperty("Races", out var racesEl) && racesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in racesEl.EnumerateArray())
                {
                    var r = el.Deserialize<Race>();
                    if (r is not null) Races[r.Id] = r;
                }
            }
            if (changes.TryGetProperty("RaceParticipants", out var rpsEl) && rpsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in rpsEl.EnumerateArray())
                {
                    var rp = el.Deserialize<RaceParticipant>();
                    if (rp is not null)
                    {
                        if (Races.TryGetValue(raceId, out var race))
                        {
                            var existing = race.RaceParticipants.FirstOrDefault(x => x.ParticipantID == rp.ParticipantID && x.RaceID == rp.RaceID);
                            if (existing is null) race.RaceParticipants.Add(rp);
                            else
                            {
                                var idx = race.RaceParticipants.IndexOf(existing);
                                race.RaceParticipants[idx] = rp;
                            }
                        }
                    }
                }
            }
            if (changes.TryGetProperty("RaceTimePoints", out var rtpsEl) && rtpsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in rtpsEl.EnumerateArray())
                {
                    var rtp = el.Deserialize<RaceTimePoint>();
                    if (rtp is not null)
                    {
                        if (Races.TryGetValue(raceId, out var race))
                        {
                            var existing = race.RaceTimePoints.FirstOrDefault(x => x.Id == rtp.Id);
                            if (existing is null) race.RaceTimePoints.Add(rtp);
                            else
                            {
                                var idx = race.RaceTimePoints.IndexOf(existing);
                                race.RaceTimePoints[idx] = rtp;
                            }
                        }
                    }
                }
            }
            if (changes.TryGetProperty("RaceParticipantTimePoints", out var rptpsEl) && rptpsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in rptpsEl.EnumerateArray())
                {
                    var tp = el.Deserialize<RaceParticipantTimePoint>();
                    if (tp is not null)
                    {
                        if (tp.RaceID is not null && Races.TryGetValue(tp.RaceID.Value, out var race))
                        {
                            var existing = race.RaceParticipantTimePoints.FirstOrDefault(x => x.Id == tp.Id);
                            if (existing is null) race.RaceParticipantTimePoints.Add(tp);
                            else
                            {
                                var idx = race.RaceParticipantTimePoints.IndexOf(existing);
                                race.RaceParticipantTimePoints[idx] = tp;
                            }
                        }
                        else
                        {
                            // global unassigned -> track separately
                            var existing = UnassignedTimePoints.FirstOrDefault(x => x.Id == tp.Id);
                            if (existing is null) UnassignedTimePoints.Add(tp);
                            else
                            {
                                var idx = UnassignedTimePoints.IndexOf(existing);
                                UnassignedTimePoints[idx] = tp;
                            }
                        }
                    }
                }
            }

            // update last known timestamp to now (server sets LastModifiedUtc on save)
            await SetLastKnownForAsync(raceId, DateTime.UtcNow);
        }
        catch { }
    }

    private Task OnRaceDeleted(Guid id)
    {
        Races.Remove(id);
        return Task.CompletedTask;
    }

    private Task OnUnassignedTimePointAdded(object payload)
    {
        try
        {
            var el = (JsonElement)payload;
            var tp = el.Deserialize<RaceParticipantTimePoint>();
            if (tp is not null)
            {
                UnassignedTimePoints.Add(tp);
            }
        }
        catch { }
        return Task.CompletedTask;
    }

    private Task OnTimePointAssigned(object payload)
    {
        try
        {
            var el = (JsonElement)payload;
            // payload: [raceId, participantId, timePoint]
            if (el.ValueKind == JsonValueKind.Array && el.GetArrayLength() >= 3)
            {
                var raceId = el[0].GetGuid();
                var tp = el[2].Deserialize<RaceParticipantTimePoint>();
                if (tp is not null && Races.TryGetValue(raceId, out var race))
                {
                    var existing = race.RaceParticipantTimePoints.FirstOrDefault(x => x.Id == tp.Id);
                    if (existing is null) race.RaceParticipantTimePoints.Add(tp);
                    else
                    {
                        var idx = race.RaceParticipantTimePoints.IndexOf(existing);
                        race.RaceParticipantTimePoints[idx] = tp;
                    }
                }
            }
        }
        catch { }
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _signalR.DisposeAsync();
    }
}
