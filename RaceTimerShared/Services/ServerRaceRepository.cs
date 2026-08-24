using Microsoft.Extensions.Logging;
using RaceTimer.Shared.Http;
using RaceTimer.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaceTimer.Shared.Services;

/// <summary>
/// Server-basiertes Race Repository, das REST-API und SignalR für Änderungsbenachrichtigungen kombiniert.
/// Implementiert IRaceRepository für vollständige Feature-Parity mit CoreRaceRepository
/// und IRepositoryChangeNotifier für Event-basierte Änderungsbenachrichtigungen.
/// </summary>
public class ServerRaceRepository : IRaceRepository, IRepositoryChangeNotifier, IAsyncDisposable
{
    private readonly IRaceTimerApiClient _apiClient;
    private readonly SignalRSyncService _signalRSync;
    private readonly ILogger<ServerRaceRepository> _logger;
    private readonly object _subscriptionLock = new();

    private bool _isConnected = false;
    private bool _isSubscribed => _raceSubscriptions.Count > 0;

    private List<Guid> _raceSubscriptions = new();

    public Uri ServerUri
    {
        set
        {
            if (_isConnected || _isSubscribed)
            {
                throw new InvalidOperationException("Unsubscribe first!");
            }

            _apiClient.ServerUri = value;
            _signalRSync.ServerUri = value;
        }
    }

    public event EventHandler<RepositoryChangedEventArgs>? RepositoryChanged;

    public ServerRaceRepository(
        IRaceTimerApiClient apiClient,
        SignalRSyncService signalRSync,
        ILogger<ServerRaceRepository> logger)
    {
        _apiClient = apiClient;
        _signalRSync = signalRSync;
        _logger = logger;

        // Subscribe to SignalR events
        _signalRSync.RepositoryChanged += OnSignalRChanged;
    }

    #region Lifecycle Management

    /// <summary>
    /// Verbindet sich mit dem Server und SignalR-Hub.
    /// </summary>
    public async Task SubscribeAsync(Guid RaceId, CancellationToken cancellationToken = default)
    {
        if (_raceSubscriptions.Contains(RaceId))
        {
            throw new InvalidOperationException("Already subscribed!");
        }

        try
        {
            if (!_isConnected)
            {
                await ConnectAsync(cancellationToken);
            }

            await _signalRSync.SubscribeToRaceChangesAsync(RaceId, cancellationToken);

            lock (_subscriptionLock)
            {
                _raceSubscriptions.Add(RaceId);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to race changes");
            throw;
        }

    }

    private async Task ConnectAsync(CancellationToken cancellationToken = default)
    {

        try
        {
            // Connect to SignalR hub
            var connected = await _signalRSync.ConnectAsync(cancellationToken);
            if (!connected)
            {
                throw new InvalidOperationException("Failed to connect to SignalR hub");
            }

            lock (_subscriptionLock)
            {
                _isConnected = true;
            }

            _logger.LogInformation("Connected to signalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to signalR server");
            throw;
        }
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_isSubscribed)
        {
            throw new InvalidOperationException("Unsubscribe from races first!");
        }

        try
        {
            await _signalRSync.DisconnectAsync();

            lock (_subscriptionLock)
            {
                _isConnected = false;
            }

            _logger.LogInformation("Disconnected from signalR");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnection from server");
        }
    }

    /// <summary>
    /// Trennt die Verbindung zum Server.
    /// </summary>
    public async Task UnsubscribeAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_subscriptionLock)
        {
            if (!_isSubscribed)
                return;
        }

        try
        {
            foreach(Guid raceId in _raceSubscriptions.ToList())
            {
                await UnsubscribeAsync(raceId, cancellationToken);
            }

            _logger.LogInformation("Unsubscribed from all server repository changes");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during unsubscription from server");
        }
    }

    /// <summary>
    /// Trennt die Verbindung zum Server.
    /// </summary>
    public async Task UnsubscribeAsync(Guid raceID, CancellationToken cancellationToken = default)
    {
        lock (_subscriptionLock)
        {
            if (!_isSubscribed)
                return;
        }

        if (!_raceSubscriptions.Contains(raceID))
        {
            throw new InvalidOperationException("Not subscribed!");
        }

        try
        {
            await _signalRSync.UnsubscribeFromRaceChangesAsync(raceID, cancellationToken);

            lock (_subscriptionLock)
            {
                _raceSubscriptions.Remove(raceID);
            }

            _logger.LogInformation("Unsubscribed from server repository changes");

            if (!_isSubscribed)
            {
                await DisconnectAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during unsubscription from server");
        }
    }

    #endregion

    #region Races

    public async Task<Race?> AddRaceAsync(string name)
    {
        var race = await _apiClient.CreateRaceAsync(name);
        return race;
    }

    public async Task<Race?> GetRaceAsync(Guid id)
    {
        return await _apiClient.GetRaceAsync(id);
    }

    public async Task<IEnumerable<Race>> GetAllRacesAsync()
    {
        return await _apiClient.GetRacesAsync();
    }

    public async Task<IEnumerable<Race>> GetRacesByStatusAsync(RaceStatus status)
    {
        return await _apiClient.GetRacesByStatusAsync(status.ToString());
    }

    public async Task UpdateRaceAsync(Race race)
    {
        await _apiClient.UpdateRaceAsync(race);
    }

    public async Task<bool> DeleteRaceAsync(Guid id)
    {
        return await _apiClient.DeleteRaceAsync(id);
    }

    public async Task<bool> StartRaceAsync(Guid raceId, DateTime timePointUtc, params List<Guid> participantIds)
    {
        return await _apiClient.StartRaceAsync(raceId, participantIds, timePointUtc);
    }

    #endregion

    #region Participants

    public async Task<Participant?> CreateParticipantAsync(string name)
    {
        return await _apiClient.CreateParticipantAsync(name);
    }

    public async Task<Participant?> GetParticipantAsync(Guid id)
    {
        return await _apiClient.GetParticipantAsync(id);
    }

    public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
    {
        return await _apiClient.GetParticipantsAsync();
    }

    public async Task<Participant?> TryFindParticipantAsync(string name)
    {
        var participants = await _apiClient.GetParticipantsAsync();
        return participants.FirstOrDefault(p => p.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task UpdateParticipantAsync(Participant participant)
    {
        await _apiClient.UpdateParticipantAsync(participant);
    }

    public async Task DeleteParticipantAsync(Guid id)
    {
        await _apiClient.DeleteParticipantAsync(id);
    }

    #endregion

    #region Race Participants

    public async Task<RaceParticipant> AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
    {
        var result = await _apiClient.AssignParticipantToRaceAsync(raceId, participantId);
        return result ?? new RaceParticipant { RaceID = raceId, ParticipantID = participantId };
    }

    public async Task<IEnumerable<RaceParticipant>> GetRacesParticipantsAsync(Guid raceId)
    {
        return await _apiClient.GetRaceParticipantsAsync(raceId);
    }

    public async Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
    {
        return await _apiClient.RemoveParticipantFromRaceAsync(raceId, participantId);
    }

    #endregion

    #region Race Time Points

    public async Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name)
    {
        return await _apiClient.CreateRaceTimePointAsync(raceId, name);
    }

    public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
    {
        return await _apiClient.GetRaceTimePointsAsync(raceId);
    }

    public async Task<bool> UpdateTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints)
    {
        return await _apiClient.UpdateRaceTimePointsAsync(raceId, timePoints);
    }

    public async Task<bool> DeleteRaceTimePointAsync(Guid timePointId)
    {
        return await _apiClient.DeleteRaceTimePointAsync(Guid.Empty, timePointId);
    }

    public async Task<bool> CopyRaceTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo)
    {
        return await _apiClient.CopyTimePointsAsync(raceIdCopyFrom, raceIdCopyTo);
    }

    #endregion

    #region Race Participant Time Points

    public async Task<RaceParticipantTimePoint> AddUnassignedTimePointAsync(DateTime timePointUtc)
    {
        var result = await _apiClient.CreateUnassignedTimePointAsync(timePointUtc);
        return result ?? new RaceParticipantTimePoint { TimePointUTC = timePointUtc };
    }

    public async Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id)
    {
        return await _apiClient.GetRaceParticipantTimePointAsync(id);
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsForRaceAsync(Guid raceId)
    {
        return await _apiClient.GetRaceParticipantTimePointsAsync(raceId);
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimepointsAsync()
    {
        return await _apiClient.GetUnassignedTimePointsAsync();
    }

    public async Task<bool> AssignTimePointToRaceParticipantAsync(Guid timePointId, Guid raceId, Guid participantId)
    {
        return await _apiClient.AssignTimePointToParticipantAsync(timePointId, participantId, raceId);
    }

    public async Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId)
    {
        return await _apiClient.DeleteRaceParticipantTimePointAsync(timePointId);
    }

    public async Task<bool> SetRaceParticipantTimePointPenaltyTime(Guid timePointId, TimeSpan penaltyTime)
    {
        return await _apiClient.SetTimePointPenaltyAsync(timePointId, penaltyTime);
    }

    public async Task<bool> CorrectTimePointAsync(Guid timePointId, DateTime correctedTimeUTC, string reason, string? correctedByUser = null)
    {
        return await _apiClient.CorrectTimePointAsync(timePointId, correctedTimeUTC, reason, correctedByUser);
    }

    public async Task<bool> UndoTimePointCorrectionAsync(Guid timePointId)
    {
        return await _apiClient.UndoTimePointCorrectionAsync(timePointId);
    }

    #endregion

    #region Change Tracking

    public async Task<object> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc)
    {
        var result = await _apiClient.GetChangesSinceAsync(raceId, sinceUtc);
        return result ?? new object();
    }

    public async Task CheckForRaceCompletionAsync(Guid raceId)
    {
        await _apiClient.FinishRaceAsync(raceId);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Handler: wird aufgerufen, wenn SignalR ein Event empfängt.
    /// Reicht das Event an den lokalen Event-Handler weiter.
    /// </summary>
    private void OnSignalRChanged(object? sender, RepositoryChangedEventArgs e)
    {
        _logger.LogDebug("Forwarding SignalR change event: {ChangeType} - {EntityType} ({EntityId})", 
            e.ChangeType, e.EntityType, e.EntityId);

        RepositoryChanged?.Invoke(this, e);
    }

    #endregion

    #region Disposal

    public async ValueTask DisposeAsync()
    {
        await UnsubscribeAllAsync();
        _signalRSync.RepositoryChanged -= OnSignalRChanged;
        GC.SuppressFinalize(this);
    }

    #endregion
}
