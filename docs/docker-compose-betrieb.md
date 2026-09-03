# Docker-Compose-Betrieb

## Start

Voraussetzungen sind Docker Desktop mit Compose-Unterstützung. Optional können die Host-Ports über eine `.env`-Datei angepasst werden; `.env.example` enthält die verfügbaren Variablen.

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

Der RaceTimerServer ist danach standardmäßig unter `http://localhost:8080` erreichbar. Der serverseitige Blazor-Webhost ist unter `http://localhost:8088` erreichbar.

## Architektur

Der Browser kommuniziert ausschließlich mit dem Webhost. Der Webhost verwendet intern `http://server:8080`; diese Adresse wird nicht an den Browser ausgegeben. Der Server ist zusätzlich über den veröffentlichten Host-Port für unabhängige Clients erreichbar.

Die Serverdatenbank liegt im benannten Volume `racetimer-server-data`. Das Volume darf bei regulären Updates nicht gelöscht werden.

Das Runtime-Image legt `/data` für den Containerbenutzer an und setzt die passenden Besitzrechte. Bei einem bereits mit einer älteren Image-Version angelegten Volume muss die Berechtigung einmalig korrigiert oder das Volume bewusst neu angelegt werden.

## Prüfung

```powershell
docker compose config
docker compose ps
docker compose logs server web
```

Beide Services müssen den Status `healthy` erreichen. Beim Beenden bleiben die Daten erhalten:

```powershell
docker compose down
```

`docker compose down -v` löscht dagegen auch die persistente Datenbank und darf nur bewusst für einen vollständigen Reset verwendet werden.

## Sicherheitsgrenzen

Die Entwicklungsvariante nutzt unverschlüsseltes HTTP. Für eine Veröffentlichung muss `RACETIMER_SERVER_ISSUER` auf eine HTTPS-URL gesetzt und TLS an einem Reverse Proxy terminiert werden. Der Server verarbeitet außerhalb der Entwicklung `X-Forwarded-For` und `X-Forwarded-Proto`; der Proxy darf diese Header nur selbst setzen. Zertifikatspfade für OpenIddict werden über `Authentication__SigningCertificatePath` und `Authentication__EncryptionCertificatePath` aus einem Secret-Mount bereitgestellt.
