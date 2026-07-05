using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RaceTimer.Shared.Services;

/// <summary>
/// Service für die Verwaltung der SignalR-Verbindung zu einem RaceTimer-Server.
/// Übersetzt SignalR-Events zu RepositoryChangedEventArgs und emittiert lokale Events.
/// </summary>
public class SignalRSyncService
{
    private HubConnection? _connection;
    private readonly string _serverUrl;
    private readonly ILogger<SignalRSyncService> _logger;
    private readonly object _connectionLock = new object();

    public bool IsConnected
    {
        get
        {
            lock (_connectionLock)
            {
                return _connection?.State == HubConnectionState.Connected;
            }
        }
    }

    /// <summary>
    /// Event wird ausgelöst, wenn ein Event vom Server empfangen wird.
    /// </summary>
    public event EventHandler<RepositoryChangedEventArgs>? RepositoryChanged;

    public SignalRSyncService(string serverUrl, ILogger<SignalRSyncService> logger)
    {
        _serverUrl = serverUrl;
        _logger = logger;
    }

    /// <summary>
    /// Verbindet sich mit dem SignalR-Hub des Servers.
    /// </summary>
    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_connectionLock)
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                _logger.LogInformation("Already connected to SignalR hub");
                return true;
            }
        }

        try
        {
            var hubUrl = new Uri(new Uri(_serverUrl), "/hubs/racetimer").ToString();

            lock (_connectionLock)
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                // Register event handlers
                _connection.On<RepositoryChangedEventArgs>("RaceChanged", OnRaceChanged);
                _connection.On<RepositoryChangedEventArgs>("GlobalChanged", OnGlobalChanged);
                _connection.Reconnecting += OnReconnecting;
                _connection.Reconnected += OnReconnected;
                _connection.Closed += OnClosed;
            }

            await _connection.StartAsync(cancellationToken);
            _logger.LogInformation("Connected to SignalR hub at {HubUrl}", hubUrl);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            lock (_connectionLock)
            {
                _connection?.DisposeAsync();
                _connection = null;
            }
            return false;
        }
    }

    /// <summary>
    /// Trennt die Verbindung zum SignalR-Hub.
    /// </summary>
    public async Task DisconnectAsync()
    {
        lock (_connectionLock)
        {
            if (_connection == null)
                return;

            try
            {
                _connection.Reconnecting -= OnReconnecting;
                _connection.Reconnected -= OnReconnected;
                _connection.Closed -= OnClosed;
            }
            catch { }
        }

        try
        {
            if (_connection != null)
            {
                await _connection.StopAsync();
                await _connection.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting from SignalR hub");
        }

        lock (_connectionLock)
        {
            _connection = null;
        }

        _logger.LogInformation("Disconnected from SignalR hub");
    }

    /// <summary>
    /// Abonniert Änderungen für ein spezifisches Rennen.
    /// </summary>
    public async Task SubscribeToRaceChangesAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        lock (_connectionLock)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("Cannot subscribe: not connected to SignalR hub");
                return;
            }
        }

        try
        {
            await _connection.InvokeAsync("SubscribeToRaceChanges", raceId, cancellationToken);
            _logger.LogInformation("Subscribed to changes for race {RaceId}", raceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to race changes for {RaceId}", raceId);
        }
    }

    /// <summary>
    /// Beendet das Abonnement für ein spezifisches Rennen.
    /// </summary>
    public async Task UnsubscribeFromRaceChangesAsync(Guid raceId, CancellationToken cancellationToken = default)
    {
        lock (_connectionLock)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("Cannot unsubscribe: not connected to SignalR hub");
                return;
            }
        }

        try
        {
            await _connection.InvokeAsync("UnsubscribeFromRaceChanges", raceId, cancellationToken);
            _logger.LogInformation("Unsubscribed from changes for race {RaceId}", raceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from race changes for {RaceId}", raceId);
        }
    }

    /// <summary>
    /// Handler: wird aufgerufen, wenn ein rennspezifisches Event empfangen wird.
    /// </summary>
    private void OnRaceChanged(RepositoryChangedEventArgs args)
    {
        _logger.LogDebug("Received race change event: {ChangeType} - {EntityType} ({EntityId})", 
            args.ChangeType, args.EntityType, args.EntityId);
        RepositoryChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Handler: wird aufgerufen, wenn ein globales Event empfangen wird.
    /// </summary>
    private void OnGlobalChanged(RepositoryChangedEventArgs args)
    {
        _logger.LogDebug("Received global change event: {ChangeType} - {EntityType} ({EntityId})", 
            args.ChangeType, args.EntityType, args.EntityId);
        RepositoryChanged?.Invoke(this, args);
    }

    /// <summary>
    /// Handler: wird aufgerufen, bevor die Verbindung neu hergestellt wird.
    /// </summary>
    private Task OnReconnecting(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR reconnecting...");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handler: wird aufgerufen, nachdem die Verbindung neu hergestellt wurde.
    /// </summary>
    private Task OnReconnected(string? connectionId)
    {
        _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handler: wird aufgerufen, wenn die Verbindung geschlossen wird.
    /// </summary>
    private Task OnClosed(Exception? exception)
    {
        _logger.LogWarning(exception, "SignalR connection closed");
        return Task.CompletedTask;
    }
}
