using Microsoft.AspNetCore.SignalR;
using RaceTimer.Shared.Services;
using RaceTimerServer.Hubs;

namespace RaceTimerServer.Services;

/// <summary>
/// Service für die Verwaltung und das Broadcasting von Repository-Änderungen über SignalR.
/// Verbindet das Server-seitige CoreRaceRepository mit dem RaceTimerHub.
/// </summary>
public class RepositoryChangeNotificationService : IDisposable
{
    private readonly IRaceRepository _repository;
    private readonly IHubContext<RaceTimerHub> _hubContext;
    private readonly ILogger<RepositoryChangeNotificationService> _logger;

    public RepositoryChangeNotificationService(
        IRaceRepository repository,
        IHubContext<RaceTimerHub> hubContext,
        ILogger<RepositoryChangeNotificationService> logger)
    {
        _repository = repository;
        _hubContext = hubContext;
        _logger = logger;

        // Subscribe to repository changes if it supports IRepositoryChangeNotifier
        if (_repository is IRepositoryChangeNotifier changeNotifier)
        {
            changeNotifier.RepositoryChanged += OnRepositoryChanged;
            _logger.LogInformation("Repository change notification service initialized");
        }
        else
        {
            _logger.LogWarning("Repository does not implement IRepositoryChangeNotifier");
        }
    }

    /// <summary>
    /// Handler: wird aufgerufen, wenn sich das Repository ändert.
    /// Broadcastet die Änderung zu allen verbundenen SignalR-Clients.
    /// </summary>
    private async void OnRepositoryChanged(object? sender, RepositoryChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Repository changed: {ChangeType} - {EntityType} ({EntityId})", 
                e.ChangeType, e.EntityType, e.EntityId);

            // Determine if this is a race-specific or global change
            if (e.EntityType == "Race" && e.EntityId.HasValue)
            {
                // Send to race-specific group
                await _hubContext.Clients
                    .Group($"race-{e.EntityId}")
                    .SendAsync("RaceChanged", e);
            }
            else if (e.EntityType == "Participant" || e.EntityType == "RaceParticipant" || 
                     e.EntityType == "RaceTimePoint" || e.EntityType == "RaceParticipantTimePoint")
            {
                // Send globally (or to specific race if EntityId represents raceId)
                if (e.EntityId.HasValue)
                {
                    await _hubContext.Clients
                        .Group($"race-{e.EntityId}")
                        .SendAsync("RaceChanged", e);
                }
                else
                {
                    await _hubContext.Clients.All.SendAsync("GlobalChanged", e);
                }
            }
            else
            {
                // Send globally for unknown types
                await _hubContext.Clients.All.SendAsync("GlobalChanged", e);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting repository change");
        }
    }

    public void Dispose()
    {
        if (_repository is IRepositoryChangeNotifier changeNotifier)
        {
            changeNotifier.RepositoryChanged -= OnRepositoryChanged;
        }
    }
}
