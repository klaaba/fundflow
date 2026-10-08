# FundFlow

**Business-Analysis-Prototyp für Änderungen an Fonds-Sparplänen**

Fiktive Daten · keine Anlageberatung · unabhängiges Demonstrationsprojekt

FundFlow ist ein unabhängiger Demonstrator für die fachliche Analyse und technische Umsetzung von Änderungsaufträgen zu fiktiven Fonds- und ETF-Sparplänen. Die Anwendung verwendet ausschließlich Beispieldaten und dient weder der Anlageberatung noch der Durchführung realer Wertpapiergeschäfte.

## Worum es geht

Eine Kundin möchte ihren bestehenden Sparplan ändern – Sparrate, Ausführungstag oder Fondsaufteilung. FundFlow zeigt den Weg dieser Anforderung von der fachlichen Beschreibung bis zur getesteten Umsetzung:

- Fachkonzept mit Prozess, Regeln, Statusmodell und Datenmodell
- User Stories mit Akzeptanzkriterien
- Testfälle mit Rückverfolgbarkeit zu jeder fachlichen Regel
- Umsetzung in C# mit automatisierten Tests

Das Fachkonzept liegt unter [docs/01_Fachkonzept.md](docs/01_Fachkonzept.md).

## Technik

- .NET 10, ASP.NET Core Razor Pages
- Entity Framework Core mit SQLite
- xUnit

## Lokal starten

Voraussetzung: .NET SDK 10.

```bash
dotnet build
dotnet test
dotnet run --project src/FundFlow.Web
```

Die Anwendung ist danach unter <http://localhost:5085> erreichbar.

## Projektstruktur

```
docs/                       Fachkonzept und Projektdokumentation
src/FundFlow.Domain/        Fachmodell, Regeln, Wirksamkeitstermin
src/FundFlow.Infrastructure Datenhaltung, Musterdaten, Auftragsanlage
src/FundFlow.Scenarios/     Testfälle als Daten
src/FundFlow.Web/           Oberfläche (Razor Pages)
tests/FundFlow.Tests/       automatisierte Tests
```

## Projektstand

| Etappe | Inhalt | Stand |
|---|---|---|
| 0 | Grundgerüst | erledigt |
| 1 | Fachregeln | erledigt |
| 2 | Wirksamkeitstermin | erledigt |
| 3 | Datenhaltung und Auftrag | erledigt |
| 4 | Testszenarien | offen |
| 5 | Oberfläche | offen |
| 6 | Fehlerbeispiel DEF-001 | offen |
| 7 | Dokumentation und Feinschliff | offen |
| 8 | Veröffentlichung | offen |
