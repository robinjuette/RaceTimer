using Microsoft.AspNetCore.SignalR;
using RaceTimer.Shared.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RaceTimerServer.Hubs;

/// <summary>
/// SignalR Hub für Echtzeit-Benachrichtigungen über Repository-Änderungen.
/// Clients verbinden sich mit diesem Hub, um Push-Updates für Rassen- und Teilnehmeränderungen zu erhalten.
/// </summary>
public class RaceTimerHub : Hub
{
    private readonly ILogger<RaceTimerHub> _logger;

    public RaceTimerHub(ILogger<RaceTimerHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Client sich mit dem Hub verbindet.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Client die Verbindung trennt.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception, $"Client disconnected with error: {Context.ConnectionId}");
        }
        else
        {
            _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client ruft diese Methode auf, um sich für Änderungen eines Rennens zu abonnieren.
    /// </summary>
    /// <param name="raceId">Die ID des Rennens, das überwacht werden soll</param>
    public async Task SubscribeToRaceChanges(Guid raceId)
    {
        var groupName = GetRaceGroupName(raceId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} subscribed to race {raceId}");
    }

    /// <summary>
    /// Client ruft diese Methode auf, um ein Rennabonnement zu beenden.
    /// </summary>
    /// <param name="raceId">Die ID des Rennens</param>
    public async Task UnsubscribeFromRaceChanges(Guid raceId)
    {
        var groupName = GetRaceGroupName(raceId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} unsubscribed from race {raceId}");
    }

    /// <summary>
    /// Interne Methode: Server ruft diese auf, um eine Änderung an alle Clients zu broadcasten.
    /// </summary>
    public async Task BroadcastRaceChange(Guid raceId, RepositoryChangedEventArgs change)
    {
        var groupName = GetRaceGroupName(raceId);
        await Clients.Group(groupName).SendAsync("RaceChanged", change);
        _logger.LogDebug($"Broadcasted change for race {raceId} to group {groupName}");
    }

    /// <summary>
    /// Interne Methode: Server ruft diese auf, um globale (nicht-rennspezifische) Änderungen zu broadcasten.
    /// </summary>
    public async Task BroadcastGlobalChange(RepositoryChangedEventArgs change)
    {
        await Clients.All.SendAsync("GlobalChanged", change);
        _logger.LogDebug("Broadcasted global change to all clients");
    }

    /// <summary>
    /// Helper-Methode: Generiert den Group-Namen für ein Rennen.
    /// </summary>
    private static string GetRaceGroupName(Guid raceId) => $"race-{raceId}";
}
