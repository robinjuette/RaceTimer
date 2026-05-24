using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace RaceTimerClientApp.Web.Client.Services;

public class RaceSignalRService : IAsyncDisposable
{
    private HubConnection _connection;

    public event Func<Guid, Task>? RaceDeleted;
    public event Func<object, Task>? RaceUpdated;
    public event Func<object, Task>? RaceCreated;
    public event Func<object, Task>? ParticipantUpdated;
    public event Func<object, Task>? ParticipantCreated;
    public event Func<object, Task>? UnassignedTimePointAdded;
    public event Func<object, Task>? TimePointAssigned;
    public event Func<bool, Task>? ConnectionStateChanged;

    public bool IsConnected { get; private set; }
    public event Func<string, Task>? Reconnected;

    public RaceSignalRService(NavigationManager navigation)
    {
        var uri = navigation.ToAbsoluteUri("/raceHub");
        InitConnection(new HubConnectionBuilder().WithUrl(uri).WithAutomaticReconnect().Build());
    }

    // constructor that accepts full hub url (for platforms without NavigationManager)
    public RaceSignalRService(string hubUrl)
    {
        var conn = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
        InitConnection(conn);
    }

    private void InitConnection(HubConnection connection)
    {
        _connection = connection;

        _connection.On<Guid>("RaceDeleted", async (id) => { if (RaceDeleted != null) await RaceDeleted(id); });
        _connection.On<JsonElement>("RaceUpdated", async (payload) => { if (RaceUpdated != null) await RaceUpdated(payload); });
        _connection.On<JsonElement>("RaceCreated", async (payload) => { if (RaceCreated != null) await RaceCreated(payload); });
        _connection.On<JsonElement>("ParticipantUpdated", async (payload) => { if (ParticipantUpdated != null) await ParticipantUpdated(payload); });
        _connection.On<JsonElement>("ParticipantCreated", async (payload) => { if (ParticipantCreated != null) await ParticipantCreated(payload); });
        _connection.On<JsonElement>("UnassignedTimePointAdded", async (payload) => { if (UnassignedTimePointAdded != null) await UnassignedTimePointAdded(payload); });
        _connection.On<JsonElement>("TimePointAssigned", async (payload) => { if (TimePointAssigned != null) await TimePointAssigned(payload); });

        _connection.Reconnected += async (connectionId) =>
        {
            IsConnected = true;
            if (ConnectionStateChanged != null) await ConnectionStateChanged(true);
            if (Reconnected != null) await Reconnected(connectionId);
        };

        _connection.Reconnecting += async (ex) =>
        {
            IsConnected = false;
            if (ConnectionStateChanged != null) await ConnectionStateChanged(false);
        };

        _connection.Closed += async (ex) =>
        {
            IsConnected = false;
            if (ConnectionStateChanged != null) await ConnectionStateChanged(false);
        };
    }

    public async Task StartAsync() => await _connection.StartAsync();
    public async Task StopAsync() => await _connection.StopAsync();

    public async Task SubscribeRaceAsync(Guid raceId)
    {
        await _connection.InvokeAsync("SubscribeRace", raceId.ToString());
    }

    public async Task UnsubscribeRaceAsync(Guid raceId)
    {
        await _connection.InvokeAsync("UnsubscribeRace", raceId.ToString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
