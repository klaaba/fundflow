# FundFlow – Testbericht

**Stand:** 8. Oktober 2026 · Fachkonzept Version 1.5 · .NET SDK 10.0.401 · macOS 26.6

## 1. Ergebnis

| Kennzahl | Ergebnis |
|---|---|
| Automatisierte Tests | **179 von 179 bestanden** |
| Testfälle aus dem Fachkonzept (TC-01 bis TC-30) | **30 von 30 erfüllt** |
| Fachliche Regeln mit mindestens einem Testfall | **17 von 17** (BR-01 bis BR-17) |
| Abgleich Testkatalog ↔ Fachkonzept (Tabellen 12.2 und 12.3) | übereinstimmend |
| Darstellung bei 375 und 320 Pixel Breite | kein seitliches Scrollen auf allen Seiten |

Ausführen:

```bash
dotnet test
```

Dieselben 30 Testfälle laufen in der Anwendung unter **Testansicht** – mit erwartetem und tatsächlichem Wert je Einzelprüfung.

## 2. Teststrategie

Getestet wird auf mehreren Ebenen. Jede Ebene beantwortet eine eigene Frage:

| Ebene | Frage | Tests | Umfang |
|---|---|---|---|
| Regeln | Setzt jede Regel das Fachkonzept genau um – inklusive Grenzwerte? | 107 | Validator, Wirksamkeitstermin, Zeitzone, Statusmodell, Änderungsprotokoll |
| Verarbeitung | Wird ein Auftrag vollständig und in einem Schritt angelegt – oder gar nicht? | 20 | Auftragsanlage, Ersetzung, Versionen, Sitzungstrennung, Zurücksetzen, Aufräumen |
| Testkatalog | Sind die 30 Testfälle des Fachkonzepts erfüllt? | 31 | TC-01 bis TC-30 gegen Fachlogik und Datenbank; Vollständigkeit des Katalogs |
| Rückverfolgbarkeit | Hat jede Regel einen Testfall, und stimmt der Code mit dem Dokument überein? | 5 | liest `docs/01_Fachkonzept.md` ein und vergleicht |
| Oberfläche | Funktioniert der Ablauf so, wie ein Besucher ihn erlebt? | 16 | alle Seiten, Prüfen und Absenden, Fehlerfall, geänderter Termin, Sitzungstrennung, DEF-001 |

**Testumgebung.** Datenbanktests laufen gegen SQLite im Arbeitsspeicher mit den echten Migrationen. Das Datum ist fest vorgegeben (Referenzdatum 08.10.2026, für einzelne Fälle 12.10., 13.10. und 30.10.2026), sodass Termine und Grenzfälle reproduzierbar sind. Jeder Testfall des Katalogs erhält eine eigene Datenbank.

## 3. Testfälle des Fachkonzepts

| Test-ID | Titel | Regeln | Ergebnis |
|---|---|---|---|
| TC-01 | Sparrate erhöhen | BR-01, BR-08 | erfüllt |
| TC-02 | Sparrate unter Mindestbetrag | BR-01 | erfüllt |
| TC-03 | Dritten Fonds aufnehmen | BR-04, BR-11 | erfüllt |
| TC-04 | Fondsaufteilung ergibt 98 % | BR-04 | erfüllt |
| TC-05 | Unzulässiger Ausführungstag | BR-03 | erfüllt |
| TC-06 | Wunschdatum in der Vergangenheit | BR-06 | erfüllt |
| TC-07 | Auftragsanlage vollständig | BR-08 | erfüllt |
| TC-08 | Mindestbetrag genau erreicht | BR-01 | erfüllt |
| TC-09 | Mindestbetrag um 1 Cent unterschritten | BR-01 | erfüllt |
| TC-10 | Drei Nachkommastellen | BR-02 | erfüllt |
| TC-11 | Höchstbetrag genau erreicht | BR-09 | erfüllt |
| TC-12 | Höchstbetrag um 1 Cent überschritten | BR-09 | erfüllt |
| TC-13 | Fonds mit 0 % | BR-05 | erfüllt |
| TC-14 | Anteile mit Nachkommastellen | BR-10 | erfüllt |
| TC-15 | Sechs Fonds | BR-11 | erfüllt |
| TC-16 | Fonds doppelt | BR-12 | erfüllt |
| TC-17 | Nicht sparplanfähiger Fonds | BR-13 | erfüllt |
| TC-18 | Wunschdatum heute | BR-06, BR-07 | erfüllt |
| TC-19 | Annahmeschluss genau eingehalten | BR-07 | erfüllt |
| TC-20 | Annahmeschluss verpasst | BR-07 | erfüllt |
| TC-21 | Ausführungstag wechseln | BR-03, BR-07 | erfüllt |
| TC-22 | Wunschdatum genau zwölf Monate voraus | BR-14 | erfüllt |
| TC-23 | Wunschdatum mehr als zwölf Monate voraus | BR-14 | erfüllt |
| TC-24 | Keine Änderung | BR-15 | erfüllt |
| TC-25 | Offenen Auftrag ersetzen | BR-08, BR-16 | erfüllt |
| TC-26 | Sparrate nicht lesbar | BR-17 | erfüllt |
| TC-27 | Mehrere Fehler gleichzeitig | BR-01, BR-04 | erfüllt |
| TC-28 | Doppelte Einreichung des offenen Auftrags | BR-15 | erfüllt |
| TC-29 | Rücknahme ohne Storno | BR-15 | erfüllt |
| TC-30 | Annahmeschluss verpasst bei neuem Ausführungstag | BR-03, BR-07 | erfüllt |

Eingaben, Ausgangsstände und Erwartungen je Fall: Fachkonzept, Abschnitt 12.

## 4. Gegenproben: Finden die Tests tatsächlich Fehler?

Grüne Tests beweisen nur etwas, wenn sie bei einem Fehler rot werden. Deshalb wurde in jeder Etappe gezielt ein Fehler eingebaut, der Testlauf beobachtet und der Fehler wieder entfernt.

| Eingebauter Fehler | Fehlgeschlagene Tests | Ergebnis |
|---|---|---|
| Mindestsparrate 20 € statt 25 € | BR-01-Grenzwerte (24,99 € und 20,00 €), TC-27 | erkannt |
| Vorlauf 2 statt 3 Tage | TC-20, TC-30, Jahreswechsel | erkannt |
| Vorlauf 4 statt 3 Tage | TC-19 (Grenzwert Annahmeschluss) | erkannt |
| Sitzungsfilter abgeschaltet | 4 Tests zur Sitzungstrennung | erkannt |
| Ersetzung des offenen Auftrags weggelassen | TC-25 | erkannt |
| Gültigkeitsende der alten Version nicht gesetzt | TC-07, TC-20, TC-25, Version nach Wirksamkeit | erkannt |
| Prüfung auf Sparplanfähigkeit (BR-13) entfernt | TC-17 und 2 Regeltests – mit lesbarer Begründung je Prüfung | erkannt |
| TC-30 im Fachkonzept aus der Matrix gestrichen | Rückverfolgbarkeitstest nennt die Abweichung | erkannt |
| DEF-001: Summenprüfung beim Absenden übersprungen | TC-04, TC-27, Auftragsverarbeitung, Testansicht (28 von 30) | erkannt, siehe [Fehlerbericht](04_Fehlerbericht_DEF-001.md) |

Die Gegenproben bis auf DEF-001 wurden nach dem Testlauf zurückgenommen und sind nicht Teil der Historie. DEF-001 ist als eigenes Fehlerbeispiel mit Einbau und Behebung im Repository nachvollziehbar.

## 5. Manuelle Prüfung im Browser

| Ablauf | Beobachtung |
|---|---|
| Fehlerfall 60 / 30 / 8 % | Laufende Summe springt auf 98 % (rot); nach „Änderung prüfen“ Meldung mit BR-04 oben und am Feld; kein Auftrag |
| Gültige Änderung | Zusammenfassung mit Wirksamkeitstermin und nur den geänderten Angaben; nach dem Absenden Bestätigung, Auftragsdetail, Statusverlauf |
| Zweiter Auftrag bei offenem ersten | Formular mit Werten des offenen Auftrags vorbelegt; Hinweis auf Ersetzung (BR-16); Protokoll vergleicht mit gültiger Version (E-08); erster Auftrag „ersetzt“ |
| Versionsleiste | gültige, geplante und verworfene Versionen mit Zeiträumen |
| 375 und 320 Pixel | alle Seiten ohne seitliches Scrollen; Tabellen werden zu Blöcken |

Dabei gefundene und behobene Mängel: Beträge brachen in Tabellen um; die Auftragsliste war bei 320 Pixel breiter als der Bildschirm (durch `overflow: hidden` verdeckt); Auftragsnummern brachen am Bindestrich um; neue Fondszeilen hatten für Screenreader keine Nummer; die Regelspalte der Testansicht war zu breit.

## 6. Nicht getestet

- Last und gleichzeitige Zugriffe vieler Besucher (SQLite, siehe [Architektur](02_Architektur.md), Abschnitt 10)
- Browser außer dem eingebauten Chromium-Browser der Entwicklungsumgebung
- Betrieb hinter dem Reverse Proxy mit HTTPS (folgt mit der Veröffentlichung)
