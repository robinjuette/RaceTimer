using Microsoft.Extensions.Logging;
using RaceTimer.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RaceTimer.Shared.Services
{
    /// <summary>
    /// Runtime-switchable repository proxy.
    /// Delegates all IRaceRepository calls to the currently active repository (local or server).
    /// Manages clean subscription/unsubscription during mode transitions.
    /// Thread-safe and supports disposal.
    /// </summary>
    public class ConfiguredConnectionRepository : IRaceRepository, IRepositoryChangeNotifier, IAsyncDisposable
    {
        private readonly ILogger<ConfiguredConnectionRepository> _logger;
        private readonly object _switchLock = new object();

        private IRaceRepository _currentRepository;
        private IRepositoryChangeNotifier? _currentChangeNotifier;
        private EventHandler<RepositoryChangedEventArgs>? _forwardedHandler;
        private bool _disposed;

        /// <summary>
        /// Initializes with an initial repository (e.g., CoreRaceRepository or ServerRaceRepository).
        /// </summary>
        public ConfiguredConnectionRepository(
            IRaceRepository initialRepository,
            IRepositoryChangeNotifier? initialChangeNotifier = null,
            ILogger<ConfiguredConnectionRepository>? logger = null)
        {
            _currentRepository = initialRepository ?? throw new ArgumentNullException(nameof(initialRepository));
            _currentChangeNotifier = initialChangeNotifier ?? (initialRepository as IRepositoryChangeNotifier);
            _logger = logger;

            if (_logger == null)
            {
                // Create a simple no-op logger fallback
                _logger = new SimpleLogger<ConfiguredConnectionRepository>();
            }

            // Subscribe to change notifications from the initial repository
            if (_currentChangeNotifier != null)
            {
                _forwardedHandler = (s, e) => RepositoryChanged?.Invoke(this, e);
                _currentChangeNotifier.RepositoryChanged += _forwardedHandler;
            }
        }

        /// <summary>
        /// Switches to a different repository implementation and subscription at runtime.
        /// Unsubscribes from the old repository and subscribes to the new one.
        /// </summary>
        public async Task SwitchRepositoryAsync(
            IRaceRepository newRepository,
            IRepositoryChangeNotifier? newChangeNotifier = null,
            CancellationToken cancellationToken = default)
        {
            if (newRepository == null)
                throw new ArgumentNullException(nameof(newRepository));

            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));

                _logger.LogInformation("Switching repository: from {OldType} to {NewType}",
                    _currentRepository?.GetType().Name ?? "null",
                    newRepository.GetType().Name);

                // Step 1: Unsubscribe from the old repository's change events
                if (_currentChangeNotifier != null && _forwardedHandler != null)
                {
                    _currentChangeNotifier.RepositoryChanged -= _forwardedHandler;
                    _logger.LogDebug("Unsubscribed from old repository change notifications");
                }

                // Step 2: Async cleanup on the old change notifier (e.g., disconnect SignalR)
                if (_currentChangeNotifier is IAsyncDisposable oldDisposable)
                {
                    // Schedule async cleanup without blocking the lock
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await oldDisposable.DisposeAsync();
                            _logger.LogDebug("Disposed old repository resources");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error disposing old repository");
                        }
                    });
                }

                // Step 3: Switch to new repository
                _currentRepository = newRepository;
                _currentChangeNotifier = newChangeNotifier ?? (newRepository as IRepositoryChangeNotifier);

                // Step 4: Subscribe to the new repository's change events
                if (_currentChangeNotifier != null)
                {
                    _forwardedHandler = (s, e) => RepositoryChanged?.Invoke(this, e);
                    _currentChangeNotifier.RepositoryChanged += _forwardedHandler;
                    _logger.LogDebug("Subscribed to new repository change notifications");
                }
            }

            // Step 5: Async subscription to new change notifier (e.g., connect SignalR)
            if (_currentChangeNotifier != null)
            {
                try
                {
                    await _currentChangeNotifier.SubscribeAsync(cancellationToken);
                    _logger.LogInformation("Successfully subscribed to new repository changes");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error subscribing to new repository changes");
                    throw;
                }
            }
        }

        /// <summary>
        /// Event fired when the currently active repository reports a change.
        /// </summary>
        public event EventHandler<RepositoryChangedEventArgs>? RepositoryChanged;

        /// <summary>
        /// Subscribes to change notifications from the current repository.
        /// For ConfiguredConnectionRepository, this is handled during repository switch.
        /// </summary>
        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }

            if (_currentChangeNotifier != null)
            {
                await _currentChangeNotifier.SubscribeAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Unsubscribes from change notifications from the current repository.
        /// </summary>
        public async Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }

            if (_currentChangeNotifier != null)
            {
                await _currentChangeNotifier.UnsubscribeAsync(cancellationToken);
            }
        }

        // ===== IRaceRepository Delegation =====

        public async Task<Race?> AddRaceAsync(string name)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.AddRaceAsync(name);
        }

        public async Task<RaceParticipantTimePoint> AddUnassignedTimePointAsync(DateTime timePointUtc)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.AddUnassignedTimePointAsync(timePointUtc);
        }

        public async Task<RaceParticipant> AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.AssignParticipantToRaceAsync(raceId, participantId);
        }

        public async Task<bool> AssignTimePointToRaceParticipantAsync(Guid timePointId, Guid raceId, Guid participantId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.AssignTimePointToRaceParticipantAsync(timePointId, raceId, participantId);
        }

        public async Task<Participant?> CreateParticipantAsync(string name)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.CreateParticipantAsync(name);
        }

        public async Task DeleteParticipantAsync(Guid id)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            await _currentRepository.DeleteParticipantAsync(id);
        }

        public async Task<bool> DeleteRaceAsync(Guid id)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.DeleteRaceAsync(id);
        }

        public async Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.DeleteRaceParticipantTimePointAsync(timePointId);
        }

        public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetAllParticipantsAsync();
        }

        public async Task<IEnumerable<Race>> GetAllRacesAsync()
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetAllRacesAsync();
        }

        public async Task<object> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetChangesSinceAsync(raceId, sinceUtc);
        }

        public async Task<Participant?> TryFindParticipantAsync(string name)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.TryFindParticipantAsync(name);
        }

        public async Task<Participant?> GetParticipantAsync(Guid id)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetParticipantAsync(id);
        }

        public async Task<Race?> GetRaceAsync(Guid id)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetRaceAsync(id);
        }

        public async Task<IEnumerable<Race>> GetRacesByStatusAsync(RaceStatus status)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetRacesByStatusAsync(status);
        }

        public async Task<IEnumerable<RaceParticipant>> GetRacesParticipantsAsync(Guid id)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetRacesParticipantsAsync(id);
        }

        public async Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetRaceParticipantTimePointAsync(id);
        }

        public async Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsForRaceAsync(Guid raceId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetRaceParticipantTimePointsForRaceAsync(raceId);
        }

        public async Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimepointsAsync()
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetUnassignedTimepointsAsync();
        }

        public async Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.RemoveParticipantFromRaceAsync(raceId, participantId);
        }

        public async Task<bool> SetRaceParticipantTimePointPenaltyTime(Guid timePointId, TimeSpan penaltyTime)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.SetRaceParticipantTimePointPenaltyTime(timePointId, penaltyTime);
        }

        public async Task<bool> StartRaceAsync(Guid raceId, DateTime timePointUtc, params List<Guid> participantIds)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.StartRaceAsync(raceId, timePointUtc, participantIds);
        }

        public async Task UpdateParticipantAsync(Participant participant)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            await _currentRepository.UpdateParticipantAsync(participant);
        }

        public async Task UpdateRaceAsync(Race race)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            await _currentRepository.UpdateRaceAsync(race);
        }

        public async Task<bool> UpdateTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.UpdateTimePointsAsync(raceId, timePoints);
        }

        public async Task<bool> CopyRaceTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.CopyRaceTimePointsAsync(raceIdCopyFrom, raceIdCopyTo);
        }

        public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.GetRaceTimePointsAsync(raceId);
        }

        public async Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.CreateRaceTimePointAsync(raceId, name);
        }

        public async Task<bool> DeleteRaceTimePointAsync(Guid timePointId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.DeleteRaceTimePointAsync(timePointId);
        }

        public async Task CheckForRaceCompletionAsync(Guid raceId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            await _currentRepository.CheckForRaceCompletionAsync(raceId);
        }

        public async Task<bool> CorrectTimePointAsync(Guid timePointId, DateTime correctedTimeUTC, string reason, string? correctedByUser = null)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.CorrectTimePointAsync(timePointId, correctedTimeUTC, reason, correctedByUser);
        }

        public async Task<bool> UndoTimePointCorrectionAsync(Guid timePointId)
        {
            lock (_switchLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ConfiguredConnectionRepository));
            }
            return await _currentRepository.UndoTimePointCorrectionAsync(timePointId);
        }

        // ===== Disposal =====

        public async ValueTask DisposeAsync()
        {
            lock (_switchLock)
            {
                if (_disposed)
                    return;

                _disposed = true;
            }

            // Unsubscribe
            if (_currentChangeNotifier != null && _forwardedHandler != null)
            {
                _currentChangeNotifier.RepositoryChanged -= _forwardedHandler;
            }

            // Dispose current repository if it implements IAsyncDisposable
            if (_currentRepository is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync();
            }

            _logger.LogInformation("ConfiguredConnectionRepository disposed");
        }
    }
}

/// <summary>
/// Simple fallback logger implementation for cases where no logger is provided.
/// </summary>
internal class SimpleLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
