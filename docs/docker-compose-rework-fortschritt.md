# Fortschritt: Docker-Compose-Rework

## Aktueller Stand

- **Schritt:** Punkt 6 – Tests, Dokumentation und Betriebsübergabe
- **Status:** abgeschlossen
- **Umsetzungsbeschreibung:** Der Webhost erhält einen eigenen Hosted-Modus mit fester Serveradresse aus `IConfiguration`. Die bestehende MAUI-/Offline-/Online-Konfiguration bleibt unverändert. UI und Service dürfen im Hosted-Modus weder die Serveradresse noch das Repository durch Benutzereingaben ändern. Server und Webhost besitzen leichte `/health`-Endpunkte für Compose.

## Reihenfolge und Status

1. Punkt 1 – Zielkonfiguration und Hosted-Modus: abgeschlossen (Build erfolgreich)
2. Punkt 2 – Interne Serverkommunikation des Webhosts: abgeschlossen (Build erfolgreich)
3. Punkt 3 – Compose-Datei am Repository-Root: abgeschlossen (`docker compose config` erfolgreich)
4. Punkt 4 – Containerbetrieb und HTTPS-Verhalten: abgeschlossen (Solution- und Image-Build erfolgreich)
5. Punkt 5 – WebAssembly-/Same-Origin-Strategie: abgeschlossen (Webhost-Build erfolgreich)
6. Punkt 6 – Tests, Dokumentation und Betriebsübergabe: abgeschlossen (Builds und Compose-Start erfolgreich)

## Offene Fragen und Probleme

- Der Server-Healthcheck schlug zunächst wegen fehlendem `wget` und danach wegen fehlender Schreibrechte für `/data/racetimer.db` fehl. Die Runtime-Images installieren nun `curl`; das Server-Image legt `/data` mit passenden Besitzrechten an. Die Compose-Checks verwenden `curl --fail`, und der Webcheck nutzt einen leichten `/health`-Endpoint statt des vollständigen Blazor-Renderings.
- Der erste Hosted-Create-Aufruf lieferte HTTP 500 wegen zyklischer EF-Navigationseigenschaften bei der JSON-Serialisierung. Der Server ignoriert diese Zyklen jetzt bei API-Antworten. Der Create-Aufruf liefert HTTP 201, die Weboberfläche HTTP 200; beide Container sind healthy.
