# Schritt 5: ConfiguredConnectionRepository - Laufzeitumschaltung

## Übersicht
ConfiguredConnectionRepository ist ein intelligenter Proxy, der IRaceRepository und IRepositoryChangeNotifier implementiert und zur Laufzeit zwischen lokalen (CoreRaceRepository) und Server-basierten (ServerRaceRepository) Repositories umschalten kann - ohne dass die Consumer-Services davon wissen.

## Kernfunktionalität

### Thread-Safe Repository Switching
```csharp
await configuredRepo.SwitchRepositoryAsync(
	newRepository: serverRepository,
	newChangeNotifier: serverRepository,
	cancellationToken);
```

**Schaltprozess:**
1. Unsubscribe von alten Change-Notifications
2. Asynchrones Cleanup des alten Repositories (z.B. SignalR-Disconnect)
3. Wechsel zur neuen Repository-Instanz
4. Subscribe zu neuen Change-Notifications
5. Asynchrones Setup des neuen Repositories (z.B. SignalR-Connect)

### Lock-Basierte Synchronisation
- Interner `_switchLock` schützt gegen Race Conditions während Umschaltung
- IRaceRepository-Methoden werden thread-safe delegiert
- Disposal ist thread-safe

## Integration mit AppConfigService

AppConfigService nutzt ConfiguredConnectionRepository für nahtlose Mode-Umschaltung:

### Online-Modus starten
```csharp
// Benutzer gibt Server-URL ein und klickt "Verbinden"
bool success = await appConfig.ConnectToServerAsync("https://racetimer.example.com");
// → Erstellt ServerRaceRepository + SignalRSyncService
// → Ruft configuredRepo.SwitchRepositoryAsync() auf
// → Alle IRaceRepository-Aufrufe verwenden nun den Server
```

### Offline-Modus zurückkehren
```csharp
// Benutzer klickt "Trennen"
await appConfig.DisconnectFromServerAsync();
// → Erstellt CoreRaceRepository
// → Ruft configuredRepo.SwitchRepositoryAsync() auf
// → Alle IRaceRepository-Aufrufe verwenden wieder lokal
```

## DI-Registrierung

```csharp
// In SharedServiceCollectionExtensions.cs
builder.Services.AddLocalRaceServices(dbPath);  // Initial setup
builder.Services.AddConfiguredConnectionRepository();  // Runtime switcher

// Im MAUI App (MauiProgram.cs)
builder.Services.AddMauiRaceServicesWithSwitch(dbPath);  // Kombinierte Registrierung

// Consumers erhalten ConfiguredConnectionRepository als IRaceRepository
var repository = serviceProvider.GetRequiredService<IRaceRepository>();
// Es ist ein ConfiguredConnectionRepository!
```

## Lifecycle-Management

### SubscribeAsync / UnsubscribeAsync
- `SubscribeAsync()`: Delegiert an das aktuelle Repository
- Besonders wichtig für ServerRaceRepository (SignalR-Verbindung starten)
- CoreRaceRepository: meistens ein No-op

### DisposeAsync
- Unsubscribe von Change-Notifications
- DisposeAsync auf dem aktuellen Repository (falls implementiert)
- ServerRaceRepository wird aufgeräumt (SignalR-Disconnect)

## Akzeptanzkriterien

✅ **Thread-Safe Switching**: Mehrere Aufrufe während Umschaltung funktionieren korrekt
✅ **Event Forwarding**: Change-Notifications werden korrekt von alt zu neu weitergeleitet
✅ **Ressourcen-Management**: Keine Leaks beim Umschalten
✅ **Transparenz**: Consumer-Services (RaceManagementService, etc.) benötigen keine Änderungen
✅ **Graceful Fallback**: Falls DI nicht verfügbar, fallback auf einfache Logger

## Beispielablauf: Offline → Online

1. **Initial** (Offline-Modus)
   - ConfiguredConnectionRepository → CoreRaceRepository (SQLite)
   - RaceManagementService nutzt _repository (welches der Configured ist)
   - Events von DB-Änderungen werden weitergeleitet

2. **Benutzer: "Mit Server verbinden"**
   - Settings.razor → AppConfigService.ConnectToServerAsync(url)
   - AppConfigService erstellt: SignalRSyncService, ServerRaceRepository
   - Ruft auf: configuredRepo.SwitchRepositoryAsync(serverRepository)
   - ConfiguredConnectionRepository:
	 - Unsubscribed von CoreRaceRepository.RepositoryChanged
	 - Wechsel zu ServerRaceRepository
	 - Subscribe zu ServerRaceRepository.RepositoryChanged
	 - Ruft ServerRaceRepository.SubscribeAsync() (SignalR-Verbindung starten)

3. **Während Online-Modus**
   - Alle IRaceRepository-Aufrufe gehen an ServerRaceRepository
   - ServerRaceRepository delegiert an REST-API
   - SignalR-Events werden als RepositoryChanged weitergeleitet
   - RaceManagementService erhält alle Events automatisch

4. **Benutzer: "Trennen"**
   - Settings.razor → AppConfigService.DisconnectFromServerAsync()
   - AppConfigService erstellt: CoreRaceRepository
   - Ruft auf: configuredRepo.SwitchRepositoryAsync(coreRepository)
   - ConfiguredConnectionRepository:
	 - Unsubscribed von ServerRaceRepository.RepositoryChanged
	 - Async Cleanup: ServerRaceRepository.DisposeAsync() (SignalR-Disconnect)
	 - Wechsel zu CoreRaceRepository
	 - Subscribe zu CoreRaceRepository.RepositoryChanged

5. **Zurück im Offline-Modus**
   - Alle IRaceRepository-Aufrufe gehen an CoreRaceRepository
   - Daten kommen aus lokaler SQLite
   - DB-Change-Events werden weitergeleitet
   - Keine Netzwerkabhängigkeit

## Fehlerbehandlung

- **ObjectDisposedException**: Wenn auf disposed Repository zugegriffen wird
- **ArgumentNullException**: Wenn newRepository null ist
- **Logging**: Alle Switches werden geloggt (Info-Level)
- **Fehler beim SubscribeAsync**: Werden geworfen (sollen Benutzer wissen, dass Connection fehlgeschlagen)
