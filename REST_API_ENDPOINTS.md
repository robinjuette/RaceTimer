# REST-API Endpunkt-Übersicht

## Base URL
```
http://localhost:5000/api/racetimer
```

## Races (8 Endpunkte)

### GET /races
**Beschreibung:** Alle Rennen abrufen  
**Response:** `200 OK` - `IEnumerable<Race>`

### GET /races/{id}
**Beschreibung:** Spezifisches Rennen abrufen  
**Parameter:** `id` (Guid)  
**Response:** `200 OK` - `Race` | `404 Not Found`

### GET /races/status/{status}
**Beschreibung:** Rennen nach Status filtern  
**Parameter:** `status` (string) - "Planning", "Running", "Finished", etc.  
**Response:** `200 OK` - `IEnumerable<Race>`

### POST /races
**Beschreibung:** Neues Rennen erstellen  
**Request Body:**
```json
{
  "name": "string"
}
```
**Response:** `201 Created` - `Race` | `409 Conflict`

### PUT /races/{id}
**Beschreibung:** Rennen aktualisieren  
**Parameter:** `id` (Guid)  
**Request Body:**
```json
{
  "id": "guid",
  "name": "string",
  "status": "string",
  // weitere Eigenschaften
}
```
**Response:** `204 No Content` | `400 Bad Request` | `404 Not Found`

### DELETE /races/{id}
**Beschreibung:** Rennen löschen  
**Parameter:** `id` (Guid)  
**Response:** `204 No Content` | `404 Not Found`

### POST /races/{id}/start
**Beschreibung:** Rennen starten  
**Parameter:** `id` (Guid)  
**Request Body:**
```json
{
  "participantIds": ["guid1", "guid2", ...],
  "timePointUtc": "2025-01-01T12:00:00Z"  // optional
}
```
**Response:** `200 OK` - `bool`

### POST /races/{id}/finish
**Beschreibung:** Rennen beenden  
**Parameter:** `id` (Guid)  
**Response:** `200 OK` - `bool` | `404 Not Found`

---

## Participants (5 Endpunkte)

### GET /participants
**Beschreibung:** Alle Teilnehmer abrufen  
**Response:** `200 OK` - `IEnumerable<Participant>`

### GET /participants/{id}
**Beschreibung:** Spezifischen Teilnehmer abrufen  
**Parameter:** `id` (Guid)  
**Response:** `200 OK` - `Participant` | `404 Not Found`

### POST /participants
**Beschreibung:** Neuen Teilnehmer erstellen  
**Request Body:**
```json
{
  "name": "string"
}
```
**Response:** `201 Created` - `Participant` | `409 Conflict`

### PUT /participants/{id}
**Beschreibung:** Teilnehmer aktualisieren  
**Parameter:** `id` (Guid)  
**Request Body:** `Participant` object  
**Response:** `204 No Content` | `400 Bad Request`

### DELETE /participants/{id}
**Beschreibung:** Teilnehmer löschen  
**Parameter:** `id` (Guid)  
**Response:** `204 No Content` | `404 Not Found`

---

## Race Participants (3 Endpunkte)

### GET /races/{raceId}/participants
**Beschreibung:** Alle Teilnehmer eines Rennens  
**Parameter:** `raceId` (Guid)  
**Response:** `200 OK` - `IEnumerable<RaceParticipant>`

### POST /races/{raceId}/participants/{participantId}
**Beschreibung:** Teilnehmer zu Rennen hinzufügen  
**Parameter:** `raceId` (Guid), `participantId` (Guid)  
**Response:** `201 Created` - `RaceParticipant` | `404 Not Found`

### DELETE /races/{raceId}/participants/{participantId}
**Beschreibung:** Teilnehmer aus Rennen entfernen  
**Parameter:** `raceId` (Guid), `participantId` (Guid)  
**Response:** `204 No Content` | `404 Not Found`

---

## Race Time Points (5 Endpunkte)

### GET /races/{raceId}/timepoints
**Beschreibung:** Alle Zeitpunkte eines Rennens  
**Parameter:** `raceId` (Guid)  
**Response:** `200 OK` - `IEnumerable<RaceTimePoint>`

### POST /races/{raceId}/timepoint
**Beschreibung:** Neuen Zeitpunkt erstellen  
**Parameter:** `raceId` (Guid)  
**Request Body:**
```json
{
  "name": "string"  // optional
}
```
**Response:** `201 Created` - `RaceTimePoint` | `404 Not Found`

### PUT /races/{raceId}/timepoints
**Beschreibung:** Zeitpunkte aktualisieren  
**Parameter:** `raceId` (Guid)  
**Request Body:** `List<RaceTimePoint>`  
**Response:** `204 No Content` | `404 Not Found`

### DELETE /races/{raceId}/timepoints/{timePointId}
**Beschreibung:** Zeitpunkt löschen  
**Parameter:** `raceId` (Guid), `timePointId` (Guid)  
**Response:** `204 No Content` | `404 Not Found`

### POST /races/{raceIdCopyFrom}/timepoints/copy-to/{raceIdCopyTo}
**Beschreibung:** Zeitpunkte von einem Rennen zu anderem kopieren  
**Parameter:** `raceIdCopyFrom` (Guid), `raceIdCopyTo` (Guid)  
**Response:** `200 OK` - `bool`

---

## Race Participant Time Points (11 Endpunkte)

### GET /timepoints/race/{raceId}
**Beschreibung:** Alle Teilnehmer-Zeitpunkte eines Rennens  
**Parameter:** `raceId` (Guid)  
**Response:** `200 OK` - `IEnumerable<RaceParticipantTimePoint>`

### GET /timepoints/{id}
**Beschreibung:** Spezifischen Teilnehmer-Zeitpunkt abrufen  
**Parameter:** `id` (Guid)  
**Response:** `200 OK` - `RaceParticipantTimePoint` | `404 Not Found`

### GET /timepoints/unassigned
**Beschreibung:** Alle nicht zugeordneten Zeitpunkte  
**Response:** `200 OK` - `IEnumerable<RaceParticipantTimePoint>`

### POST /timepoints
**Beschreibung:** Neuen nicht zugeordneten Zeitpunkt erstellen  
**Request Body:**
```json
{
  "timePointUtc": "2025-01-01T12:34:56Z"
}
```
**Response:** `201 Created` - `RaceParticipantTimePoint`

### POST /timepoints/{timePointId}/assign/{participantId}
**Beschreibung:** Zeitpunkt zu Teilnehmer zuordnen  
**Parameter:** `timePointId` (Guid), `participantId` (Guid)  
**Query:** `raceId` (Guid) - **erforderlich**  
**Response:** `200 OK` - `bool`

### DELETE /timepoints/{timePointId}
**Beschreibung:** Zeitpunkt löschen  
**Parameter:** `timePointId` (Guid)  
**Response:** `204 No Content` | `404 Not Found`

### POST /timepoints/{timePointId}/penalty
**Beschreibung:** Strafzeit für Zeitpunkt setzen  
**Parameter:** `timePointId` (Guid)  
**Request Body:** `TimeSpan` (z.B. `"00:01:30"` für 1 Minute 30 Sekunden)  
**Response:** `200 OK` - `bool`

### POST /timepoints/{timePointId}/correct
**Beschreibung:** Zeitpunkt korrigieren  
**Parameter:** `timePointId` (Guid)  
**Request Body:**
```json
{
  "correctedTimeUtc": "2025-01-01T12:34:56Z",
  "reason": "string",
  "correctedByUser": "string"  // optional
}
```
**Response:** `200 OK` - `bool`

### POST /timepoints/{timePointId}/undo
**Beschreibung:** Zeitpunkt-Korrektur rückgängig machen  
**Parameter:** `timePointId` (Guid)  
**Response:** `200 OK` - `bool` | `404 Not Found`

---

## Change Tracking (1 Endpunkt)

### GET /races/{raceId}/changes
**Beschreibung:** Änderungen seit Zeitstempel abrufen (Differential Query)  
**Parameter:** 
- `raceId` (Guid) - path parameter
- `sinceUtc` (DateTime) - query parameter  
**Response:** `200 OK` - `object` (dynamische Änderungsdaten)

---

## HTTP Status Codes

| Code | Bedeutung |
|------|-----------|
| `200 OK` | Erfolgreiche GET/POST/PUT Anfrage |
| `201 Created` | Ressource erfolgreich erstellt |
| `204 No Content` | Erfolgreiche DELETE/PUT ohne Response-Body |
| `400 Bad Request` | Ungültige Anfrage (z.B. ID-Mismatch) |
| `404 Not Found` | Ressource nicht gefunden |
| `409 Conflict` | Konflikt beim Erstellen (z.B. doppeltes Rennen) |
| `500 Internal Server Error` | Server-Fehler |

---

## Content-Type

Alle Anfragen und Responses verwenden `application/json`:
```
Content-Type: application/json
```

---

## Beispiel-Anfragen mit cURL

### Alle Rennen abrufen
```bash
curl -X GET http://localhost:5000/api/racetimer/races
```

### Neues Rennen erstellen
```bash
curl -X POST http://localhost:5000/api/racetimer/races \
  -H "Content-Type: application/json" \
  -d '{"name":"Marathon 2025"}'
```

### Rennen aktualisieren
```bash
curl -X PUT http://localhost:5000/api/racetimer/races/550e8400-e29b-41d4-a716-446655440000 \
  -H "Content-Type: application/json" \
  -d '{"id":"550e8400-e29b-41d4-a716-446655440000","name":"Updated Race"}'
```

### Rennen löschen
```bash
curl -X DELETE http://localhost:5000/api/racetimer/races/550e8400-e29b-41d4-a716-446655440000
```

### Rennen mit Status "Running" abrufen
```bash
curl -X GET "http://localhost:5000/api/racetimer/races/status/Running"
```

---

## Dokumentation

Swagger/OpenAPI-Dokumentation verfügbar unter:
```
http://localhost:5000/swagger
```

OpenAPI-Spezifikation (JSON):
```
http://localhost:5000/swagger/v1/swagger.json
```
