# FundFlow – Arbeitsregeln für die KI-gestützte Entwicklung

## Maßgebliche Quelle

`docs/01_Fachkonzept.md` ist die verbindliche fachliche Grundlage. Regeln (BR-xx), Testfälle (TC-xx), User Stories (US-xx) und Entscheidungen (E-xx) werden dort definiert. Weicht die Umsetzung davon ab, wird zuerst das Fachkonzept angepasst und die Änderung freigegeben, erst danach der Code.

## Vorgehen

- Gearbeitet wird in Etappen (siehe README, Abschnitt „Projektstand“). Nach jeder Etappe anhalten, Ergebnis und Testlauf zeigen und auf Freigabe warten.
- Pro Etappe ein Git-Commit. Kein Push zu GitHub ohne ausdrückliche Zustimmung.
- Keine neuen Funktionen außerhalb des Umfangs von Version 1 (Fachkonzept, Abschnitt 5).

## Projektstruktur

| Projekt | Inhalt | Darf verweisen auf |
|---|---|---|
| `src/FundFlow.Domain` | Fachmodell, Regeln, Wirksamkeitstermin – keine Abhängigkeiten zu EF Core oder ASP.NET | – |
| `src/FundFlow.Infrastructure` | EF Core, SQLite, Sitzungsfilter, Musterdaten, Auftragsanlage | Domain |
| `src/FundFlow.Scenarios` | Testfälle TC-01 … TC-30 als Daten und Ausführung | Domain, Infrastructure |
| `src/FundFlow.Web` | Razor Pages | alle `src`-Projekte |
| `tests/FundFlow.Tests` | xUnit | alle Projekte |

## Konventionen

- Code-Bezeichner englisch; Oberfläche, Meldungstexte und Dokumentation deutsch.
- Regel- und Testfall-IDs im Code nennen, z. B. `// BR-04` oder Testnamen mit `TC-04`.
- Beträge als `decimal`, Anteile als `int`, Datumswerte als `DateOnly`, Zeitstempel als `DateTimeOffset` in UTC.
- „Heute“ immer über `TimeProvider` ermitteln, nie über `DateTime.Now`/`DateTime.Today`. Fachliches Datum in der Zeitzone Europe/Berlin.
- Validierung immer serverseitig; alle Verstöße gemeinsam zurückgeben.
- Nichts löschen, was fachlich protokolliert ist: Status und Verweise statt Überschreiben.
- Warnungen gelten als Fehler (`Directory.Build.props`).

## Befehle

```bash
dotnet build
dotnet test
dotnet run --project src/FundFlow.Web
```

## Darstellung

Fachanwendung, keine Firmen-Website: keine untere Kontaktleiste, kein Hero-Bild. Prüfung bei 375 px und 320 px Breite, kein horizontales Scrollen.
