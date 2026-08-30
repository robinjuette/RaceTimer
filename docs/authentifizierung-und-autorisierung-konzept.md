# Konzept: Authentifizierung und Autorisierung für RaceTimer

## 1. Zielsetzung

Der RaceTimerServer soll sowohl vollständig lokal und ohne Benutzeranmeldung als auch als zentraler Server für die RaceTimerApp und die Web-Version betrieben werden können.

Das System soll:

- Benutzer sicher authentifizieren, ohne dass RaceTimer Klartextpasswörter verarbeitet oder speichert.
- öffentliche und geschützte Funktionen konfigurierbar machen.
- die RaceTimerApp bei Serververbindungen per OAuth 2.0/OpenID Connect anmelden.
- die Web-Version per OpenID Connect und Cookie-Authentifizierung anbinden.
- Benutzer mit Rennteilnehmern verknüpfen können.
- den Zugriff auf Ergebnisse verknüpfter Teilnehmer ermöglichen.
- ohne E-Mail-Adresse funktionieren.
- per Docker Compose deploybar sein.
- auch ohne die Web-Version vollständig funktionsfähig bleiben.

Ein RaceTimerServer bildet ein einzelnes Betreiber- beziehungsweise Rennumfeld ab. Eine Mehrmandantenfähigkeit ist nicht Bestandteil dieses Konzepts.

## 2. Zielarchitektur

Die Web-Version ist immer eine eigenständige Anwendung. Sie ist niemals Bestandteil des RaceTimerServer-Prozesses.

Die Standardbereitstellung verwendet eine gemeinsame Docker-Compose-Datei mit getrennten Containern:

```text
Docker Compose
│
├── RaceTimerServer
│   ├── REST API
│   ├── SignalR-Hub
│   ├── ASP.NET Core Identity
│   ├── OpenIddict
│   └── Autorisierung
│
├── RaceTimerApp.Web
│   └── separate Blazor-Web-Anwendung
│
└── Datenbank
	├── RaceTimer-Daten
	└── Identity-Daten
```

Die Web-Anwendung kann bei Bedarf auch unabhängig von diesem Docker Compose betrieben werden. Der RaceTimerServer muss ohne Web-Version funktionieren.

Vorgesehene Technologien:

- ASP.NET Core Identity für Benutzer, Rollen, Claims und Passwortverwaltung
- OpenIddict als OAuth-2.0-/OpenID-Connect-Server
- Cookie-Authentifizierung innerhalb der Web-Version
- Bearer Access Tokens für REST API und RaceTimerApp
- Authorization Code Flow mit PKCE für die MAUI-App
- Entity Framework Core für Identity- und RaceTimer-Daten
- Docker Compose für die Standardbereitstellung

ASP.NET Core Identity übernimmt Passwort-Hashing, Benutzerverwaltung, Fehlversuchssperren, Rollen und Claims. OpenIddict stellt die standardisierten OAuth-/OIDC-Endpunkte und Tokens bereit.

## 3. Betriebsmodi

### 3.1 Lokaler Standalone-Betrieb

Im lokalen Betrieb der RaceTimerApp wird das lokale Repository verwendet. Es ist kein RaceTimerServer und keine Anmeldung erforderlich. Die lokalen Funktionen bleiben unabhängig von Identity und OpenIddict nutzbar.

### 3.2 Serverbetrieb der RaceTimerApp

Beim Verbinden mit einem RaceTimerServer startet die App bei Bedarf die Anmeldung im Systembrowser. Die Anmeldung erfolgt über OpenIddict mit Authorization Code Flow und PKCE.

Die App erhält ein kurzlebiges Access Token und ein Refresh Token. Access Tokens werden als Bearer Token an REST- und SignalR-Aufrufe angehängt. Tokens werden über den sicheren plattformspezifischen Gerätespeicher, beispielsweise .NET MAUI `SecureStorage`, gespeichert.

Die App implementiert keine eigene Passwortverwaltung und speichert keine Passwörter. Ist eine Funktion öffentlich freigegeben, kann sie ohne Anmeldung genutzt werden. Eine Anmeldung wird erst beim Zugriff auf geschützte Funktionen verlangt.

### 3.3 Betrieb ohne Web-Version

Der Server funktioniert unabhängig von der Web-Anwendung und stellt weiterhin REST API, SignalR, Identity, OpenIddict, öffentliche Daten sowie die Anmeldung der RaceTimerApp bereit.

### 3.4 Web-Version

Die separate Web-Version ist ein OIDC-Client des RaceTimerServers:

```text
RaceTimerApp.Web ── OIDC ──> RaceTimerServer
RaceTimerApp     ── OIDC ──> RaceTimerServer
```

Die Web-Version verwendet ein eigenes serverseitiges Authentifizierungs-Cookie. Dieses Cookie wird nicht mit dem RaceTimerServer geteilt. Dadurch bleibt die Web-Version unabhängig hostbar und kann sowohl im gemeinsamen Compose als auch in einer getrennten Umgebung betrieben werden.

Die Web-Version ruft die REST API und gegebenenfalls SignalR des RaceTimerServers auf. Die Autorisierung wird immer zusätzlich serverseitig geprüft.

## 4. Benutzerverwaltung

### 4.1 Benutzerkonto

Ein Benutzer besitzt mindestens:

- interne Benutzer-ID
- eindeutigen Benutzernamen
- Anzeigenamen
- Aktivierungsstatus
- Erstellungsdatum
- Zeitpunkt der letzten Anmeldung
- optionale E-Mail-Adresse

Der Benutzername ist die primäre lokale Anmeldekennung. Passwörter werden ausschließlich von ASP.NET Core Identity verarbeitet und als sichere Hashes gespeichert.

### 4.2 Optionale E-Mail-Adresse

Eine E-Mail-Adresse ist nicht erforderlich. Wenn sie angegeben wird, kann sie für Passwort-Zurücksetzung, Einladungen, Benachrichtigungen und eine optionale Bestätigung verwendet werden.

Benutzer ohne E-Mail-Adresse können ihr Passwort ändern, wenn sie ihr bisheriges Passwort kennen. Eine automatisierte Wiederherstellung per E-Mail ist für diese Benutzer nicht möglich. Ein Administrator muss in diesem Fall ein neues temporäres Passwort oder einen alternativen Zurücksetzungsprozess bereitstellen.

### 4.3 Registrierung

Die Benutzerregistrierung wird serverseitig konfiguriert:

- `AdminOnly`: Benutzer werden ausschließlich durch Administratoren angelegt.
- `Open`: Benutzer dürfen sich selbst registrieren.
- `Disabled`: Es können keine weiteren Benutzer angelegt werden.

Der Standard ist `AdminOnly`. Bei offener Registrierung erhalten neue Benutzer keine privilegierten Rollen, keine Teilnehmerverknüpfung und keine Verwaltungsberechtigungen.

## 5. Rollen und Berechtigungen

Vorgesehene Standardrollen sind:

### Administrator

Verwaltet Benutzer, Rollen, Teilnehmerverknüpfungen, Sicherheitseinstellungen, öffentliche Zugriffsregeln sowie sämtliche Renn- und Ergebnisdaten.

### RaceManager

Verwaltet Rennen, Teilnehmer, Rennzuordnungen und Rennstatus entsprechend den zugewiesenen Policies.

### Official

Kann beispielsweise Zeiten prüfen, Zeitpunkte korrigieren und Ergebnisse freigeben.

### Viewer

Kann geschützte allgemeine Lesefunktionen verwenden, für die eine Anmeldung, aber keine Verwaltungsrolle erforderlich ist.

### Participant

Kann Ergebnisse der zugeordneten Rennteilnehmer sehen. Die Rolle allein berechtigt nicht zum Zugriff auf beliebige Teilnehmer.

Die Autorisierung erfolgt policy-basiert, beispielsweise mit Policies wie:

- `CanManageUsers`
- `CanManageRaces`
- `CanManageParticipants`
- `CanCorrectTimePoints`
- `CanViewAllResults`
- `CanViewOwnResults`
- `CanViewPublicLiveEvents`

Jede API- und SignalR-Aktion wird serverseitig autorisiert. Die Benutzeroberfläche darf Aktionen ausblenden, ist aber keine Sicherheitsgrenze.

## 6. Konfigurierbarer öffentlicher Zugriff

Der Server erhält eine zentrale Konfiguration für öffentlich zugängliche Daten und Funktionen. Mögliche Einstellungen sind:

- öffentliche Rennübersicht
- öffentliche Liste laufender Rennen
- öffentliche Live-Informationen
- öffentliche Teilnehmerliste
- öffentliche Zwischenstände
- öffentliche Endergebnisse
- öffentliche Renndetails

Beispiel:

```text
PublicAccess.LiveEvents = true
PublicAccess.EventDiscovery = true
PublicAccess.FinishedResults = false
PublicAccess.ParticipantDetails = false
PublicAccess.Management = false
```

Öffentliche Endpunkte werden ausdrücklich freigegeben. Verwaltungsfunktionen sind standardmäßig geschützt.

### 6.1 Öffentliche laufende Events

Wenn laufende Events öffentlich freigegeben sind, können sie ohne vorherige Kenntnis einer Renn-ID gefunden und angesehen werden.

Dafür stellt der Server eine öffentliche Discovery-Funktion bereit, beispielsweise:

```text
GET /api/public/events/live
```

Die Antwort enthält nur aktuell laufende und zur Veröffentlichung freigegebene Rennen. Die Live-Daten eines Events können anschließend über einen öffentlichen REST- oder SignalR-Endpunkt abgerufen werden.

Öffentliche Daten dürfen keine privaten Kontaktdaten, internen Notizen, Benutzerinformationen oder nicht freigegebenen Teilnehmerdetails enthalten.

## 7. Verknüpfung von Benutzern und Teilnehmern

Zwischen Benutzer und Rennteilnehmer wird eine eigene Beziehung geführt:

```text
RaceTimerUserParticipant
------------------------
UserId
ParticipantId
CreatedAtUtc
CreatedByUserId
```

Ein Benutzer kann mehreren Teilnehmern zugeordnet werden. Ein Teilnehmer kann mehreren Benutzern zugeordnet werden, beispielsweise Eltern, Trainern oder Vereinsbetreuern.

Die Zuordnung darf ausschließlich durch Administratoren erstellt oder entfernt werden. Es gibt zunächst keine Selbstzuordnung und keine automatische Zuordnung anhand von Namen oder E-Mail-Adressen.

Änderungen an Zuordnungen werden protokolliert. Der Server speichert insbesondere, wer wann eine Zuordnung erstellt oder entfernt hat.

Beim Abruf eigener Ergebnisse prüft der Server:

1. den authentifizierten Benutzer,
2. die diesem Benutzer zugeordneten Teilnehmer,
3. die tatsächliche Teilnahme an einem Rennen,
4. die Sichtbarkeit des konkreten Ergebnisses.

Eine übergebene `ParticipantId` allein darf niemals Zugriff auf fremde Ergebnisse ermöglichen. Die Teilnehmerverknüpfung berechtigt ausschließlich zu den vorgesehenen persönlichen Ergebnisdaten, nicht zu Verwaltung, Zeitkorrekturen oder beliebigen Teilnehmerinformationen.

## 8. API- und SignalR-Sicherheit

Nicht authentifizierte Zugriffe auf geschützte Funktionen führen zu `401 Unauthorized`. Authentifizierte, aber nicht berechtigte Zugriffe führen zu `403 Forbidden`.

Für SignalR gelten dieselben Regeln wie für REST:

- öffentliche Live-Kanäle sind nur bei entsprechender Konfiguration anonym nutzbar,
- geschützte Verbindungen benötigen ein gültiges Token,
- Verbindungsaufbau und Methodenaufrufe werden autorisiert,
- private und administrative Daten werden nicht über öffentliche Gruppen verteilt.

Die RaceTimerApp verwendet das Bearer Token auch beim Aufbau einer SignalR-Verbindung.

## 9. OpenIddict und Clients

OpenIddict wird zunächst innerhalb des RaceTimerServer-Containers betrieben. Eine spätere Auslagerung bleibt möglich, ist für die erste Version aber nicht erforderlich.

### 9.1 RaceTimerApp

Die MAUI-App wird als nativer öffentlicher OIDC-Client registriert:

- Authorization Code Flow
- PKCE verpflichtend
- kein Client Secret in der App
- plattformspezifische Redirect URI
- kurzlebige Access Tokens
- Refresh Tokens für länger laufende Sitzungen

### 9.2 Web-Version

Die separate Web-Version wird als vertraulicher OIDC-Client registriert:

- Authorization Code Flow
- Client Secret ausschließlich serverseitig
- eigenes serverseitiges Login-Cookie
- keine langfristige Token-Speicherung in Browser-JavaScript

Die Web-Version greift für geschützte Daten über ihre serverseitige Logik auf die API zu oder verwendet dafür serverseitig verwaltete Tokens.

### 9.3 Scopes und Claims

Mögliche technische Scopes sind:

- `openid`
- `profile`
- `offline_access`
- `racetimer.read`
- `racetimer.manage`

Rollen können als Claims ausgegeben werden. Teilnehmerverknüpfungen werden jedoch nicht ausschließlich in langlebigen Token-Claims abgebildet. Die aktuelle Beziehung wird beim Zugriff auf geschützte Daten serverseitig geprüft.

## 10. Datenhaltung und Konfiguration

Standardmäßig liegen RaceTimer- und Identity-Daten in derselben konfigurierbaren Datenbank:

```text
RaceTimerDbContext
├── Renndaten
├── Teilnehmerdaten
├── Ergebnisdaten
├── AspNetUsers
├── AspNetRoles
├── AspNetUserClaims
├── AspNetUserRoles
└── RaceTimerUserParticipants
```

Eine getrennte Identity-Datenbank soll als Konfigurationsoption möglich bleiben. Für beide Varianten sind EF-Core-Migrationen, Backups und eine sichere Datenbankkonfiguration erforderlich.

OpenIddict-Signatur- und Verschlüsselungsschlüssel müssen persistent, sicher und containerübergreifend verfügbar sein. Geheimnisse dürfen nicht im Repository gespeichert werden.

## 11. Initiale Einrichtung

Beim ersten Start prüft der Server, ob ein Administrator existiert. Falls nicht, wird ein einmaliger Setup-Prozess aktiviert.

Der erste Administrator wird interaktiv oder über sicher bereitgestellte Setup-Konfiguration angelegt. Nach erfolgreicher Einrichtung wird der Bootstrap-Prozess deaktiviert.

Der Server darf nicht bei jedem Start ein bekanntes Standardpasswort erzeugen.

## 12. Sicherheitsgrundsätze

1. Keine Klartextpasswörter im RaceTimer-Code oder in der Datenbank.
2. Autorisierung ausschließlich serverseitig durchführen.
3. Teilnehmerzugriff niemals allein anhand einer Client-ID erlauben.
4. Öffentliche Funktionen nur über explizite Konfiguration freigeben.
5. Verwaltungsfunktionen standardmäßig schützen.
6. Refresh Tokens sicher und widerrufbar behandeln.
7. OpenIddict-Schlüssel persistent und geschützt speichern.
8. HTTPS für produktive und nicht-lokale Verbindungen voraussetzen.
9. Benutzer- und Berechtigungsänderungen auditieren.
10. Den lokalen Standalone-Betrieb unabhängig von Authentifizierung halten.

## 13. Empfohlene Implementierungsreihenfolge

1. Berechtigungsmatrix und öffentliche Funktionen festlegen.
2. Identity-Datenmodell und Datenbankkonfiguration ergänzen.
3. Rollen, Policies und initialen Administrator implementieren.
4. OpenIddict im RaceTimerServer konfigurieren.
5. OIDC-Login und Cookie-Session in der separaten Web-Version ergänzen.
6. Benutzer-Teilnehmer-Verknüpfung implementieren.
7. API-Endpunkte schrittweise mit Policies schützen.
8. Öffentliche Event-Discovery und öffentliche Live-Daten umsetzen.
9. Die MAUI-App mit OIDC, PKCE und sicherer Token-Speicherung anbinden.
10. SignalR-Authentifizierung und Autorisierung ergänzen.
11. Docker-Compose-Betrieb einschließlich Schlüssel- und Datenbankpersistenz dokumentieren.
12. Build-Validierung und manuelle Sicherheitsprüfung durchführen.
