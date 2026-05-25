using Microsoft.AspNetCore.SignalR.Client;
using RaceTimer.Shared.Models;

namespace RaceTimer.Shared.Services;

/// <summary>
/// Service für optionale SignalR-Verbindung zu externem Server.
/// Ermöglicht Real-Time Synchronisierung von Änderungen.
/// </summary>
public class SignalRSyncService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly string _serverUrl;
    private bool _isConnected;

    // Events für Änderungen
    public event Func<object, Task>? OnRaceChanged;
    public event Func<object, Task>? OnParticipantChanged;
    public event Func<object, Task>? OnRaceParticipantChanged;
    public event Func<object, Task>? OnTimePointChanged;

    public SignalRSyncService(string serverUrl)
    {
        _serverUrl = serverUrl;
        _isConnected = false;
    }

    public bool IsConnected => _isConnected && _hubConnection?.State == HubConnectionState.Connected;

    /// <summary>
    /// Verbindung zum Server herstellen
    /// </summary>
    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (_hubConnection != null)
                await _hubConnection.DisposeAsync();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_serverUrl}/raceHub")
                .WithAutomaticReconnect()
                .Build();

            // Listener für Server-Events
            _hubConnection.On<object>("RaceChanged", async (data) =>
            {
                if (OnRaceChanged != null)
                    await OnRaceChanged.Invoke(data);
            });

            _hubConnection.On<object>("ParticipantChanged", async (data) =>
            {
                if (OnParticipantChanged != null)
                    await OnParticipantChanged.Invoke(data);
            });

            _hubConnection.On<object>("RaceParticipantChanged", async (data) =>
            {
                if (OnRaceParticipantChanged != null)
                    await OnRaceParticipantChanged.Invoke(data);
            });

            _hubConnection.On<object>("TimePointChanged", async (data) =>
            {
                if (OnTimePointChanged != null)
                    await OnTimePointChanged.Invoke(data);
            });

            await _hubConnection.StartAsync();
            _isConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SignalR connection failed: {ex.Message}");
            _isConnected = false;
            return false;
        }
    }

    /// <summary>
    /// Ein Rennen zum Empfangen von Updates abonnieren
    /// </summary>
    public async Task SubscribeToRaceAsync(Guid raceId)
    {
        if (IsConnected && _hubConnection != null)
        {
            try
            {
                await _hubConnection.InvokeAsync("SubscribeRace", raceId.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to subscribe to race: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Abonnement für ein Rennen beenden
    /// </summary>
    public async Task UnsubscribeFromRaceAsync(Guid raceId)
    {
        if (IsConnected && _hubConnection != null)
        {
            try
            {
                await _hubConnection.InvokeAsync("UnsubscribeRace", raceId.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to unsubscribe from race: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Verbindung trennen
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _isConnected = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
