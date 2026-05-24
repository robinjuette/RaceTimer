using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using Microsoft.Maui.Storage;
using RaceTimer.Shared.Models;

namespace RaceTimerClientApp.Web.Client.Services
{
    public class RaceStateService : IAsyncDisposable
    {
        private readonly RaceSignalRService _signalR;
        private readonly HttpClient _http;

        public Dictionary<Guid, Race> Races { get; } = new();
        public List<RaceParticipantTimePoint> UnassignedTimePoints { get; } = new();

        private const string LastOpenedKey = "racetimer.lastopened";
        private const string LastKnownKeyPrefix = "racetimer.lastknown.";

        public RaceStateService(RaceSignalRService signalR, HttpClient http)
        {
            _signalR = signalR;
            _http = http;
        }

        public async Task InitializeAsync()
        {
            await _signalR.StartAsync();
            // No JS storage on MAUI; use Preferences
            try
            {
                var json = Preferences.Get(LastOpenedKey, string.Empty);
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
            var r = await _http.GetFromJsonAsync<Race>($"api/races/{id}");
            if (r is not null) Races[id] = r;
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
            Preferences.Set(LastOpenedKey, json);
        }

        public async ValueTask DisposeAsync()
        {
            await _signalR.DisposeAsync();
        }
    }
}
