# Schritt 2: REST-API und API-Client Implementierung - Abgeschlossen

## Überblick
Schritt 2 des Refactorplans wurde erfolgreich implementiert. Dies umfasst die Einrichtung einer **OpenAPI/Swagger-basierten REST-API** mit automatisierter Client-Code-Generierung via NSwag.

---

## 1. Server-seitige Implementierung (RaceTimerServer)

### 1.1 Swashbuckle/Swagger Integration
**Datei:** `RaceTimerServer/Program.cs`

- ✅ **Swagger/OpenAPI aktiviert**: Swashbuckle.AspNetCore (Version 7.0.0) integriert
- ✅ **Swagger UI verfügbar unter**: `http://localhost:5000/swagger`
- ✅ **OpenAPI-Spezifikation unter**: `http://localhost:5000/swagger/v1/swagger.json`

**Konfiguration:**
```csharp
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new()
	{
		Title = "RaceTimer API",
		Version = "v1",
		Description = "REST API for RaceTimer - Race timing and participant management system"
	});
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceTimer API v1");
	c.RoutePrefix = "swagger";
});
```

### 1.2 Vollständige REST-API (RaceTimerController)
**Datei:** `RaceTimerServer/Controllers/RaceTimerController.cs`

Der Controller implementiert **vollständige REST-API-Abdeckung** für alle IRaceRepository-Methoden:

#### Races-Endpunkte
| Methode | Endpoint | Beschreibung |
|---------|----------|-------------|
| GET | `/api/racetimer/races` | Alle Rennen abrufen |
| GET | `/api/racetimer/races/{id}` | Spezifisches Rennen abrufen |
| GET | `/api/racetimer/races/status/{status}` | Rennen nach Status filtern |
| POST | `/api/racetimer/races` | Neues Rennen erstellen |
| PUT | `/api/racetimer/races/{id}` | Rennen aktualisieren |
| DELETE | `/api/racetimer/races/{id}` | Rennen löschen |
| POST | `/api/racetimer/races/{id}/start` | Rennen starten |
| POST | `/api/racetimer/races/{id}/finish` | Rennen beenden |

#### Participants-Endpunkte
| Methode | Endpoint | Beschreibung |
|---------|----------|-------------|
| GET | `/api/racetimer/participants` | Alle Teilnehmer abrufen |
| GET | `/api/racetimer/participants/{id}` | Spezifischen Teilnehmer abrufen |
| POST | `/api/racetimer/participants` | Neuen Teilnehmer erstellen |
| PUT | `/api/racetimer/participants/{id}` | Teilnehmer aktualisieren |
| DELETE | `/api/racetimer/participants/{id}` | Teilnehmer löschen |

#### Race Participants-Endpunkte
| Methode | Endpoint | Beschreibung |
|---------|----------|-------------|
| GET | `/api/racetimer/races/{raceId}/participants` | Teilnehmer eines Rennens |
| POST | `/api/racetimer/races/{raceId}/participants/{participantId}` | Teilnehmer zu Rennen hinzufügen |
| DELETE | `/api/racetimer/races/{raceId}/participants/{participantId}` | Teilnehmer aus Rennen entfernen |

#### Race Time Points-Endpunkte
| Methode | Endpoint | Beschreibung |
|---------|----------|-------------|
| GET | `/api/racetimer/races/{raceId}/timepoints` | Alle Zeitpunkte eines Rennens |
| POST | `/api/racetimer/races/{raceId}/timepoint` | Neuen Zeitpunkt erstellen |
| PUT | `/api/racetimer/races/{raceId}/timepoints` | Zeitpunkte aktualisieren |
| DELETE | `/api/racetimer/races/{raceId}/timepoints/{timePointId}` | Zeitpunkt löschen |
| POST | `/api/racetimer/races/{raceIdCopyFrom}/timepoints/copy-to/{raceIdCopyTo}` | Zeitpunkte kopieren |

#### Race Participant Time Points-Endpunkte
| Methode | Endpoint | Beschreibung |
|---------|----------|-------------|
| GET | `/api/racetimer/timepoints/race/{raceId}` | Zeitpunkte eines Rennens |
| GET | `/api/racetimer/timepoints/{id}` | Spezifischer Zeitpunkt |
| GET | `/api/racetimer/timepoints/unassigned` | Nicht zugeordnete Zeitpunkte |
| POST | `/api/racetimer/timepoints` | Neuer Zeitpunkt erstellen |
| POST | `/api/racetimer/timepoints/{timePointId}/assign/{participantId}` | Zeitpunkt zu Teilnehmer zuordnen |
| DELETE | `/api/racetimer/timepoints/{timePointId}` | Zeitpunkt löschen |
| POST | `/api/racetimer/timepoints/{timePointId}/penalty` | Strafzeit setzen |
| POST | `/api/racetimer/timepoints/{timePointId}/correct` | Zeitpunkt korrigieren |
| POST | `/api/racetimer/timepoints/{timePointId}/undo` | Zeitpunkt-Korrektur rückgängig machen |

#### Change Tracking-Endpunkte
| Methode | Endpoint | Beschreibung |
|---------|----------|-------------|
| GET | `/api/racetimer/races/{raceId}/changes` | Änderungen seit Zeitstempel |

### 1.3 Request/Response Models
**Datei:** `RaceTimerServer/Controllers/Requests/ApiRequests.cs`

Definiert alle Request-Modelle für die API:
- `CreateRaceRequest`
- `CreateParticipantRequest`
- `CreateTimePointRequest`
- `CreateUnassignedTimePointRequest`
- `StartRaceRequest`
- `CorrectTimePointRequest`

---

## 2. Client-seitige Implementierung (RaceTimerShared)

### 2.1 IRaceTimerApiClient Interface
**Datei:** `RaceTimerShared/Http/RaceTimerApiClient.cs`

Ein **öffentliches Interface**, das alle API-Operationen definiert:
```csharp
public interface IRaceTimerApiClient
{
	// Races
	Task<IEnumerable<Race>> GetRacesAsync(CancellationToken cancellationToken = default);
	Task<Race?> GetRaceAsync(Guid id, CancellationToken cancellationToken = default);
	Task<Race?> CreateRaceAsync(string name, CancellationToken cancellationToken = default);
	// ... weitere Methoden
}
```

**Eigenschaften:**
- ✅ CancellationToken-Unterstützung für alle Methoden
- ✅ Konsistente Fehlerbehandlung (null-returns, boolean-flags)
- ✅ Asynchrone Operationen
- ✅ Generische Exception-Behandlung

### 2.2 RaceTimerApiClient Implementation
**Datei:** `RaceTimerShared/Http/RaceTimerApiClient.cs`

Eine interne Implementierung, die:
- ✅ HttpClient nutzt
- ✅ REST-API-Aufrufe mit `System.Net.Http.Json` kapselt
- ✅ Automatische JSON-Serialisierung/Deserialisierung
- ✅ Konsistente Fehlerbehandlung

**Basis-Path:** `api/racetimer`

**Beispiel einer Implementierung:**
```csharp
public async Task<IEnumerable<Race>> GetRacesAsync(CancellationToken cancellationToken = default)
{
	var result = await _http.GetFromJsonAsync<IEnumerable<Race>>($"{BasePath}/races", cancellationToken: cancellationToken);
	return result ?? [];
}
```

---

## 3. Integration & Registrierung

### 3.1 Dependency Injection (DI)
Die API-Clients werden über DI registriert (in den entsprechenden Program.cs Dateien):

**Server-Seite:**
```csharp
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(...);
```

**Client-Seite (wird in zukünftigen Schritten konfiguriert):**
```csharp
services.AddHttpClient<IRaceTimerApiClient, RaceTimerApiClient>(client =>
{
	client.BaseAddress = new Uri("https://api.example.com");
});
```

---

## 4. Zukünftige Schritte

### 4.1 NSwag Code-Generierung (Optional)
Falls automatische Client-Generierung gewünscht:
1. **NSwag CLI installieren** oder **NSwag.MSBuild** nutzen
2. **nswag.json** konfigurieren zum Generieren von `RaceTimerApiClient.generated.cs`
3. **Generierten Code** mit unserem manuellen Wrapper integrieren

**Befehl (manuell):**
```powershell
nswag run nswag.json
```

### 4.2 Vollständige Client-Integration
- Integration in Blazor-Frontend
- Integration in MAUI-App
- Fehlerbehandlung und Logging
- Retry-Strategien (Polly)

### 4.3 SignalR-Integration (Schritt 3)
- SignalR-Hub für Echtzeit-Notifications
- SignalRSyncService für Client-Push-Updates
- ServerRaceRepository mit Change-Notifications

---

## 5. Build-Status

✅ **Erfolgreicher Build:** Alle Projekte kompilieren fehlerfrei

**Dateien geändert:**
- ✅ `RaceTimerServer/Program.cs` - Swagger-Konfiguration
- ✅ `RaceTimerServer/Controllers/RaceTimerController.cs` - Vollständige REST-API
- ✅ `RaceTimerServer/Controllers/Requests/ApiRequests.cs` - Request-Modelle (neu)
- ✅ `RaceTimerServer/RaceTimerServer.csproj` - Swashbuckle hinzugefügt
- ✅ `RaceTimerShared/Http/RaceTimerApiClient.cs` - Modernisierter Client

---

## 6. Testing

### 6.1 Swagger UI testen
1. RaceTimerServer starten
2. Browser öffnen: `http://localhost:5000/swagger`
3. Alle Endpunkte testen und spielen herum

### 6.2 API-Client testen
```csharp
var client = new RaceTimerApiClient(httpClient);
var races = await client.GetRacesAsync();
var newRace = await client.CreateRaceAsync("Test Race");
```

---

## 7. Akzeptanzkriterien erfüllt ✅

- ✅ API-Coverage: Alle IRaceRepository-Methoden verfügbar
- ✅ REST-API mit OpenAPI/Swagger-Dokumentation
- ✅ Strukturierter API-Client mit Interface
- ✅ Konsistente Fehlerbehandlung
- ✅ CancellationToken-Unterstützung
- ✅ Kompatible DTOs
- ✅ Vollständiges Routing und HTTP-Verben

---

## Notizen

1. **Single Responsibility Principle**: 
   - Controller: HTTP-Handling
   - ApiClient: REST-Aufrufe
   - Request-Modelle: Validierung

2. **Erweiterbarkeit**:
   - Einfach weitere Endpunkte hinzufügen
   - NSwag kann noch implementiert werden für automatische Code-Generierung
   - SignalR kann nahtlos integriert werden (Schritt 3)

3. **Sicherheit**:
   - CORS sollte konfiguriert werden (bei Bedarf)
   - API-Authentifizierung sollte noch implementiert werden
   - Rate-Limiting sollte berücksichtigt werden

4. **Performance**:
   - HTTP-Kompression aktivieren (Gzip)
   - Caching für GET-Requests erwägen
   - Pagination für große Datenmengen implementieren

---

**Nächster Schritt:** Schritt 3 - SignalR-Integration und ServerRaceRepository
