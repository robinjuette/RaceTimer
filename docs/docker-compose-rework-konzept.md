# Konzept: Gemeinsames Docker-Compose-Hosting von RaceTimerServer und Web Client

## 1. Ziel und Abgrenzung

Der `RaceTimerServer` und der Race-Timer-Webclient sollen gemeinsam über ein Docker-Compose-Deployment betrieben werden.

Ziele:

- Der RaceTimerServer bleibt über einen veröffentlichten Host-Port für weitere, unabhängige Clients erreichbar.
- Der gehostete Webclient verwendet ausschließlich den in Compose gestarteten RaceTimerServer.
- Die Serveradresse des gehosteten Webclients darf nicht über die Weboberfläche oder eine frei editierbare Clientkonfiguration geändert werden.
- Die Kommunikation zwischen Webclient und Server soll innerhalb des Compose-Netzwerks über den Servicenamen des Servercontainers erfolgen.
- Die Lösung soll für .NET 10 und den Betrieb im lokalen/vertrauten Netzwerk geeignet sein.
- TLS wird zunächst nicht in den Containern umgesetzt. Eine spätere TLS-Terminierung erfolgt über einen vorgeschalteten Reverse Proxy.
- Der Server bleibt in dieser Ausbaustufe ohne Authentifizierung und ist nur für ein vertrauenswürdiges Netzwerk vorgesehen.
- Die Serverdatenbank wird über ein persistentes Docker-Volume erhalten.

Nicht Bestandteil dieses Reworks sind Datenbankwechsel, Authentifizierung/Autorisierung, TLS und eine Migration nach Azure. Diese Themen müssen vor einem Betrieb außerhalb des vertrauenswürdigen Netzwerks separat umgesetzt beziehungsweise entschieden werden.

## 2. Ist-Zustand im Repository

### Server

- Projekt: `RaceTimerServer/RaceTimerServer.csproj`
- Ziel-Framework: `net10.0`
- Dockerfile: `RaceTimerServer/Dockerfile`
- HTTP-Endpunkte: Controller, `/hubs/racetimer` via SignalR und in Development Swagger.
- Das Dockerfile erwartet den Repository-Root als Build-Kontext und veröffentlicht Port 8080 (zusätzlich ist 8081 deklariert).
- HTTPS-Weiterleitung ist im Server aktuell aktiviert und muss für den anfänglichen HTTP-Betrieb in Compose angepasst werden.

### Webhost und Web Client

- Webhost: `RaceTimerApp/RaceTimerApp.Web/RaceTimerApp.Web.csproj`
- WebAssembly-Projekt: `RaceTimerApp/RaceTimerApp.Web.Client/RaceTimerApp.Web.Client.csproj`
- Dockerfile: `RaceTimerApp/RaceTimerApp.Web/Dockerfile`
- Der Webhost registriert lokale und konfigurierbare Repositories und verwendet `AddInteractiveServerComponents()` sowie `AddInteractiveWebAssemblyComponents()`.
- `Routes` werden aktuell mit `InteractiveServer` gerendert; `HeadOutlet` verwendet `InteractiveAuto`.
- Die bestehende `AppConfigService` unterstützt Offlinebetrieb und die Eingabe einer beliebigen Online-Server-URL.
- Die Seite `RaceTimerApp/RaceTimerApp.Shared/Pages/Settings.razor` zeigt diese Umschaltung und die Server-URL-Eingabe noch an.

### Relevante technische Konsequenz

Ein Docker-Compose-Servicename wie `server` ist nur innerhalb des Compose-Netzwerks auflösbar. JavaScript/WebAssembly-Code im Browser läuft außerhalb dieses Netzwerks und kann `http://server:8080` nicht zuverlässig erreichen. Die bevorzugte erste Umsetzung nutzt deshalb den vorhandenen serverseitigen Blazor-Ausführungsmodus als Backend-for-Frontend (BFF): Der Webcontainer greift intern auf `http://server:8080` zu, der Browser spricht nur mit dem Webhost.

Falls der WebAssembly-Ausführungsmodus künftig tatsächlich verwendet werden soll, ist zusätzlich ein Same-Origin-Proxy erforderlich, zum Beispiel `/race-api` und `/race-hub` im Webhost. Eine interne Containeradresse darf niemals als Browser-URL ausgeliefert werden.

## 3. Zielarchitektur

### Festgelegte Variante: serverseitiger Webhost als BFF

```text
Externe Clients ───────► Host-Port 8080 ───────► server (RaceTimerServer:8080)

Browser ───────────────► Host-Port 8088 ───────► web (RaceTimerApp.Web:8080)
													│
													└── http://server:8080
														über Compose-Netzwerk
```

Compose startet zwei Services im selben privaten Netzwerk, beispielsweise `racetimer-network`:

- `server`: API und SignalR, intern über Port 8080; zusätzlich Veröffentlichung eines Host-Ports für externe Clients.
- `web`: Blazor Web App, intern über Port 8080; Veröffentlichung eines separaten Host-Ports für Browserzugriff.
- `server` erhält ein persistentes Volume für die vom Server verwendete Datenbank.

Der Webhost erhält die feste interne Serveradresse über eine deploymentseitige Konfiguration, beispielsweise `RaceTimer__ServerUrl=http://server:8080`. Diese Adresse ist keine Benutzereinstellung. Die Services dürfen über Compose-interne DNS-Namen kommunizieren; die Host-Portfreigabe wird nicht für die interne Kommunikation verwendet.

### Öffentliche Erreichbarkeit des Servers

Die Compose-Datei veröffentlicht den Server ausdrücklich im vertrauenswürdigen lokalen Netzwerk:

- `8080:8080` für HTTP.
- Der Webclient wird über `8088:8080` veröffentlicht.

In einer späteren Ausbaustufe kann ein TLS-fähiger Reverse Proxy vor den Webclient und/oder Server geschaltet werden. Dabei sind Forwarded Headers, WebSocket-Unterstützung, Origins und SignalR abzustimmen. Das ändert nicht die interne Web-zu-Server-Kommunikation über den Compose-Servicenamen.

## 4. Konfigurations- und Sicherheitskonzept

### Unveränderliche Hosted-Konfiguration

Für den Webhost ist ein eigener Betriebsmodus vorzusehen, zum Beispiel `Hosted` beziehungsweise `FixedServer`.

Empfohlene Regeln:

1. Die feste Server-URL wird ausschließlich aus `IConfiguration`/Umgebungsvariablen beim Containerstart gelesen.
2. Die Webanwendung registriert für den Hosted-Modus direkt ein Server-Repository mit dieser URL.
3. `Offline`, beliebige Online-URLs und das Umschalten des Repositories werden im gehosteten Webclient nicht angeboten.
4. `Settings.razor` blendet Betriebsmodus, Server-URL, Connect- und Disconnect-Aktionen im Hosted-Modus aus.
5. Die Sperre erfolgt nicht nur im UI: `AppConfigService` beziehungsweise eine neue Hosted-Konfigurationsabstraktion muss Versuche zum Ändern der URL oder zum Wechsel auf das lokale Repository ablehnen.
6. Die MAUI-/Offline-Anwendung darf ihre bisherige Konfigurationslogik behalten; die Hosted-Regel darf nicht global auf alle Clients angewendet werden.
7. Die effektive Server-URL wird nicht aus Browser-LocalStorage oder einer vom Benutzer bearbeitbaren JSON-Datei übernommen.

Die Anwendung sollte eine ungültige oder fehlende Hosted-Server-URL beim Start deutlich protokollieren und den Webclient in einen kontrollierten Fehlerzustand bringen, statt unbemerkt auf Offlinebetrieb zurückzufallen.

### Netzwerk- und Browserregeln

- Der Browser erhält für den empfohlenen BFF-Modus keine interne Containeradresse.
- CORS ist für serverseitige BFF-Aufrufe nicht erforderlich; falls externe Browserclients direkt die API verwenden, muss CORS auf bekannte Origins begrenzt werden.
- SignalR muss über den jeweils richtigen Pfad und die richtige Origin erreichbar sein. Im BFF-Modus wird die Verbindung aus dem Webcontainer aufgebaut.
- `UseHttpsRedirection()` muss für die erste HTTP-Ausbaustufe deaktiviert oder bedingt konfiguriert werden, damit keine Redirect-Schleife entsteht. Bei späterer TLS-Terminierung am Reverse Proxy sind Forwarded Headers und die externe HTTPS-Origin zu konfigurieren.
- Healthchecks sollen Server und Web unterscheiden: Server prüft den API-/Health-Endpunkt, Web prüft seinen HTTP-Endpunkt und optional die Erreichbarkeit des Server-Healthchecks.
- Die erste Ausbaustufe setzt voraus, dass Server und Webclient nur im vertrauenswürdigen lokalen Netzwerk betrieben werden. Vor einer Erreichbarkeit aus nicht vertrauenswürdigen Netzen sind Authentifizierung, Autorisierung, Secrets, Rate Limiting und restriktive CORS-/Host-Regeln umzusetzen.

## 5. Vorgeschlagene Änderungen nach Dateien

Die Umsetzung soll strikt in dieser Reihenfolge erfolgen. Nach jedem Punkt muss gebaut beziehungsweise getestet werden, bevor der nächste Punkt begonnen wird.

### Punkt 1 – Zielkonfiguration und Hosted-Modus analysieren

**Kurze Umsetzungsbeschreibung:** Die vorhandene Offline-/Online-Umschaltung von MAUI und Webhost trennen und einen ausschließlich deploymentgesteuerten Hosted-Modus für den Webhost definieren. Dabei sind die bisherige Repository-Registrierung, `AppConfigService`, `SettingsService` und `Settings.razor` zu berücksichtigen.

**Erwartete Dateien:** `RaceTimerApp/RaceTimerApp.Web/Program.cs`, gemeinsame Konfigurations-/Repository-Services und `RaceTimerApp/RaceTimerApp.Shared/Pages/Settings.razor`.

**Abnahmekriterien:** Der Hosted-Modus hat eine feste Server-URL aus Konfiguration; UI und Service können diese URL nicht durch Benutzereingaben ersetzen; die MAUI-Anwendung behält Offline/Online.

### Punkt 2 – Interne Serverkommunikation des Webhosts implementieren

**Kurze Umsetzungsbeschreibung:** Das Server-Repository und der SignalR-Client des Webhosts erhalten die interne Compose-Adresse `http://server:8080`. Die Konfiguration wird über strongly typed Options oder eine gleichwertige zentrale Abstraktion eingelesen, nicht über eine hardcodierte URL in Razor-Code.

**Erwartete Dateien:** Webhost-`Program.cs`, Konfigurationsabstraktion sowie betroffene gemeinsame Services.

**Abnahmekriterien:** Der Webhost verwendet im Containerbetrieb ausschließlich den Compose-Service `server`; ein fehlender Wert erzeugt einen kontrollierten Start-/Diagnosefehler.

### Punkt 3 – Compose-Datei am Repository-Root erstellen

**Kurze Umsetzungsbeschreibung:** Eine `docker-compose.yml` oder `compose.yaml` am Root mit den Services `server` und `web`, Repository-Root als Build-Kontext, beiden vorhandenen Dockerfiles, gemeinsamem privatem Netzwerk, Healthchecks, Restart-Policy und expliziten Portfreigaben erstellen.

**Empfohlene Schnittstelle:**

- `server`: `${RACETIMER_SERVER_PORT:-8080}:8080`
- `web`: `${RACETIMER_WEB_PORT:-8088}:8080`
- intern: `web -> http://server:8080`

**Abnahmekriterien:** `docker compose config` ist gültig; beide Images bauen; der Webservice hängt vom erfolgreichen Server-Healthcheck ab; der Server ist zusätzlich vom Host aus erreichbar.

### Punkt 4 – Containerbetrieb und HTTPS-Verhalten korrigieren

**Kurze Umsetzungsbeschreibung:** Dockerfiles, ASP.NET-Core-Bindings und HTTPS-Weiterleitung auf die Compose-Ports abstimmen. Der Server soll innerhalb des Containers auf `0.0.0.0:8080` lauschen. Die Wahl zwischen TLS im Container und TLS-Terminierung am Reverse Proxy ist verbindlich festzulegen.

**Abnahmekriterien:** Keine Redirect-Schleife, keine Bindung ausschließlich an localhost und funktionierende Healthchecks aus dem Compose-Netzwerk.

### Punkt 5 – WebAssembly-/Same-Origin-Strategie festschreiben

**Kurze Umsetzungsbeschreibung:** Den gehosteten Webclient ausschließlich mit `InteractiveServer` betreiben. Die aktuell registrierten WebAssembly-Komponenten werden für diese Betriebsart nicht als interaktiver Ausführungsmodus verwendet; ein Same-Origin-Proxy und browserseitige API-Aufrufe sind nicht Bestandteil dieses Reworks.

**Abnahmekriterien:** Alle API- und SignalR-Aufrufe des Hosted-Webclients erfolgen serverseitig. Im Browser werden keine Docker-DNS-Namen verwendet. Eine spätere Aktivierung von WebAssembly erfordert ein separates Rework mit Same-Origin-Proxy.

### Punkt 6 – Tests, Dokumentation und Betriebsübergabe

**Kurze Umsetzungsbeschreibung:** Build-, Compose-, Laufzeit-, Sicherheits- und Verbindungsprüfungen automatisieren sowie README und Beispielkonfiguration für Ports und Umgebungsvariablen ergänzen.

**Abnahmekriterien:** Die Prüfmatrix aus Abschnitt 7 ist erfolgreich; die feste Serverzuordnung ist für Betreiber nachvollziehbar und die wichtigsten Fehlerfälle sind dokumentiert.

## 6. Anleitung für eine KI-gestützte Umsetzung

Die Umsetzung sollte von einer KI in kleinen, überprüfbaren Schritten durchgeführt werden:

1. Zuerst die genannten Dateien im realen Workspace lesen und keine Pfade oder Registrierungen annehmen.
2. Vor jedem Punkt die bestehende Abhängigkeit und die geplante Änderung kurz dokumentieren.
3. Ausschließlich den aktuellen Punkt ändern; keine gleichzeitige Bereinigung unrelated bestehender Logik.
4. Nach einer Änderung die betroffenen Dateien auf Compilerfehler prüfen und anschließend den vorgesehenen Build ausführen.
5. Bei Fehlern die Ursache im aktuellen Workspace analysieren. Keine zusätzliche Konfigurationsschicht als bloßen Workaround einführen.
6. Erst nach erfolgreicher Prüfung zum nächsten Punkt wechseln.
7. Compose- und Laufzeittests erst ausführen, nachdem die .NET-Projekte erfolgreich gebaut wurden.
8. Änderungen an der MAUI-Konfiguration nur vornehmen, wenn sie für die Trennung zwischen MAUI und Hosted Web tatsächlich erforderlich sind.

## 7. Prüfmatrix

### Build und Compose

- `dotnet build RaceTimer.slnx`
- `docker compose config`
- `docker compose build`
- `docker compose up -d`
- Beide Healthchecks werden `healthy`.

### Funktion

- Webclient ist am konfigurierten Web-Port erreichbar.
- Der Webclient lädt Rennen und Teilnehmer über den mitgehosteten Server.
- Änderungen werden über SignalR im Webclient sichtbar.
- Ein externer Testclient erreicht API und, sofern vorgesehen, SignalR über den veröffentlichten Serverport.
- Der Server ist unter `http://<host>:8080` und der Webclient unter `http://<host>:8088` erreichbar.

### Unveränderbarkeit

- Die Settings-Seite zeigt im Hosted-Modus keine URL-Eingabe.
- Offline-Modus und beliebige externe Server sind im Hosted-Webclient nicht auswählbar.
- Ein direkter Aufruf der bisherigen Wechselmethoden führt im Hosted-Modus zu einer kontrollierten Ablehnung.
- Nach Neustart bleibt die Compose-Serverzuordnung erhalten und wird nicht aus Benutzerdaten überschrieben.

### Fehlerfälle

- Server-Neustart führt zu einer sichtbaren, kontrollierten Verbindungsunterbrechung.
- Fehlende oder ungültige `RaceTimer`-Konfiguration wird eindeutig protokolliert.
- Ein Browser versucht nicht, den internen DNS-Namen `server` aufzulösen.
- Der anfängliche HTTP-Betrieb erzeugt keine Redirect-Schleife; die spätere TLS-Terminierung am Reverse Proxy ist als separates Deployment-Szenario dokumentiert.

## 8. Getroffene Entscheidungen und verbleibende Betriebsgrenzen

Die folgenden Entscheidungen sind für dieses Rework verbindlich:

1. **TLS:** zunächst HTTP im lokalen/vertrauten Netzwerk; TLS wird später über einen vorgeschalteten Reverse Proxy ergänzt.
2. **Ports:** RaceTimerServer auf Host-Port `8080`, Webclient auf Host-Port `8088`.
3. **Absicherung:** zunächst keine Authentifizierung; der Betrieb ist auf ein vertrauenswürdiges Netzwerk begrenzt.
4. **Blazor:** ausschließlich `InteractiveServer`. Echte browserseitige WebAssembly-Ausführung und ein Same-Origin-Proxy sind nicht Teil dieses Reworks.
5. **Persistenz:** Die Serverdatenbank wird über ein persistentes Docker-Volume gespeichert.

Vor einer späteren Öffnung in nicht vertrauenswürdige Netze müssen mindestens TLS, Authentifizierung/Autorisierung, Secrets, Rate Limiting sowie restriktive CORS- und Host-Regeln ergänzt werden. Die spätere TLS-Ausbaustufe muss außerdem WebSockets/SignalR und Forwarded Headers berücksichtigen.
