# Implementierungsplan: Authentifizierung und Autorisierung

## Dokumentstatus

| Eigenschaft | Wert |
|---|---|
| Status | In Arbeit |
| Version | 1.1 |
| Erstellt am | 2026-06-03 |
| Grundlage | `docs/authentifizierung-und-autorisierung-konzept.md` |
| Geltungsbereich | RaceTimerServer und RaceTimerApp.Web |
| Nachgelagerter Geltungsbereich | MAUI-OIDC-Anbindung |
| Gesamtfortschritt | 65 % |
| Verantwortlich | KI-gestützte Implementierung mit manueller Abnahme |

Dieses Dokument ist das operative Arbeitsdokument für die Umsetzung des Konzepts. Der Status jeder Phase und Aufgabe wird nach jeder abgeschlossenen Arbeit aktualisiert.

## Statuslegende

- `[ ] Nicht begonnen`
- `[~] In Arbeit`
- `[x] Erledigt`
- `[!] Blockiert oder Entscheidung erforderlich`
- `[-] Entfällt`

Für Phasen gilt zusätzlich:

- **Nicht begonnen**
- **In Arbeit**
- **Abgeschlossen**
- **Blockiert**
- **Abgenommen**

## Festgelegte Architekturentscheidungen

| Thema | Festlegung |
|---|---|
| Server-Datenbank | PostgreSQL als Standard für Docker Compose |
| Identity-Daten | Gemeinsame PostgreSQL-Datenbank mit den RaceTimer-Daten |
| Identity-Kontext | Serverseitiger Identity-Kontext; der gemeinsam verwendete lokale MAUI-Kontext bleibt frei von Identity-Abhängigkeiten |
| Administrator-Bootstrap | Interaktiver Setup-Endpunkt bzw. Setup-Seite mit Einmal-Token |
| Öffentliche Funktionen | Standardmäßig vollständig deaktiviert; explizite Aktivierung durch Administratoren |
| MAUI-Redirect | Systembrowser mit App- oder Universal-Link-Redirects |
| Erste Implementierungsphase | RaceTimerServer und RaceTimerApp.Web |
| MAUI-Implementierung | Eigene nachgelagerte Phase |
| Standalone-Modus | Bleibt unabhängig von Identity und OpenIddict funktionsfähig |
| Mandantenfähigkeit | Nicht Bestandteil dieser Umsetzung |
| Server-Standalone-Kompatibilität | Der bisherige SQLite-Modus bleibt ohne Identity verfügbar; Authentifizierung wird im PostgreSQL-Modus aktiviert |
| OIDC-Webclient | Serverseitiges ASP.NET-Core-Cookie mit Authorization-Code-Flow; Token bleiben außerhalb des Browser-JavaScript |

## Zielarchitektur

```text
RaceTimerServer
├── REST API
├── geschützter SignalR-Hub
├── optionaler öffentlicher Live-Hub
├── ASP.NET Core Identity
├── OpenIddict Authorization Server
├── RaceTimer-Datenbankkontext
├── Identity-/OpenIddict-Datenbankkontext
├── Benutzer-/Teilnehmerverknüpfungen
├── Auditierung
└── Setup-/Bootstrap-Prozess

RaceTimerApp.Web
├── OIDC-Client
├── eigenes serverseitiges Authentifizierungs-Cookie
├── serverseitige API-Aufrufe mit Bearer Token
└── geschützte Blazor-Komponenten und Seiten

MAUI-App (nachgelagert)
├── OIDC mit Authorization Code Flow und PKCE
├── SecureStorage
├── REST-/SignalR-Bearer Token
└── unveränderter lokaler Offline-Betrieb
```

## Fortschrittsübersicht

| Nr. | Phase | Inhalt | Status | Fortschritt | Abnahme |
|---:|---|---|---|---:|---|
| 1 | Projekt- und Paketgrundlage | Pakete, Konfiguration, Optionsklassen | Abgeschlossen | 100 % | Offen |
| 2 | Datenmodell und Datenbank | PostgreSQL, Identity, OpenIddict, Migrationen | Abgeschlossen | 100 % | Offen |
| 3 | Identity und Benutzerverwaltung | Benutzer, Rollen, Policies | Abgeschlossen | 100 % | Offen |
| 4 | Administrator-Bootstrap | Einmaliger Setup-Prozess | In Arbeit | 75 % | Offen |
| 5 | OpenIddict Authorization Server | OIDC-Endpunkte, Scopes, Tokens, Schlüssel | In Arbeit | 85 % | Offen |
| 6 | Web-OIDC-Client | Cookie, Login, Logout, API-Token | In Arbeit | 25 % | Offen |
| 7 | API-Schutz und öffentliche API | Policies, DTOs, Public-Access-Konfiguration | In Arbeit | 35 % | Offen |
| 8 | Benutzer-Teilnehmerverknüpfung | Administration, eigene Ergebnisse, Audit | In Arbeit | 55 % | Offen |
| 9 | SignalR-Sicherheit | Hub-Authentifizierung, Gruppen, Live-Hub | Nicht begonnen | 0 % | Offen |
| 10 | Docker Compose und Betrieb | PostgreSQL, Volumes, Secrets, HTTPS | Nicht begonnen | 0 % | Offen |
| 11 | Tests und Sicherheitsprüfung | Automatisierte und manuelle Prüfung | Nicht begonnen | 0 % | Offen |
| 12 | MAUI-OIDC | Separate Folgephase | Zurückgestellt | 0 % | Nicht Bestandteil Phase 1 |

---

# Umsetzungsphasen

## Phase 1: Projekt- und Paketgrundlage

**Ziel:** Die technische Grundlage für PostgreSQL, Identity und OpenIddict schaffen, ohne den lokalen MAUI-Betrieb zu beeinträchtigen.

### Aufgaben

- [x] Benötigte ASP.NET-Core-Identity-Pakete für .NET 10 ergänzen.
- [x] Npgsql- und EF-Core-PostgreSQL-Pakete ergänzen.
- [x] OpenIddict-Core-, EF-Core- und ASP.NET-Core-Pakete ergänzen.
- [x] Paketversionen zentral dokumentieren und auf Kompatibilität prüfen.
- [x] Konfigurationsklassen für Datenbank, Authentifizierung, Tokens und öffentliche Zugriffe anlegen.
- [x] Entwicklungs- und Produktionskonfiguration trennen.
- [x] Secrets aus versionierten Konfigurationsdateien fernhalten.
- [x] Projektabhängigkeiten prüfen und unnötige Identity-Abhängigkeiten aus Shared-/MAUI-Projekten vermeiden.

### Erwartete Dateien

- `RaceTimerServer/RaceTimerServer.csproj`
- `RaceTimerServer/appsettings.json`
- `RaceTimerServer/appsettings.Development.json`
- neue Dateien unter `RaceTimerServer/Configuration`

### Abschlusskriterien

- Der Server kompiliert mit den neuen Paketen.
- Der lokale MAUI-Build bleibt unverändert funktionsfähig.
- Fehlende Pflichtkonfiguration wird mit verständlicher Fehlermeldung erkannt.

---

## Phase 2: Datenmodell und Datenbank

**Ziel:** Identity-, OpenIddict-, Verknüpfungs- und Auditdaten in einer gemeinsamen PostgreSQL-Datenbank bereitstellen.

### Aufgaben

- [x] `RaceTimerUser` auf Basis von `IdentityUser` definieren.
- [x] Zusätzliche Benutzereigenschaften ergänzen: Anzeigename, Aktivierungsstatus, Erstellungsdatum und letzte Anmeldung.
- [x] Serverseitigen Identity-/OpenIddict-DbContext anlegen.
- [ ] Bestehenden `RaceTimerDbContext` für den lokalen SQLite-Betrieb unverändert kompatibel halten.
- [x] PostgreSQL-Registrierung für den Server implementieren.
- [x] `RaceTimerUserParticipant` modellieren.
- [ ] Auditmodell für Anlage und Entfernung von Teilnehmerverknüpfungen modellieren.
- [x] Eindeutigkeit und Indizes für Benutzer, Rollen und Verknüpfungen definieren.
- [ ] Serverseitige Validierung von `ParticipantId` zwischen Auth- und RaceTimer-Kontext vorsehen.
- [~] EF-Core-Migrationen erzeugen.
- [ ] Migrationen auf einer leeren PostgreSQL-Datenbank prüfen.
- [ ] Migrationen auf einer bestehenden RaceTimer-Datenbank prüfen.

### Abschlusskriterien

- Identity- und OpenIddict-Tabellen werden korrekt erzeugt.
- Bestehende RaceTimer-Daten bleiben erreichbar.
- Lokale SQLite-Migrationen werden nicht verändert oder beschädigt.
- Die Datenbank kann aus einer leeren Umgebung reproduzierbar aufgebaut werden.

---

## Phase 3: Identity, Rollen und Policies

**Ziel:** Benutzerverwaltung und zentrale Autorisierungsregeln implementieren.

### Aufgaben

- [x] Identity mit sicherem Passwort-Hashing konfigurieren.
- [x] Optionale E-Mail-Adresse ermöglichen.
- [x] Fehlversuchssperre und Aktivierungsstatus konfigurieren.
- [x] Benutzernamen eindeutig behandeln.
- [x] Standardrollen idempotent anlegen: `Administrator`, `RaceManager`, `Official`, `Viewer`, `Participant`.
- [x] Policies zentral definieren: `CanManageUsers`, `CanManageRaces`, `CanManageParticipants`, `CanCorrectTimePoints`, `CanViewAllResults`, `CanViewOwnResults`, `CanViewPublicLiveEvents`.
- [x] Rollen- und Policy-Zuordnung dokumentieren.
- [ ] Authorization Handler für eigene Ergebnisse vorbereiten.
- [ ] Verhalten für `401 Unauthorized` und `403 Forbidden` prüfen.

### Abschlusskriterien

- Rollen werden ohne Duplikate angelegt.
- Ein deaktivierter Benutzer kann sich nicht anmelden.
- Policies können unabhängig von Controllern getestet werden.
- Rollen werden nicht als alleinige Prüfung für Teilnehmerdaten verwendet.

---

## Phase 4: Einmaliger Administrator-Bootstrap

**Ziel:** Einen sicheren initialen Administrator ohne bekanntes Standardpasswort einrichten.

### Aufgaben

- [x] Service zur Prüfung des Setup-Zustands implementieren.
- [x] Prüfen, ob ein Benutzer mit Administratorrolle existiert.
- [x] Einmal-Token aus sicherer Konfiguration oder Secret Store lesen.
- [x] Interaktiven Setup-Endpunkt implementieren.
- [ ] Setup-Seite in der Web-Anwendung bereitstellen.
- [ ] Benutzername, Anzeigename und Passwort validieren.
- [ ] Administratorbenutzer und Rolle transaktional anlegen.
- [ ] Token nach erfolgreicher Einrichtung invalidieren.
- [ ] Setup nach erfolgreicher Einrichtung dauerhaft deaktivieren.
- [ ] Parallelzugriffe auf den Bootstrap verhindern.
- [ ] Setup-Aktionen auditieren.
- [ ] Kein Standardpasswort und keine wiederholte automatische Anlage zulassen.

### Abschlusskriterien

- Setup funktioniert nur, wenn noch kein Administrator existiert.
- Ein ungültiger oder bereits verwendeter Token wird abgewiesen.
- Ein Neustart erzeugt keinen weiteren Administrator.
- Passwörter und Setup-Token erscheinen nicht in Logs.

---

## Phase 5: OpenIddict Authorization Server

**Ziel:** Den RaceTimerServer als OAuth-2.0-/OpenID-Connect-Server bereitstellen.

### Aufgaben

- [x] OpenIddict mit EF Core und dem serverseitigen DbContext verbinden.
- [x] Authorization-Code-Flow aktivieren.
- [x] PKCE für öffentliche Clients verpflichtend konfigurieren.
- [x] Refresh Tokens aktivieren und widerrufbar machen.
- [ ] Scopes konfigurieren:
  - [ ] `openid`
  - [ ] `profile`
  - [ ] `offline_access`
  - [ ] `racetimer.read`
  - [ ] `racetimer.manage`
- [ ] Rollen-, Benutzer- und Profil-Claims konfigurieren.
- [ ] Kurzlebige Access Tokens konfigurieren.
- [ ] Refresh-Token-Laufzeit und Rotation festlegen.
- [ ] Authorization-, Token-, Logout- und Discovery-Endpunkte konfigurieren.
- [ ] Native MAUI-Clientregistrierung für die Folgephase vorbereiten.
- [ ] Vertrauliche Web-Clientregistrierung vorbereiten.
- [~] Persistente Signatur- und Verschlüsselungsschlüssel konfigurieren.
- [ ] HTTPS-Anforderungen für Nicht-Development prüfen.
- [ ] Issuer-URL und externe Reverse-Proxy-Szenarien dokumentieren.

### Abschlusskriterien

- OIDC Discovery ist erreichbar.
- Der Authorization-Code-Flow liefert gültige Tokens.
- Access Tokens enthalten nur erforderliche Claims.
- Refresh Tokens können erneuert und widerrufen werden.
- Schlüssel bleiben über Containerneustarts erhalten.

---

## Phase 6: OIDC-Client der Web-Version

**Ziel:** Die separate Blazor-Web-Anwendung über OIDC anmelden und ein eigenes Cookie verwenden.

### Aufgaben

- [ ] OIDC- und Cookie-Authentifizierung in `RaceTimerApp.Web` konfigurieren.
- [ ] Web-Client-ID und Client-Secret ausschließlich serverseitig konfigurieren.
- [ ] Login-Challenge implementieren.
- [ ] Logout einschließlich Server-Logout implementieren.
- [ ] Fehlerbehandlung für fehlgeschlagene Anmeldung ergänzen.
- [ ] Login- und Logout-UI bereitstellen.
- [ ] Serverseitige Tokenverwaltung für API-Aufrufe implementieren.
- [ ] Bearer Token automatisch an geschützte API-Aufrufe anhängen.
- [ ] Refresh-Token-Verwendung serverseitig kapseln.
- [ ] Keine langfristige Token-Speicherung in Browser-JavaScript einführen.
- [ ] Blazor-Seiten und Komponenten mit Autorisierung versehen.
- [ ] Rollen und Policies in der Web-UI als Ergänzung zur API-Autorisierung verwenden.
- [ ] Fehlende Berechtigung als `403` darstellen.

### Erwartete Dateien

- `RaceTimerApp/RaceTimerApp.Web/Program.cs`
- `RaceTimerApp/RaceTimerApp.Web/appsettings.json`
- `RaceTimerApp/RaceTimerApp.Web/appsettings.Development.json`
- neue Authentifizierungs- und Token-Serviceklassen
- neue Login-/Logout-Komponenten
- Anpassungen am serverseitigen API-Client

### Abschlusskriterien

- Eine Web-Sitzung verwendet nur das Cookie der Web-Anwendung.
- Das RaceTimerServer-Cookie wird nicht in der Web-Anwendung verwendet.
- Geschützte Seiten erzwingen eine Anmeldung.
- Serverseitige API-Aufrufe funktionieren mit Access Token.
- Abgelaufene Tokens führen zu einer kontrollierten Erneuerung oder erneuten Anmeldung.

---

## Phase 7: API-Schutz und öffentliche Endpunkte

**Ziel:** Alle vorhandenen Funktionen standardmäßig schützen und öffentliche Funktionen ausdrücklich freigeben.

### Aufgaben

- [x] Bestehende Controller-Endpunkte vollständig klassifizieren.
- [x] Alle Endpunkte zunächst mit Authentifizierung schützen.
- [x] Verwaltungsfunktionen mit passenden Policies versehen.
- [ ] Lesezugriffe nach Rolle, eigener Zuordnung und öffentlicher Freigabe unterscheiden.
- [ ] `PublicAccess`-Optionsmodell implementieren.
- [x] Öffentliche Endpunkte unter `/api/public` getrennt bereitstellen.
- [~] Public-Access-Service implementieren.
- [x] Öffentliche DTOs statt vollständiger interner Entities verwenden.
- [x] Event-Discovery für laufende Rennen implementieren.
- [x] Öffentliche Live-Daten implementieren.
- [x] Öffentliche Ergebnisse, Renndetails und Teilnehmerdetails einzeln konfigurierbar machen.
- [ ] Private Kontaktdaten, interne Notizen und Benutzerdaten aus öffentlichen Antworten entfernen.
- [ ] Standardwerte aller öffentlichen Funktionen auf `false` setzen.

### Vorgesehene Endpunkte

```text
GET /api/public/events/live
GET /api/public/events/{id}/live
GET /api/public/events/{id}/results
```

Die endgültige Endpoint-Matrix wird während der Implementierung als Teil des Entscheidungsprotokolls festgehalten.

### Abschlusskriterien

- Anonyme Aufrufe geschützter Endpunkte liefern `401`.
- Authentifizierte, aber unberechtigte Aufrufe liefern `403`.
- Öffentliche Funktionen sind standardmäßig nicht erreichbar.
- Aktivierte öffentliche Endpunkte liefern keine privaten Daten.
- Verwaltungsfunktionen bleiben unabhängig von Public-Access-Einstellungen geschützt.

---

## Phase 8: Benutzer-Teilnehmerverknüpfung und eigene Ergebnisse

**Ziel:** Benutzer kontrolliert mit Teilnehmern verknüpfen und persönliche Ergebnisse sicher bereitstellen.

### Aufgaben

- [x] Administrationsservice für Verknüpfungen implementieren.
- [x] Endpunkt zum Anzeigen der Verknüpfungen implementieren.
- [x] Endpunkt zum Anlegen einer Verknüpfung implementieren.
- [x] Endpunkt zum Entfernen einer Verknüpfung implementieren.
- [x] Nur Administratoren für Änderungen zulassen.
- [x] Doppelte Verknüpfungen verhindern.
- [x] Vor jeder Anlage prüfen, ob der Teilnehmer tatsächlich existiert.
- [x] Anlage und Entfernung auditieren.
- [ ] Authorization Service für eigene Ergebnisse implementieren.
- [ ] Authentifizierten Benutzer serverseitig ermitteln.
- [ ] Aktuelle Verknüpfungen aus der Datenbank laden.
- [ ] Tatsächliche Rennteilnahme prüfen.
- [ ] Sichtbarkeit des konkreten Ergebnisses prüfen.
- [ ] Manipulierte `ParticipantId`-Parameter ablehnen.
- [ ] Verknüpfung nicht in langlebigen Token-Claims voraussetzen.

### Abschlusskriterien

- Selbstzuordnung ist nicht möglich.
- Namen oder E-Mail-Adressen lösen keine automatische Zuordnung aus.
- Ein Benutzer kann mehrere Teilnehmer haben.
- Ein Teilnehmer kann mehreren Benutzern zugeordnet sein.
- Fremde Ergebnisse sind trotz manipulierter IDs nicht erreichbar.
- Alle Zuordnungsänderungen enthalten Wer-, Wann- und Was-Informationen.

---

## Phase 9: SignalR-Sicherheit

**Ziel:** SignalR entsprechend den REST-Regeln authentifizieren und autorisieren.

### Aufgaben

- [x] Bestehenden RaceTimer-Hub standardmäßig schützen.
- [x] Bearer-Authentifizierung für SignalR konfigurieren.
- [x] Berechtigung beim Verbindungsaufbau prüfen.
- [x] Berechtigung bei `SubscribeToRaceChanges` prüfen.
- [ ] Abonnements auf zulässige Rennbereiche begrenzen.
- [ ] Öffentliche Live-Daten von geschützten Gruppen trennen.
- [ ] Separaten öffentlichen Live-Hub oder gleichwertige isolierte Zugriffsschicht implementieren.
- [ ] Öffentliche DTOs und Gruppennamen verwenden.
- [ ] Administrative und private Events niemals in öffentliche Gruppen senden.
- [ ] Tokenübertragung für die spätere MAUI-App über `accessTokenProvider` vorbereiten.
- [ ] Verbindungs- und Autorisierungsereignisse ohne Tokeninhalte protokollieren.

### Abschlusskriterien

- Anonyme Clients können keinen geschützten Hub verwenden.
- Ein gültiges Token allein erlaubt noch kein beliebiges Rennabonnement.
- Öffentliche Gruppen sind nur bei aktivierter Konfiguration nutzbar.
- Private Daten werden nicht über öffentliche Kanäle verteilt.

---

## Phase 10: Docker Compose und Produktionsbetrieb

**Ziel:** Eine reproduzierbare Standardbereitstellung mit PostgreSQL und persistenten Secrets schaffen.

### Aufgaben

- [ ] PostgreSQL-Service in Docker Compose konfigurieren.
- [ ] Persistentes PostgreSQL-Volume konfigurieren.
- [ ] Healthcheck für PostgreSQL ergänzen.
- [ ] Server-Start von verfügbarer Datenbank abhängig machen.
- [ ] PostgreSQL-Connection-String über Umgebungsvariablen bereitstellen.
- [ ] Web- und Server-URLs für interne und externe Kommunikation trennen.
- [ ] Persistentes Volume oder Secret Store für OpenIddict-Schlüssel konfigurieren.
- [ ] Entwicklungs- und Produktionsschlüssel trennen.
- [ ] Migrationen und Backup-Verfahren dokumentieren.
- [ ] HTTPS- und Reverse-Proxy-Anforderungen dokumentieren.
- [ ] Prüfen, dass keine Secrets in Repository, Images oder Logs gelangen.

### Abschlusskriterien

- Compose startet PostgreSQL, Server und Web-Anwendung reproduzierbar.
- Ein Neustart verliert weder Daten noch OpenIddict-Schlüssel.
- Healthchecks melden fehlerhafte Abhängigkeiten.
- Die produktive Konfiguration erzwingt HTTPS für externe Verbindungen.

---

## Phase 11: Tests und Sicherheitsprüfung

**Ziel:** Funktionale, sicherheitsrelevante und betriebliche Anforderungen nachweisen.

### Automatisierte Tests

- [ ] Identity-Benutzer ohne E-Mail-Adresse
- [ ] eindeutige Benutzernamen
- [ ] deaktivierte Benutzer
- [ ] Fehlversuchssperre
- [ ] idempotente Rollenanlage
- [ ] Bootstrap ohne Administrator
- [ ] ungültiger Einmal-Token
- [ ] Wiederverwendung des Einmal-Tokens
- [ ] geschützte API ohne Token (`401`)
- [ ] API mit falscher Rolle (`403`)
- [ ] Zugriff auf eigene Ergebnisse
- [ ] verweigerter Zugriff auf fremde Ergebnisse
- [ ] manipulierte `ParticipantId`
- [ ] öffentliche Endpunkte standardmäßig deaktiviert
- [ ] öffentliche DTOs ohne private Daten
- [ ] SignalR-Hub ohne Token
- [ ] SignalR-Gruppenautorisierung
- [ ] OIDC-Discovery und Tokenfluss
- [ ] Web-Login und Logout
- [ ] API-Aufruf der Web-App mit Bearer Token
- [ ] Tokenablauf und erneute Anmeldung
- [ ] PostgreSQL-Migration auf leerer Datenbank
- [ ] Neustart mit persistenten Schlüsseln

### Manuelle Sicherheitsprüfung

- [ ] Alle Controller ohne Token prüfen.
- [ ] Alle Verwaltungsfunktionen mit normalen Benutzerrollen prüfen.
- [ ] Teilnehmer- und Ergebniszugriffe mit manipulierten IDs prüfen.
- [ ] Setup-Endpunkt nach erfolgreicher Einrichtung prüfen.
- [ ] Refresh-Token-Widerruf prüfen.
- [ ] Öffentliche Live-Daten auf Datenlecks prüfen.
- [ ] Docker-Volumes und Secret-Konfiguration prüfen.
- [ ] Logs auf Passwörter, Access Tokens und Refresh Tokens prüfen.
- [ ] HTTPS-Verhalten außerhalb der Entwicklungsumgebung prüfen.

### Abschlusskriterien

- Relevante automatisierte Tests sind erfolgreich.
- Es bestehen keine ungeklärten kritischen oder hohen Sicherheitsbefunde.
- Build und Datenbankmigrationen sind erfolgreich.
- Die manuelle Sicherheitscheckliste ist vollständig bearbeitet.

---

## Phase 12: MAUI-OIDC-Folgephase

**Status:** Zurückgestellt; nicht Bestandteil der ersten Implementierungsphase.

### Vorbereitung in Phase 1 bis 11

- [ ] `ITokenStore` als Abstraktion vorsehen.
- [ ] `IOidcAuthenticationService` als Abstraktion vorsehen.
- [ ] `IServerSession` oder gleichwertige Sitzungsabstraktion vorsehen.
- [ ] API-Client für austauschbare Bearer-Token-Versorgung vorbereiten.
- [ ] SignalR-Client für `accessTokenProvider` vorbereiten.
- [ ] Offline- und Online-Repository klar getrennt halten.

### Spätere Umsetzung

- [ ] Authorization Code Flow mit PKCE
- [ ] Systembrowser
- [ ] App- oder Universal-Link-Redirects
- [ ] sichere Tokenablage mit `SecureStorage`
- [ ] Access- und Refresh-Token-Lebenszyklus
- [ ] REST-Bearer-Authentifizierung
- [ ] SignalR-Bearer-Authentifizierung
- [ ] Verhalten bei abgelaufener Anmeldung
- [ ] unveränderter lokaler Standalone-Betrieb

---

# Berechtigungsübersicht

| Funktion | Anonym | Viewer | Participant | Official | RaceManager | Administrator |
|---|---:|---:|---:|---:|---:|---:|
| Öffentliche Live-Daten | nur bei Freigabe | ja | ja | ja | ja | ja |
| Geschützte allgemeine Lesedaten | nein | ja | nach Policy | ja | ja | ja |
| Eigene verknüpfte Ergebnisse | nein | nach Verknüpfung | ja | ja | ja | ja |
| Beliebige Ergebnisse | nein | nach Policy | nein | nach Policy | nach Policy | ja |
| Rennen verwalten | nein | nein | nein | nein | ja | ja |
| Teilnehmer verwalten | nein | nein | nein | nein | ja | ja |
| Zeitpunkte korrigieren | nein | nein | nein | ja | nein, sofern nicht zusätzlich berechtigt | ja |
| Benutzer verwalten | nein | nein | nein | nein | nein | ja |
| Teilnehmerverknüpfungen verwalten | nein | nein | nein | nein | nein | ja |

Diese Tabelle ist vor dem Schutz der einzelnen Endpunkte zu validieren und bei abweichenden fachlichen Entscheidungen zu aktualisieren.

# Erwartete technische Artefakte

- [ ] Server-Konfigurations- und Optionsklassen
- [ ] Identity-Benutzermodell
- [ ] serverseitiger Identity-/OpenIddict-DbContext
- [ ] EF-Core-Migrationen
- [ ] Rollen- und Policy-Registrierung
- [ ] Bootstrap-Service und Setup-Endpunkt
- [ ] OpenIddict-Konfiguration
- [ ] Web-OIDC-Konfiguration
- [ ] serverseitiger Token-Service für API-Aufrufe
- [ ] Public-Access-Service
- [ ] öffentliche DTOs
- [ ] Benutzer-Teilnehmer-Service
- [ ] Audit-Service bzw. Auditpersistenz
- [ ] autorisierte REST-Endpunkte
- [ ] geschützter SignalR-Hub
- [ ] öffentlicher Live-Hub oder isolierte Public-Live-Schicht
- [ ] Docker-Compose-Anpassungen
- [ ] automatisierte Tests
- [ ] Betriebs- und Sicherheitsdokumentation

# Fortschrittsjournal

| Datum | Phase/Aufgabe | Status | Ergebnis / nächste Aktion |
|---|---|---|---|
| 2026-06-03 | Planungsdokument erstellt | Erledigt | Umsetzung kann mit Phase 1 beginnen |
| 2026-06-03 | Phase 1: Paket- und Konfigurationsgrundlage | Erledigt | Identity 10.0.8, Npgsql 10.0.0, OpenIddict 7.0.0; SQLite-Standalone bleibt kompatibel |
| 2026-06-03 | Phase 2/3: Identity-Grundmodell | In Arbeit | Benutzer, Verknüpfungsmodell, DbContext, Rollen und Policies angelegt; Migration und Clientregistrierung folgen |
| 2026-06-03 | Phase 2: Migrationserzeugung | Blockiert | `dotnet-ef` ist in der Entwicklungsumgebung nicht installiert; Design-Time-Factory ergänzt, Toolinstallation und Migration folgen |
| 2026-06-03 | Phase 2: Identity-Migration | Erledigt | `IdentityAndOpenIddict` mit EF Core 10.0.8 erzeugt; bestehende SQLite-Migrationen unverändert |
| 2026-06-03 | Phase 6: Web-OIDC-Grundlage | In Arbeit | Webclient erhält eigenes Cookie; Authority und interne API-URL werden getrennt konfiguriert |
| 2026-06-03 | Phase 10: Compose-Grundlage | In Arbeit | PostgreSQL-Service, persistentes Volume, Healthcheck und Secret-Umgebungsvariablen ergänzt |
| 2026-06-03 | Phase 7/9: API- und SignalR-Grundschutz | In Arbeit | Controller-Fallback und Änderungs-Policies sowie bedingter Hub-Schutz umgesetzt; Public-Live-Schicht folgt |
| 2026-06-03 | Phase 4/5/8: Bootstrap, OIDC und Zuordnungen | In Arbeit | Setup-/Authorize-Endpunkte, OIDC-Scopes, Client-Seed sowie administrative Teilnehmerzuordnung mit Audit ergänzt |
| 2026-06-03 | Laufzeittest Standalone | Erledigt | Serverstart mit `Authentication:Enabled=false` und `/health` erfolgreich; keine bestehenden Tests im Workspace gefunden |
| 2026-06-03 | Compose-Validierung | Erledigt | YAML-Struktur mit temporären Testsecrets validiert; echte Secrets bleiben Pflicht und werden nicht versioniert |
| 2026-06-03 | Phase 7: Public-Live-Schicht | In Arbeit | `/api/public/events/live`, Live-Detail und Finished-Results mit deaktivierten Defaults und bereinigten DTOs ergänzt |
| 2026-06-03 | Phase 9: Hub-Methoden | In Arbeit | Gruppenabonnements und serverseitige Broadcast-Methoden mit Policies versehen |
| 2026-06-03 | Validierung | Erledigt | Gesamtlösung kompiliert; Compose mit Testsecrets validiert; Standalone-Healthcheck zuvor erfolgreich |
| 2026-06-03 | OIDC-Token-Endpunkt | Erledigt | Authorization-Code- und Refresh-Token-Exchange ergänzt; produktive Zertifikatspfade werden außerhalb der Entwicklung verpflichtend konfiguriert |
| 2026-06-03 | OIDC-Passthrough | Erledigt | Token-Endpunkt wird explizit an den AuthorizationController weitergereicht |

Neue Einträge werden chronologisch ergänzt. Jeder Eintrag soll mindestens Datum, betroffene Phase, Status und Ergebnis enthalten.

# Entscheidungsprotokoll

| ID | Datum | Thema | Entscheidung | Auswirkung |
|---|---|---|---|---|
| ADR-001 | 2026-06-03 | Datenbankprovider | PostgreSQL als Compose-Standard | Npgsql, PostgreSQL-Migrationen und PostgreSQL-Compose-Service erforderlich |
| ADR-002 | 2026-06-03 | Identity-Datenbank | Gemeinsame Datenbank mit RaceTimer-Daten | Gemeinsame Betriebs- und Backup-Strategie; getrennte Kontexte weiterhin möglich |
| ADR-003 | 2026-06-03 | Administrator-Bootstrap | Interaktiver Setup mit Einmal-Token | Kein bekanntes Standardpasswort; Setup-Sicherheitsmodell erforderlich |
| ADR-004 | 2026-06-03 | Öffentliche Zugriffe | Alle öffentlichen Funktionen zunächst deaktiviert | Public-Access-Konfiguration und explizite Freigaben erforderlich |
| ADR-005 | 2026-06-03 | MAUI-Authentifizierung | Systembrowser mit App-/Universal-Link | MAUI-OIDC wird als separate Phase umgesetzt |
| ADR-006 | 2026-06-03 | Implementierungsumfang | Server und Web zuerst | MAUI bleibt in der ersten Phase unangetastet |
| ADR-007 | 2026-06-03 | Kompatibilitätsmodus | `Authentication:Enabled=false` erhält den bisherigen SQLite-Serverbetrieb | Identity/PostgreSQL wird nur im aktivierten Authentifizierungsmodus initialisiert |
| ADR-008 | 2026-06-03 | Web-URL-Trennung | OIDC-Authority (Browser) und API-Server-URL (Web-Container) sind getrennte Einstellungen | Verhindert, dass Browser interne Compose-DNS-Namen verwendet |

# Änderungsprotokoll

| Version | Datum | Änderung | Begründung |
|---|---|---|---|
| 1.0 | 2026-06-03 | Initialer Implementierungsplan angelegt | Umsetzung des Authentifizierungs- und Autorisierungskonzepts planbar und nachvollziehbar machen |

# Abnahme der ersten Implementierungsphase

Die erste Implementierungsphase darf erst als abgeschlossen markiert werden, wenn alle folgenden Kriterien erfüllt sind:

- [ ] Server startet mit PostgreSQL.
- [ ] RaceTimer- und Identity-Daten werden persistent gespeichert.
- [ ] Administrator kann einmalig eingerichtet werden.
- [ ] Kein bekanntes Standardpasswort wird erzeugt.
- [ ] OpenIddict Discovery, Authorization Code Flow und Token-Ausgabe funktionieren.
- [ ] RaceTimerApp.Web kann sich per OIDC anmelden.
- [ ] Die Web-Anwendung verwendet ein eigenes Cookie.
- [ ] Geschützte API-Endpunkte liefern ohne Authentifizierung `401`.
- [ ] Unberechtigte authentifizierte Benutzer erhalten `403`.
- [ ] Öffentliche Funktionen sind standardmäßig deaktiviert.
- [ ] Teilnehmerverknüpfungen werden nur durch Administratoren verwaltet.
- [ ] Eigene Ergebnisse werden serverseitig anhand der aktuellen Verknüpfung geprüft.
- [ ] Geschützte SignalR-Verbindungen und Abonnements sind autorisiert.
- [ ] Öffentliche Daten enthalten keine privaten oder administrativen Informationen.
- [ ] OpenIddict-Schlüssel und Datenbankdaten überstehen Containerneustarts.
- [ ] Build, Migrationen, automatisierte Tests und manuelle Sicherheitsprüfung sind erfolgreich.

# Arbeitsregeln für die KI-gestützte Umsetzung

1. Vor jeder Phase den aktuellen Status in diesem Dokument auf **In Arbeit** setzen.
2. Vor Änderungen die betroffenen Dateien und vorhandenen Abhängigkeiten prüfen.
3. Nur die Aufgaben der aktuellen Phase bearbeiten, sofern keine zwingende Abhängigkeit eine Vorziehung erfordert.
4. Änderungen minimal und kompatibel mit dem bestehenden lokalen Standalone-Betrieb halten.
5. Nach jeder relevanten Änderung kompilieren und passende Tests ausführen.
6. Neue Architekturentscheidungen im Entscheidungsprotokoll ergänzen.
7. Blockierende Fragen im Status als **Blockiert** markieren und im Fortschrittsjournal dokumentieren.
8. Nach erfolgreicher Prüfung die Phase auf **Abgeschlossen** setzen und den Prozentwert aktualisieren.
9. Die Phase erst nach manueller oder ausdrücklich dokumentierter fachlicher Abnahme auf **Abgenommen** setzen.
10. Keine Passwörter, Tokens, privaten Schlüssel oder produktiven Secrets in Quelldateien, Logs oder Dokumentation eintragen.
