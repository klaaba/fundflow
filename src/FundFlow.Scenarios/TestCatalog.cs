using FundFlow.Domain.Rules;
using FundFlow.Infrastructure.Orders;

namespace FundFlow.Scenarios;

/// <summary>
/// Testfälle TC-01 bis TC-30 aus Fachkonzept 12.2 – einmal definiert, genutzt von den
/// xUnit-Tests und der Testansicht. Nicht genannte Eingaben entsprechen dem Ausgangsstand;
/// das Wunschdatum ist das Referenzdatum.
/// </summary>
public static class TestCatalog
{
    public static readonly DateOnly DefaultReferenceDate = new(2026, 10, 8);

    private const string W01 = "Anteil Demo Welt Aktien ETF (INS-01)";
    private const string W02 = "Anteil Demo Europa Aktien ETF (INS-02)";
    private const string W03 = "Anteil Demo Schwellenländer ETF (INS-03)";

    private static readonly ScenarioExpectation Created = new() { Outcome = SubmitOutcome.Created };

    private static ScenarioExpectation Rejected(params string[] rules) =>
        new() { Outcome = SubmitOutcome.Rejected, ViolatedRules = rules };

    public static IReadOnlyList<TestScenario> All { get; } =
    [
        Define("TC-01", "Sparrate erhöhen", ["BR-01", "BR-08"],
            "Sparrate 250,00 €", Input(amount: "250,00"),
            "Auftrag angelegt, Wirksamkeitstermin 15.10.2026; Protokoll: Sparrate 150,00 € → 250,00 €.",
            Created with
            {
                EffectiveDate = new(2026, 10, 15),
                ChangeLog = [("Sparrate", "150,00 €", "250,00 €")],
            }),

        Define("TC-02", "Sparrate unter Mindestbetrag", ["BR-01"],
            "Sparrate 20,00 €", Input(amount: "20,00"),
            "Abgelehnt, Meldung BR-01, kein Auftrag.",
            Rejected("BR-01") with { MessagesContain = [RuleMessages.MinMonthlyAmount] }),

        Define("TC-03", "Dritten Fonds aufnehmen", ["BR-04", "BR-11"],
            "INS-01 50 %, INS-02 30 %, INS-03 20 %",
            Input(allocations: [("INS-01", "50"), ("INS-02", "30"), ("INS-03", "20")]),
            "Auftrag angelegt; Protokoll: INS-01 60 → 50, INS-02 40 → 30, INS-03 – → 20.",
            Created with
            {
                ChangeLog = [(W01, "60 %", "50 %"), (W02, "40 %", "30 %"), (W03, "–", "20 %")],
            }),

        Define("TC-04", "Fondsaufteilung ergibt 98 %", ["BR-04"],
            "INS-01 60 %, INS-02 30 %, INS-03 8 %",
            Input(allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]),
            "Abgelehnt, Meldung „… ergibt aktuell 98 % …“. Regressionstest zu DEF-001.",
            Rejected("BR-04") with { MessagesContain = ["ergibt aktuell 98 %"] }),

        Define("TC-05", "Unzulässiger Ausführungstag", ["BR-03"],
            "Ausführungstag 7 (direkte Anfrage)", Input(day: "7"),
            "Abgelehnt, Meldung BR-03.",
            Rejected("BR-03") with { MessagesContain = [RuleMessages.ExecutionDay] }),

        Define("TC-06", "Wunschdatum in der Vergangenheit", ["BR-06"],
            "Sparrate 200,00 €, Wunschdatum 07.10.2026", Input(amount: "200,00", requestedFrom: "2026-10-07"),
            "Abgelehnt, Meldung BR-06.",
            Rejected("BR-06") with { MessagesContain = [RuleMessages.RequestedFromNotPast] }),

        Define("TC-07", "Auftragsanlage vollständig", ["BR-08"],
            "Sparrate 200,00 €, Ausführungstag 1, INS-01 50 % / INS-02 50 %",
            Input(amount: "200,00", day: "1", allocations: [("INS-01", "50"), ("INS-02", "50")]),
            "Auftrag CR-2026-000001, Zeitstempel = Testzeit, Status „fachlich geprüft“, Statusverlauf mit einem Eintrag; " +
            "Version 2 gültig ab 01.11.2026; Version 1 gültig bis 31.10.2026.",
            Created with
            {
                RequestNumber = "CR-2026-000001",
                CreatedAtIsTestTime = true,
                Status = "fachlich geprüft",
                StatusHistoryEntries = 1,
                EffectiveDate = new(2026, 11, 1),
                NewVersionNo = 2,
                NewVersionValidFrom = new(2026, 11, 1),
                PreviousVersionValidTo = new(2026, 10, 31),
            }),

        Define("TC-08", "Mindestbetrag genau erreicht", ["BR-01"],
            "Sparrate 25,00 €", Input(amount: "25,00"),
            "Auftrag angelegt (Grenzwert).", Created),

        Define("TC-09", "Mindestbetrag um 1 Cent unterschritten", ["BR-01"],
            "Sparrate 24,99 €", Input(amount: "24,99"),
            "Abgelehnt, Meldung BR-01 (Grenzwert).", Rejected("BR-01")),

        Define("TC-10", "Drei Nachkommastellen", ["BR-02"],
            "Sparrate 250,555", Input(amount: "250,555"),
            "Abgelehnt, Meldung BR-02.", Rejected("BR-02")),

        Define("TC-11", "Höchstbetrag genau erreicht", ["BR-09"],
            "Sparrate 10.000,00 €", Input(amount: "10.000,00"),
            "Auftrag angelegt (Grenzwert).", Created),

        Define("TC-12", "Höchstbetrag um 1 Cent überschritten", ["BR-09"],
            "Sparrate 10.000,01 €", Input(amount: "10.000,01"),
            "Abgelehnt, Meldung BR-09 (Grenzwert).", Rejected("BR-09")),

        Define("TC-13", "Fonds mit 0 %", ["BR-05"],
            "INS-01 60 %, INS-02 40 %, INS-03 0 %",
            Input(allocations: [("INS-01", "60"), ("INS-02", "40"), ("INS-03", "0")]),
            "Abgelehnt, Meldung BR-05.", Rejected("BR-05")),

        Define("TC-14", "Anteile mit Nachkommastellen", ["BR-10"],
            "INS-01 33,5 %, INS-02 66,5 %",
            Input(allocations: [("INS-01", "33,5"), ("INS-02", "66,5")]),
            "Abgelehnt, Meldung BR-10.", Rejected("BR-10")),

        Define("TC-15", "Sechs Fonds", ["BR-11"],
            "INS-01 bis INS-06 mit 20/20/20/20/10/10 %",
            Input(allocations:
            [
                ("INS-01", "20"), ("INS-02", "20"), ("INS-03", "20"),
                ("INS-04", "20"), ("INS-05", "10"), ("INS-06", "10"),
            ]),
            "Abgelehnt, Meldung BR-11.", Rejected("BR-11")),

        Define("TC-16", "Fonds doppelt", ["BR-12"],
            "INS-01 50 %, INS-01 50 %",
            Input(allocations: [("INS-01", "50"), ("INS-01", "50")]),
            "Abgelehnt, Meldung BR-12.", Rejected("BR-12")),

        Define("TC-17", "Nicht sparplanfähiger Fonds", ["BR-13"],
            "INS-01 60 %, INS-07 40 % (direkte Anfrage)",
            Input(allocations: [("INS-01", "60"), ("INS-07", "40")]),
            "Abgelehnt, Meldung BR-13 mit Fondsname.",
            Rejected("BR-13") with { MessagesContain = ["Demo Immobilienfonds"] }),

        Define("TC-18", "Wunschdatum heute", ["BR-06", "BR-07"],
            "Sparrate 200,00 €, Wunschdatum 08.10.2026 (heute)", Input(amount: "200,00"),
            "Auftrag angelegt, Wirksamkeitstermin 15.10.2026, kein Verschiebungshinweis.",
            Created with { EffectiveDate = new(2026, 10, 15), Shifted = false }),

        Define("TC-19", "Annahmeschluss genau eingehalten", ["BR-07"],
            "Sparrate 200,00 €, Wunschdatum 12.10.2026", Input(amount: "200,00"),
            "Auftrag angelegt, Wirksamkeitstermin 15.10.2026, kein Hinweis (Grenzwert Annahmeschluss).",
            Created with { EffectiveDate = new(2026, 10, 15), Shifted = false },
            referenceDate: new(2026, 10, 12)),

        Define("TC-20", "Annahmeschluss verpasst", ["BR-07"],
            "Sparrate 200,00 €, Wunschdatum 13.10.2026", Input(amount: "200,00"),
            "Auftrag angelegt, Wirksamkeitstermin 15.11.2026, Verschiebungshinweis nennt 15.10.2026.",
            Created with
            {
                EffectiveDate = new(2026, 11, 15),
                Shifted = true,
                HintContains = "die Ausführung am 15.10.2026 erfolgt noch zu den bisherigen Konditionen",
            },
            referenceDate: new(2026, 10, 13)),

        Define("TC-21", "Ausführungstag wechseln", ["BR-03", "BR-07"],
            "Ausführungstag 1, Wunschdatum 20.10.2026", Input(day: "1", requestedFrom: "2026-10-20"),
            "Auftrag angelegt, Wirksamkeitstermin 01.11.2026, kein Hinweis; Protokoll: Ausführungstag 15 → 1.",
            Created with
            {
                EffectiveDate = new(2026, 11, 1),
                Shifted = false,
                ChangeLog = [("Ausführungstag", "15.", "1.")],
            }),

        Define("TC-22", "Wunschdatum genau zwölf Monate voraus", ["BR-14"],
            "Sparrate 200,00 €, Wunschdatum 08.10.2027", Input(amount: "200,00", requestedFrom: "2027-10-08"),
            "Auftrag angelegt, Wirksamkeitstermin 15.10.2027 (Grenzwert).",
            Created with { EffectiveDate = new(2027, 10, 15) }),

        Define("TC-23", "Wunschdatum mehr als zwölf Monate voraus", ["BR-14"],
            "Sparrate 200,00 €, Wunschdatum 09.10.2027", Input(amount: "200,00", requestedFrom: "2027-10-09"),
            "Abgelehnt, Meldung BR-14 (Grenzwert).", Rejected("BR-14")),

        Define("TC-24", "Keine Änderung", ["BR-15"],
            "keine Änderung", Input(),
            "Abgelehnt, Meldung BR-15 a.",
            Rejected("BR-15") with { MessagesContain = [RuleMessages.SameAsCurrentVersion] }),

        Define("TC-25", "Offenen Auftrag ersetzen", ["BR-08", "BR-16"],
            "Sparrate 300,00 €", Input(amount: "300,00"),
            "Auftrag CR-2026-000002 „fachlich geprüft“, ersetzt CR-2026-000001; dieser hat Status „ersetzt“, " +
            "Version 2 verworfen; Version 3 gültig ab 15.10.2026; Protokoll: Sparrate 150,00 € → 300,00 €.",
            Created with
            {
                RequestNumber = "CR-2026-000002",
                Status = "fachlich geprüft",
                ReplacesRequestNumber = "CR-2026-000001",
                ReplacedRequestStatus = "ersetzt",
                DiscardedVersionNo = 2,
                NewVersionNo = 3,
                NewVersionValidFrom = new(2026, 10, 15),
                ChangeLog = [("Sparrate", "150,00 €", "300,00 €")],
            },
            start: StartState.A1),

        Define("TC-26", "Sparrate nicht lesbar", ["BR-17"],
            "Sparrate „abc“", Input(amount: "abc"),
            "Abgelehnt, Meldung BR-17.",
            Rejected("BR-17") with { MessagesContain = [RuleMessages.AmountFormat] }),

        Define("TC-27", "Mehrere Fehler gleichzeitig", ["BR-01", "BR-04"],
            "Sparrate 20,00 €, INS-01 60 %, INS-02 30 %, INS-03 8 %",
            Input(amount: "20,00", allocations: [("INS-01", "60"), ("INS-02", "30"), ("INS-03", "8")]),
            "Abgelehnt, beide Meldungen (BR-01 und BR-04) werden gleichzeitig angezeigt.",
            Rejected("BR-01", "BR-04")),

        Define("TC-28", "Doppelte Einreichung des offenen Auftrags", ["BR-15"],
            "Sparrate 250,00 € (= offener Auftrag)", Input(amount: "250,00"),
            "Abgelehnt, Meldung BR-15 b mit Nummer CR-2026-000001.",
            Rejected("BR-15") with { MessagesContain = [RuleMessages.SameAsOpenRequest("CR-2026-000001")] },
            start: StartState.A1),

        Define("TC-29", "Rücknahme ohne Storno", ["BR-15"],
            "Sparrate 150,00 € (= gültige Version)", Input(amount: "150,00"),
            "Abgelehnt, Meldung BR-15 a. Dokumentierte Einschränkung von Version 1: Rücknahme erst mit Storno.",
            Rejected("BR-15") with { MessagesContain = [RuleMessages.SameAsCurrentVersion] },
            start: StartState.A1),

        Define("TC-30", "Annahmeschluss verpasst bei neuem Ausführungstag", ["BR-03", "BR-07"],
            "Ausführungstag 1, Wunschdatum 30.10.2026", Input(day: "1"),
            "Auftrag angelegt, Wirksamkeitstermin 01.12.2026; Verschiebungshinweis nennt 01.11.2026 in der allgemeinen Formulierung.",
            Created with
            {
                EffectiveDate = new(2026, 12, 1),
                Shifted = true,
                HintContains = "für den 01.11.2026 ein. Die Änderung wird daher erst zum 01.12.2026 wirksam; bis dahin",
            },
            referenceDate: new(2026, 10, 30)),
    ];

    public static TestScenario Get(string id) => All.Single(s => s.Id == id);

    private static TestScenario Define(
        string id,
        string title,
        string[] rules,
        string inputDescription,
        InputOverrides input,
        string expectedDescription,
        ScenarioExpectation expected,
        DateOnly? referenceDate = null,
        StartState start = StartState.A0)
    {
        var date = referenceDate ?? DefaultReferenceDate;
        return new TestScenario(id, title, rules, start, date, inputDescription, input.Build(date), expectedDescription, expected);
    }

    private static InputOverrides Input(
        string? amount = null,
        string? day = null,
        string? requestedFrom = null,
        (string InstrumentId, string Percentage)[]? allocations = null) =>
        new(amount, day, requestedFrom, allocations);

    /// <summary>Abweichungen vom Ausgangsstand A0; das Wunschdatum folgt sonst dem Referenzdatum.</summary>
    private sealed record InputOverrides(
        string? Amount,
        string? Day,
        string? RequestedFrom,
        (string InstrumentId, string Percentage)[]? Allocations)
    {
        public ChangeRequestInput Build(DateOnly referenceDate) => new(
            Amount ?? "150,00",
            Day ?? "15",
            RequestedFrom ?? referenceDate.ToString("yyyy-MM-dd"),
            Allocations is null
                ? [new AllocationInput("INS-01", "60"), new AllocationInput("INS-02", "40")]
                : Allocations.Select(a => new AllocationInput(a.InstrumentId, a.Percentage)).ToList());
    }
}
