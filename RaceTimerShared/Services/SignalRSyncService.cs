using System;
using System.Threading.Tasks;

namespace RaceTimer.Shared.Services
{
    // Minimal placeholder implementation to allow compilation.
    // Full implementation (SignalR client, event forwarding) will be added in step 3.
    public class SignalRSyncService
    {
        public bool IsConnected { get; private set; }

        private readonly string _serverUrl;

        public SignalRSyncService(string serverUrl)
        {
            _serverUrl = serverUrl;
            IsConnected = false;
        }

        public Task<bool> ConnectAsync()
        {
            // placeholder: do not attempt to connect yet
            IsConnected = false;
            return Task.FromResult(false);
        }

        public Task DisconnectAsync()
        {
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task SubscribeToRaceAsync(Guid raceId)
        {
            return Task.CompletedTask;
        }

        public Task UnsubscribeFromRaceAsync(Guid raceId)
        {
            return Task.CompletedTask;
        }
    }
}
