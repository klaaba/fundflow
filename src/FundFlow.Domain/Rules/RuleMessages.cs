using System.Globalization;

namespace FundFlow.Domain.Rules;

/// <summary>Meldungstexte gemäß Fachkonzept, Abschnitt 8.2.</summary>
public static class RuleMessages
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public const string MinMonthlyAmount =
        "Bitte geben Sie eine monatliche Sparrate von mindestens 25,00 € ein.";

    public const string MaxDecimalPlaces =
        "Die Sparrate darf höchstens zwei Nachkommastellen enthalten.";

    public const string ExecutionDay =
        "Bitte wählen Sie einen gültigen Ausführungstag.";

    public static string AllocationSum(decimal currentSum) =>
        $"Die Fondsaufteilung ergibt aktuell {currentSum.ToString("0.##", German)} %. " +
        "Bitte passen Sie die Anteile auf insgesamt 100 % an.";

    public const string PositivePercentage =
        "Bitte entfernen Sie Fonds ohne Anteil oder geben Sie einen Anteil größer als 0 % ein.";

    public const string RequestedFromNotPast =
        "Das Wunschdatum muss heute oder später liegen.";

    /// <summary>
    /// BR-07: Hinweis bei Verschiebung. Wechselt der Ausführungstag, findet am verpassten Termin
    /// keine Ausführung nach altem Plan statt – dann gilt die allgemeine Formulierung (Fachkonzept 8.3).
    /// </summary>
    public static string EffectiveDateShifted(DateOnly missedDate, DateOnly effectiveDate, bool executionDayChanged)
    {
        var missed = missedDate.ToString("dd.MM.yyyy", German);
        var effective = effectiveDate.ToString("dd.MM.yyyy", German);
        var lead = $"Ihr Auftrag geht nach dem Annahmeschluss für den {missed} ein. " +
                   $"Die Änderung wird daher erst zum {effective} wirksam; ";

        return executionDayChanged
            ? lead + "bis dahin wird Ihr Sparplan zu den bisherigen Konditionen ausgeführt."
            : lead + $"die Ausführung am {missed} erfolgt noch zu den bisherigen Konditionen.";
    }

    public const string MaxMonthlyAmount =
        "Die monatliche Sparrate darf höchstens 10.000,00 € betragen.";

    public const string WholePercentage =
        "Bitte geben Sie die Anteile in ganzen Prozent an.";

    public const string AllocationCount =
        "Ein Sparplan muss 1 bis 5 Fonds enthalten.";

    public const string NoDuplicateInstrument =
        "Jeder Fonds darf nur einmal im Sparplan enthalten sein.";

    public static string InstrumentNotEligible(string instrumentName) =>
        $"Der Fonds ‚{instrumentName}‘ ist derzeit nicht sparplanfähig.";

    public const string InstrumentUnknown =
        "Der gewählte Fonds ist nicht verfügbar.";

    public const string RequestedFromHorizon =
        "Das Wunschdatum darf höchstens zwölf Monate in der Zukunft liegen.";

    public const string SameAsCurrentVersion =
        "Ihre Eingaben entsprechen dem aktuell gültigen Sparplan. Bitte ändern Sie mindestens eine Angabe.";

    public static string SameAsOpenRequest(string requestNumber) =>
        $"Diese Änderung liegt bereits als offener Auftrag {requestNumber} vor.";

    /// <summary>BR-16 und E-09: Hinweis auf Sparplanseite, Formular und Zusammenfassung.</summary>
    public static string ReplacesOpenRequest(string requestNumber) =>
        $"Dieser Auftrag ersetzt den offenen Auftrag {requestNumber}. " +
        "Eine separate Stornierung ist in dieser Demo-Version nicht möglich.";

    public const string AmountFormat =
        "Bitte geben Sie die Sparrate als Betrag ein, z. B. 150,00.";

    public const string DateFormat =
        "Bitte geben Sie ein gültiges Datum ein.";
}
