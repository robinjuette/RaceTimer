using System.Net.Http.Json;
using RaceTimer.Shared.Models;

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

    public async Task UpdateRaceAsync(Race race)
    {
        await _http.PutAsJsonAsync($"api/races/{race.Id}", race);
    }

    public async Task DeleteRaceAsync(Guid id)
    {
        await _http.DeleteAsync($"api/races/{id}");
    }
}
