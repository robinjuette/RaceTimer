// Beispiele zur Verwendung des RaceTimerApiClient

// ============================================
// 1. Dependency Injection Setup (Program.cs)
// ============================================

// Für Blazor WebAssembly:
builder.Services.AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>(client =>
{
    client.BaseAddress = new Uri("https://racetimerserver.example.com");
    client.DefaultRequestHeaders.Add("User-Agent", "RaceTimerClient/1.0");
});

// Für MAUI:
services.AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>(client =>
{
    client.BaseAddress = new Uri("https://racetimerserver.local:5001");
});

// ============================================
// 2. Verwendung in Services/Components
// ============================================

public class RaceService
{
    private readonly IRaceTimerApiClient _apiClient;

    public RaceService(IRaceTimerApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Alle Rennen abrufen
    public async Task<IEnumerable<Race>> GetAllRacesAsync(CancellationToken ct = default)
    {
        return await _apiClient.GetRacesAsync(ct);
    }

    // Rennen mit Status-Filter
    public async Task<IEnumerable<Race>> GetRunningRacesAsync(CancellationToken ct = default)
    {
        return await _apiClient.GetRacesByStatusAsync("Running", ct);
    }

    // Neues Rennen erstellen
    public async Task<Race?> CreateRaceAsync(string name, CancellationToken ct = default)
    {
        return await _apiClient.CreateRaceAsync(name, ct);
    }

    // Rennen starten
    public async Task<bool> StartRaceAsync(Guid raceId, IEnumerable<Guid> participantIds, CancellationToken ct = default)
    {
        return await _apiClient.StartRaceAsync(raceId, participantIds, null, ct);
    }

    // Zeitpunkt korrigieren
    public async Task<bool> CorrectTimePointAsync(
        Guid timePointId, 
        DateTime correctedTime, 
        string reason, 
        CancellationToken ct = default)
    {
        return await _apiClient.CorrectTimePointAsync(timePointId, correctedTime, reason, null, ct);
    }
}

// ============================================
// 3. Fehlerbehandlung
// ============================================

public class SafeRaceService
{
    private readonly IRaceTimerApiClient _apiClient;
    private readonly ILogger<SafeRaceService> _logger;

    public SafeRaceService(IRaceTimerApiClient apiClient, ILogger<SafeRaceService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Race>> GetRacesWithErrorHandling()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            return await _apiClient.GetRacesAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("GetRaces request timed out");
            return [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while getting races");
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting races");
            return [];
        }
    }
}

// ============================================
// 4. Mit Retry-Strategie (Polly)
// ============================================

// Program.cs
builder.Services
    .AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>()
    .AddTransientHttpErrorPolicy()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 100));

// ============================================
// 5. Mit Logging
// ============================================

// Program.cs
builder.Services
    .AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.example.com");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}

// ============================================
// 6. Verwendung in Blazor Components
// ============================================

@page "/races"
@inject IRaceTimerApiClient ApiClient
@inject ILogger<Races> Logger

<h3>Rennen</h3>

@if (races == null)
{
    <p>Lädt...</p>
}
else if (!races.Any())
{
    <p>Keine Rennen vorhanden</p>
}
else
{
    <table class="table">
        <thead>
            <tr>
                <th>Name</th>
                <th>Status</th>
                <th>Aktionen</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var race in races)
            {
                <tr>
                    <td>@race.Name</td>
                    <td>@race.Status</td>
                    <td>
                        <button @onclick="() => DeleteRace(race.Id)">Löschen</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private IEnumerable<Race>? races;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            races = await ApiClient.GetRacesAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading races");
            races = [];
        }
    }

    private async Task DeleteRace(Guid raceId)
    {
        if (await ApiClient.DeleteRaceAsync(raceId))
        {
            races = await ApiClient.GetRacesAsync();
        }
    }
}

// ============================================
// 7. Offline-First Pattern (mit lokaler DB)
// ============================================

public class OfflineFirstRaceService
{
    private readonly IRaceTimerApiClient _apiClient;
    private readonly CoreRaceRepository _localRepo;
    private readonly ILogger<OfflineFirstRaceService> _logger;

    public OfflineFirstRaceService(
        IRaceTimerApiClient apiClient,
        CoreRaceRepository localRepo,
        ILogger<OfflineFirstRaceService> logger)
    {
        _apiClient = apiClient;
        _localRepo = localRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<Race>> GetRacesAsync()
    {
        try
        {
            // Versuche vom Server zu laden
            return await _apiClient.GetRacesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get races from server, using local cache");
            // Fallback auf lokale Datenbank
            return await _localRepo.GetAllRacesAsync();
        }
    }
}

// ============================================
// 8. Change Tracking / Differential Queries
// ============================================

public class IncrementalSyncService
{
    private readonly IRaceTimerApiClient _apiClient;
    private DateTime _lastSync = DateTime.UnixEpoch;

    public async Task SyncChangesAsync(Guid raceId)
    {
        var changes = await _apiClient.GetChangesSinceAsync(raceId, _lastSync);

        // Verarbeite nur neue Änderungen
        // ...

        _lastSync = DateTime.UtcNow;
    }
}
