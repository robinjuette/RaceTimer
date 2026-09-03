using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using RaceTimer.Shared.Services;
using RaceTimerServer.Configuration;

namespace RaceTimerServer.Hubs;

/// <summary>
/// SignalR Hub für Echtzeit-Benachrichtigungen über Repository-Änderungen.
/// Clients verbinden sich mit diesem Hub, um Push-Updates für Rassen- und Teilnehmeränderungen zu erhalten.
/// </summary>
public class RaceTimerHub(IRaceRepository repository, ILogger<RaceTimerHub> logger) : Hub
{
    /// <summary>
    /// Wird aufgerufen, wenn ein Client sich mit dem Hub verbindet.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("SignalR-Client verbunden: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Wird aufgerufen, wenn ein Client die Verbindung trennt.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            logger.LogWarning(exception, "SignalR-Client mit Fehler getrennt: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            logger.LogInformation("SignalR-Client getrennt: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Client ruft diese Methode auf, um sich für Änderungen eines Rennens zu abonnieren.
    /// </summary>
    /// <param name="raceId">Die ID des Rennens, das überwacht werden soll</param>
    [Authorize(Policy = AuthorizationPolicies.CanViewAllResults)]
    public async Task SubscribeToRaceChanges(Guid raceId)
    {
        if (await repository.GetRaceAsync(raceId) is null)
        {
            logger.LogWarning("Ungültiges Rennabonnement abgewiesen: {ConnectionId}", Context.ConnectionId);
            throw new HubException("Rennen nicht gefunden.");
        }
        var groupName = GetRaceGroupName(raceId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        logger.LogInformation("Rennabonnement erstellt: {ConnectionId} für {RaceId}", Context.ConnectionId, raceId);
    }

    /// <summary>
    /// Client ruft diese Methode auf, um ein Rennabonnement zu beenden.
    /// </summary>
    /// <param name="raceId">Die ID des Rennens</param>
    [Authorize(Policy = AuthorizationPolicies.CanViewAllResults)]
    public async Task UnsubscribeFromRaceChanges(Guid raceId)
    {
        var groupName = GetRaceGroupName(raceId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        logger.LogInformation("Rennabonnement beendet: {ConnectionId} für {RaceId}", Context.ConnectionId, raceId);
    }

    /// <summary>
    /// Interne Methode: Server ruft diese auf, um eine Änderung an alle Clients zu broadcasten.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.CanManageRaces)]
    public async Task BroadcastRaceChange(Guid raceId, RepositoryChangedEventArgs change)
    {
        var groupName = GetRaceGroupName(raceId);
        await Clients.Group(groupName).SendAsync("RaceChanged", change);
        logger.LogDebug("Rennänderung an Gruppe gesendet: {RaceId}", raceId);
    }

    /// <summary>
    /// Interne Methode: Server ruft diese auf, um globale (nicht-rennspezifische) Änderungen zu broadcasten.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.CanManageRaces)]
    public async Task BroadcastGlobalChange(RepositoryChangedEventArgs change)
    {
        await Clients.All.SendAsync("GlobalChanged", change);
        logger.LogDebug("Globale Änderung an geschützte Clients gesendet.");
    }

    /// <summary>
    /// Helper-Methode: Generiert den Group-Namen für ein Rennen.
    /// </summary>
    private static string GetRaceGroupName(Guid raceId) => $"race-{raceId}";
}
