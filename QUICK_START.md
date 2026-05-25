# Quick Start Guide - Offline-First RaceTimer

## 1️⃣ App Starten (Offline-Modus)

Die MAUI-App funktioniert sofort offline ohne Konfiguration:

```csharp
// MauiProgram.cs - Automatisch konfiguriert
builder.Services.AddMauiRaceServices();  
// ↓ Registriert:
// - RaceTimerDbContext (SQLite)
// - LocalRaceRepository
// - AppConfigService
```

## 2️⃣ Daten Lokal Verwalten

```csharp
// In Ihrer Blazor Component
@inject RaceService RaceService
@inject ParticipantService ParticipantService

// Rennen abrufen (aus lokaler DB)
var races = await RaceService.GetAllRacesAsync();

// Neues Rennen erstellen
var newRace = await RaceService.CreateRaceAsync("Mein Rennen");

// Teilnehmer hinzufügen
var participant = await ParticipantService.AddParticipantToRaceAsync(
	raceId, 
	"Max Müller"
);
```

## 3️⃣ Mit Server Verbinden (Optional)

```csharp
@inject AppConfigService AppConfigService

// In Settings.razor oder wo Sie möchten:
private async Task ConnectToServer()
{
	string serverUrl = "https://raceserver.example.com";

	bool connected = await AppConfigService.ConnectToServerAsync(serverUrl);

	if (connected)
	{
		// App ist jetzt Online!
		// Änderungen können mit Server synchronisiert werden

		// Abonniere Updates für ein bestimmtes Rennen
		await AppConfigService.SubscribeToRaceUpdatesAsync(raceId);

		// Server-Updates werden via SignalR empfangen
	}
	else
	{
		// Server nicht erreichbar - App läuft weiterhin offline
	}
}
```

## 4️⃣ Server-Updates Empfangen

```csharp
// In einer Service-Klasse
public class RaceUpdateService
{
	private readonly SignalRSyncService? _sync;

	public async Task SetupSyncAsync()
	{
		_sync = AppConfigService.GetSignalRSync();

		if (_sync?.IsConnected ?? false)
		{
			// Abonniere Events für Änderungen
			_sync.OnRaceChanged += HandleRaceChanged;
			_sync.OnParticipantChanged += HandleParticipantChanged;
		}
	}

	private async Task HandleRaceChanged(object data)
	{
		// Race wurde auf Server geändert
		// Synchronisiere mit lokaler DB
		var race = (Race)data;
		await _repository.UpdateRaceAsync(race);

		// UI aktualisieren
		StateHasChanged();
	}
}
```

## 5️⃣ Datenbank-Pfad Anpassen

```csharp
// Standard (AppData):
// Windows: C:\Users\[Username]\AppData\Roaming\RaceTimer\racetimer.db
// macOS: /Users/[Username]/Library/Application Support/RaceTimer/racetimer.db

// Benutzerdefinierten Pfad verwenden:
var customDbPath = "/some/custom/path/racetimer.db";
builder.Services.AddLocalRaceServices(customDbPath);
```

## 6️⃣ Neue Migration Erstellen

```bash
# Von RaceTimerShared Verzeichnis aus:
cd RaceTimerShared

# Migration erstellen
dotnet ef migrations add AddNewColumn \
	--context RaceTimerDbContext \
	--output-dir Data/Migrations

# Datenbank aktualisieren
dotnet ef database update --context RaceTimerDbContext
```

## 7️⃣ Custom Repository Implementieren

Falls Sie eine andere Datenlösung (z.B. Cloud-Datenbank) verwenden möchten:

```csharp
// 1. Implementieren Sie IRaceRepository
public class CloudRaceRepository : IRaceRepository
{
	public async Task<IEnumerable<Race>> GetAllRacesAsync()
	{
		// Cloud-API aufrufen
		return await _cloudApi.GetRacesAsync();
	}

	// ... weitere Methoden implementieren
}

// 2. Registrieren Sie Ihre Implementierung
builder.Services.AddScoped<IRaceRepository, CloudRaceRepository>();

// Die App nutzt jetzt CloudRaceRepository statt LocalRaceRepository
```

## 8️⃣ Settings für Benutzer Speichern

```csharp
@inject AppConfigService AppConfigService

private void SaveUserPreferences()
{
	var settings = AppConfigService.Settings;
	settings.SyncIntervalSeconds = 15;
	settings.StatusUpdateIntervalSeconds = 45;

	AppConfigService.SaveSettings(settings);
	// Wird vom AppConfigService persistiert
}
```

## 🔍 Debugging

### Lokale DB ansehen

```csharp
// Datenbankpfad prüfen
var dbPath = Path.Combine(
	Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
	"RaceTimer",
	"racetimer.db"
);

// Mit SQL-Tool öffnen (z.B. DB Browser for SQLite)
// https://sqlitebrowser.org/
```

### SignalR Connection Status

```csharp
@inject AppConfigService AppConfigService

// In einer Component
<div>
	Status: @(AppConfigService.IsServerConnected ? "✓ Online" : "✗ Offline")
	Server: @(AppConfigService.Settings.ServerUrl ?? "Keine")
</div>
```

### Logs

```csharp
// In RaceService, ParticipantService, etc.
System.Diagnostics.Debug.WriteLine($"Info: {message}");

// In Visual Studio Output pane sehen
```

## ❌ Häufige Fehler

### "DbContext not registered"
```csharp
// ✗ Falsch:
services.AddScoped<RaceService>();

// ✓ Richtig:
services.AddMauiRaceServices();  // Registriert DbContext
services.AddScoped<RaceService>();
```

### "Server nicht erreichbar"
```csharp
// ✗ Falsch:
await AppConfigService.ConnectToServerAsync("localhost:5000");

// ✓ Richtig:
await AppConfigService.ConnectToServerAsync("https://localhost:7000");
```

### "Alte AppSettings-Eigenschaften"
```csharp
// ✗ Alte Properties (entfernt):
settings.ServerPort
settings.DatabaseType
settings.SqlServerConnectionString

// ✓ Neue Properties:
settings.Mode  // "Offline" oder "Online"
settings.ServerUrl
settings.DatabasePath
settings.SyncIntervalSeconds
```

## 📚 Weitere Ressourcen

- **ARCHITECTURE_OFFLINE_FIRST.md** - Detaillierte Architektur
- **REFACTORING_GUIDE.md** - Implementierungsleitfaden
- **REFACTORING_SUMMARY.md** - Zusammenfassung aller Änderungen

---

**Viel Erfolg mit der Offline-First RaceTimer-App! 🚀**
