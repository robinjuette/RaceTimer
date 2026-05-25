# RaceTimer Refactoring - Implementierungsleitfaden

## Was wurde getan?

Das RaceTimer-Projekt wurde refaktoriert, um eine **Offline-First Architektur** mit optionalem Server-Sync zu unterstützen.

### Wichtigste Änderungen:

#### 1. **RaceTimerShared (Shared Library)**
- ✅ Neue `Data/RaceTimerDbContext.cs` - Shared Entity Framework Core DbContext
- ✅ Neue `Services/IRaceRepository.cs` - Repository-Interface für Datenzugriff
- ✅ Neue `Services/LocalRaceRepository.cs` - Offline-Implementierung mit SQLite
- ✅ Neue `Services/SignalRSyncService.cs` - SignalR Client für optionale Server-Verbindung
- ✅ Neue `Services/SharedServiceCollectionExtensions.cs` - DI Registration

#### 2. **RaceTimerServer**
- ✅ Aktualisiert `Program.cs` - Nutzt Shared `RaceTimerDbContext`
- ✅ Aktualisiert `Services/RaceRepository.cs` - Importiert aus RaceTimer.Shared.Data
- ✅ Alte `Data/RaceTimerDbContext.cs` gelöscht (wurde nach Shared verschoben)
- ✅ Migrationen aktualisiert - Neue Using-Direktiven

#### 3. **RaceTimerApp (MAUI)**
- ✅ Aktualisiert `MauiProgram.cs` - Registriert lokale Services statt HTTP-Client
- ✅ Aktualisiert `Services/RaceService.cs` - Nutzt `IRaceRepository` statt `RaceTimerApiClient`
- ✅ Aktualisiert `Services/ParticipantService.cs` - Nutzt `IRaceRepository`
- ✅ Neue `Services/AppConfigService.cs` - Verwaltet App-Konfiguration und Server-Verbindung
- ✅ Aktualisiert `Models/AppSettings.cs` - Neue Offline-First Einstellungen
- ✅ Aktualisiert `Pages/Settings.razor` - Neue UI für Offline/Online Modus

## Nächste Schritte

### 1. **Testen Sie die Offline-Funktionalität**

```bash
# Starten Sie die MAUI App im Offline-Modus
# Sie sollten Rennen lokal erstellen und bearbeiten können
```

### 2. **Konfigurieren Sie optionale Server-Verbindung**

Bearbeiten Sie `Settings.razor` in der App:
1. Wechseln Sie auf "Online (mit Server)"
2. Geben Sie die Server-URL ein (z.B. `https://localhost:7000`)
3. Klicken Sie "Verbinden"

### 3. **Starten Sie den Server (optional)**

```bash
cd RaceTimerServer
dotnet run
# Server läuft auf https://localhost:7000 (oder konfigurierter Port)
```

### 4. **Implementieren Sie Server-Sync (optional)**

Die SignalRSyncService ist bereit, aber die Synchronisierungslogik muss implementiert werden:

```csharp
// In RaceService.cs oder wo Sie Daten ändern:
if (_signalRSync?.IsConnected ?? false)
{
	// Benachrichtige Server über Änderung
	// Server aktualisiert andere Clients via SignalR
}
```

## Verwendung

### Offline-Modus (Standard)
```csharp
// Die App nutzt lokale Datenbank automatisch
var raceService = serviceProvider.GetRequiredService<RaceService>();
var races = await raceService.GetAllRacesAsync();  // Lokal aus SQLite
```

### Online-Modus (mit Server)
```csharp
var appConfig = serviceProvider.GetRequiredService<AppConfigService>();

// Mit Server verbinden
bool connected = await appConfig.ConnectToServerAsync("https://server.example.com");

if (connected)
{
	// App ist jetzt online
	// Änderungen werden mit Server synchronisiert
	await appConfig.SubscribeToRaceUpdatesAsync(raceId);
}
```

## Projektdependencies

### RaceTimerShared (.csproj)
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.8" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.8" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="10.0.8" />
```

### RaceTimerServer (nutzt Shared)
```xml
<ProjectReference Include="..\RaceTimerShared\RaceTimerShared.csproj" />
```

### RaceTimerApp (nutzt Shared)
```xml
<ProjectReference Include="..\RaceTimerShared\RaceTimerShared.csproj" />
```

## Bekannte Einschränkungen

- [ ] Zwei-Wege-Sync: Wenn Benutzer offline arbeitet und dann verbindet, werden Konflikte nicht automatisch gelöst
- [ ] Benutzer-Authentifizierung: Wird für Multi-User Szenarien benötigt
- [ ] Daten-Compression: Für mobile Netzwerke optimieren
- [ ] Persistierung von App-Einstellungen: Momentan in-memory

## Mögliche Verbesserungen

1. **Konfliktauflösung** - Merge-Logik für lokale vs. Server-Änderungen
2. **Sync-Queue** - Warteschlange für Änderungen im Offline-Modus
3. **Daten-Encryption** - SQLite-Datenbank verschlüsseln
4. **Change Tracking** - Effizientere Synchronisierung
5. **Unit Tests** - Tests für LocalRaceRepository und Services

## Datenbankmigrationen

### Neue Migration erstellen

```bash
# Von RaceTimerShared Verzeichnis:
dotnet ef migrations add MyMigrationName --context RaceTimerDbContext --output-dir Data/Migrations

# Von RaceTimerServer Verzeichnis (nutzt gleiche DbContext):
dotnet ef migrations add MyMigrationName --context RaceTimerDbContext --output-dir Migrations
```

### Migration anwenden

```bash
# Automatisch beim Startup (wenn aktiviert)
# Oder manuell:
dotnet ef database update --context RaceTimerDbContext
```

## Support

Weitere Informationen finden Sie in:
- `ARCHITECTURE_OFFLINE_FIRST.md` - Detaillierte Architektur-Dokumentation
- `RaceTimerShared/Services/IRaceRepository.cs` - API-Dokumentation
- `RaceTimerApp/RaceTimerApp.Shared/Services/AppConfigService.cs` - Konfiguration

## Kontakt & Fragen

Bei Fragen zur neuen Architektur:
1. Prüfen Sie die Dokumentation
2. Suchen Sie nach bestehenden Issues im Repo
3. Öffnen Sie ein neues Issue mit Details
