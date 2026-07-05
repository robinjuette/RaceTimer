using System;
using System.Threading;
using System.Threading.Tasks;

namespace RaceTimer.Shared.Services
{
    public interface IRepositoryChangeNotifier
    {
        /// <summary>
        /// Fired when a repository reports a change.
        /// </summary>
        event EventHandler<RepositoryChangedEventArgs>? RepositoryChanged;

        /// <summary>
        /// Optional: Subscribe to server-side change stream (SignalR etc.).
        /// Implementations that don't require an explicit subscription may be no-ops.
        /// </summary>
        Task SubscribeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Optional: Unsubscribe from server-side change stream.
        /// </summary>
        Task UnsubscribeAsync(CancellationToken cancellationToken = default);
    }
}
