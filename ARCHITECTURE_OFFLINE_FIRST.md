# RaceTimer: Offline-First Architektur mit optionalem Server-Sync

## Übersicht

Die RaceTimer-Anwendung wurde refaktoriert, um eine **Offline-First Architektur** mit optionalem Server-Synchronisierung zu unterstützen:

- **Offline-Modus (Standard)**: Die MAUI-App funktioniert vollständig lokal mit einer SQLite-Datenbank
- **Online-Modus (Optional)**: Verbindung zu einem externen Server via SignalR für Echtzeit-Synchronisierung

## Architektur

### Projektstruktur

```
RaceTimer/
├── RaceTimerShared/                    # Gemeinsame Geschäftslogik und Daten
│   ├── Data/
│   │   ├── RaceTimerDbContext.cs       # Shared EF Core DbContext
│   │   └── RaceTimerDbContextFactory.cs
│   ├── Models/                          # Datenmodelle (Race, Participant, etc.)
│   ├── Services/
│   │   ├── IRaceRepository.cs           # Repository Interface
│   │   ├── LocalRaceRepository.cs       # Offline Implementierung
│   │   ├── SignalRSyncService.cs        # Server-Synchronisierung
│   │   └── SharedServiceCollectionExtensions.cs
│   └── Http/
│       └── RaceTimerApiClient.cs        # HTTP API Client (optional)
│
├── RaceTimerServer/                    # ASP.NET Core Web API + SignalR
│   ├── Program.cs                       # Verwendet Shared DbContext
│   ├── Controllers/                     # REST API Endpoints
│   ├── Services/
│   │   └── RaceRepository.cs           # Server-spezifische Repository Logik
│   ├── Hubs/
│   │   └── RaceHub.cs                  # SignalR Hub für Real-time Updates
│   └── Migrations/                     # EF Core Migrations
│
├── RaceTimerApp/                       # .NET MAUI Anwendung
│   ├── RaceTimerApp/
│   │   ├── MauiProgram.cs              # Registriert lokale Services
│   │   └── App.xaml.cs
│   ├── RaceTimerApp.Shared/            # Shared Blazor Components
│   │   ├── Services/
│   │   │   ├── RaceService.cs          # Nutzt IRaceRepository
│   │   │   ├── ParticipantService.cs   # Nutzt IRaceRepository
│   │   │   ├── AppConfigService.cs     # Konfiguration & Server-Verbindung
│   │   │   ├── TimingService.cs
│   │   │   ├── RankingService.cs
│   │   │   └── SettingsService.cs
│   │   ├── Models/
│   │   │   └── AppSettings.cs
│   │   └── Pages/
│   │       ├── Settings.razor          # Konfigurationsseite
│   │       └── ...
│   └── RaceTimerApp.Web/              # Blazor Web Komponenten
```

## Verwendung

### 1. **Offline-Modus (Standard)**

Die App arbeitet vollständig lokal:

```csharp
// MauiProgram.cs
builder.Services.AddMauiRaceServices();  // Registriert lokale SQLite-Services
```

**Datenfluss:**
```
UI (Razor Components) 
  ↓
RaceService / ParticipantService
  ↓
IRaceRepository (LocalRaceRepository)
  ↓
RaceTimerDbContext (SQLite)
  ↓
Lokale Datenbank (~AppData/RaceTimer/racetimer.db)
```

### 2. **Online-Modus (mit Server-Sync)**

Verbindung zu einem externen Server für Echtzeit-Updates:

```csharp
// In der App
var config = serviceProvider.GetRequiredService<AppConfigService>();
bool connected = await config.ConnectToServerAsync("https://server.example.com");

if (connected)
{
	await config.SubscribeToRaceUpdatesAsync(raceId);
	// Server-Updates werden via SignalR empfangen
}
```

**Datenfluss:**
```
Server-Updates (SignalR)
  ↓
SignalRSyncService
  ↓
LocalRaceRepository (Lokal aktualisiert)
  ↓
RaceTimerDbContext (SQLite)
  ↓
UI wird benachrichtigt
```

## Services

### IRaceRepository

Interface für Datenzugriff (lokal oder remote):

```csharp
public interface IRaceRepository
{
	// Rennen
	Task<IEnumerable<Race>> GetAllRacesAsync();
	Task<Race?> GetRaceAsync(Guid id);
	Task<Race> CreateRaceAsync(Race race);
	Task<bool> UpdateRaceAsync(Race race);
	Task<bool> DeleteRaceAsync(Guid id);

	// Teilnehmer
	Task<IEnumerable<Participant>> GetAllParticipantsAsync();
	// ... weitere Methoden
}
```

### LocalRaceRepository

Offline-Implementierung mit SQLite:

```csharp
services.AddMauiRaceServices();  // Registriert LocalRaceRepository
```

### SignalRSyncService

Verwaltet die Verbindung zum Server:

```csharp
var syncService = new SignalRSyncService("https://server.example.com");
await syncService.ConnectAsync();
await syncService.SubscribeToRaceAsync(raceId);

// Events für Änderungen abonnieren
syncService.OnRaceChanged += async (data) => { /* Update UI */ };
```

### AppConfigService

Zentrale Konfigurationsverwaltung:

```csharp
var config = serviceProvider.GetRequiredService<AppConfigService>();

// Server-Verbindung
await config.ConnectToServerAsync(serverUrl);
await config.DisconnectFromServerAsync();

// Status
bool isConnected = config.IsServerConnected;
AppSettings settings = config.Settings;
```

## Dependency Injection

### In MAUI (MauiProgram.cs):

```csharp
builder.Services.AddMauiRaceServices();  // Lokale Services + SQLite
builder.Services.AddSingleton<AppConfigService>();
builder.Services.AddScoped<RaceService>();
builder.Services.AddScoped<ParticipantService>();
// ... weitere Services
```

### Im Server (RaceTimerServer/Program.cs):

```csharp
// Verwendet die gleiche Shared DbContext
builder.Services.AddDbContext<RaceTimerDbContext>(options =>
{
	options.UseSqlServer(connectionString); // oder UseSqlite
});

builder.Services.AddScoped<IRaceRepository, LocalRaceRepository>();
builder.Services.AddScoped<RaceRepository>();  // Server-spezifische Logik
builder.Services.AddSignalR();
```

## Einstellungen (Settings)

### AppSettings.cs

```csharp
public class AppSettings
{
	public string Mode { get; set; } = "Offline";  // "Offline" oder "Online"
	public string? ServerUrl { get; set; }  // Server URL (nur im Online-Modus)
	public string DatabasePath { get; set; }  // Lokale DB (AppData)
	public int SyncIntervalSeconds { get; set; } = 30;
	public int StatusUpdateIntervalSeconds { get; set; } = 60;
	public bool AutoMigrateDatabase { get; set; } = true;
}
```

### Settings UI (Settings.razor)

Benutzer können:
- Zwischen Offline und Online-Modus umschalten
- Server-URL konfigurieren
- Sync-Intervalle anpassen

## Migration und Datenbank

### Lokale Migrations (für Shared DbContext)

```bash
# Von RaceTimerShared Verzeichnis aus:
dotnet ef migrations add InitialCreate --context RaceTimerDbContext --output-dir Data/Migrations

dotnet ef database update --context RaceTimerDbContext
```

### Server Migrations (nutzt Shared DbContext):

```bash
# Von RaceTimerServer Verzeichnis aus:
dotnet ef migrations add AddNewColumn --context RaceTimerDbContext --output-dir Migrations

dotnet ef database update
```

## Fehlerbehandlung und Netzwerk

### Automatischer Reconnect

SignalRSyncService nutzt `WithAutomaticReconnect()` für automatische Wiederverbindungen.

### Offline Fallback

Wenn Server nicht erreichbar ist:
1. App arbeitet weiterhin offline mit lokaler Datenbank
2. Änderungen werden lokal gespeichert
3. Bei Server-Wiederverbindung werden Änderungen synchronisiert (zu implementieren)

## Sicherheit

- **Lokal**: SQLite-Datei ist dateigeschützt
- **Server**: SignalR sollte über HTTPS mit Authentifizierung laufen
- **Konfiguration**: Server-URLs sollten gespeicherte Secrets verwenden

## Zukünftige Erweiterungen

1. **Konfliktauflösung**: Merge-Logik für lokale und Server-Änderungen
2. **Daten-Compression**: Optimierung für mobile Netzwerke
3. **Offline Sync Queue**: Warteschlange für Änderungen im Offline-Modus
4. **Benutzer-Authentifizierung**: User Login für Multi-User Szenarien
5. **Peer-to-Peer Sync**: Direkte Synchronisierung zwischen Geräten

## Troubleshooting

### "Datenbankdatei nicht gefunden"
- Stelle sicher, dass das AppData-Verzeichnis beschreibbar ist
- Prüfe Datenbankpfad in AppSettings

### "Server-Verbindung schlägt fehl"
- Verifikiere Server-URL (https statt http)
- Prüfe Netzwerkverbindung
- Server muss auf dem richtigen Port laufen

### "SignalR Connection Timeout"
- Erhöhe Connection Timeout in SignalRSyncService
- Prüfe Server-Protokolle auf Fehler
