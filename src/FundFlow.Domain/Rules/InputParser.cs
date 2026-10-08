using System.Globalization;
using System.Text.RegularExpressions;

namespace FundFlow.Domain.Rules;

/// <summary>
/// Liest Beträge, Prozentwerte und Datumsangaben im deutschen Format (BR-17).
/// Bewusst strenger als <c>decimal.Parse</c>: „250.50“ wird nicht als 25.050 gelesen,
/// sondern als Formatfehler gemeldet.
/// </summary>
public static partial class InputParser
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly string[] DateFormats = ["yyyy-MM-dd", "dd.MM.yyyy", "d.M.yyyy"];

    // Optionales Minus, Ganzzahlteil mit oder ohne Tausenderpunkte, optional Komma mit Nachkommastellen.
    [GeneratedRegex(@"^-?(\d{1,3}(\.\d{3})+|\d{1,9})(,(?<fraction>\d+))?$")]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"^-?\d{1,9}(,\d+)?$")]
    private static partial Regex PercentagePattern();

    /// <summary>Liest einen Betrag wie „1.234,56“, „250“ oder „250,00 €“.</summary>
    /// <param name="decimalPlaces">Anzahl der eingegebenen Nachkommastellen (für BR-02).</param>
    public static bool TryParseAmount(string? text, out decimal amount, out int decimalPlaces)
    {
        amount = 0;
        decimalPlaces = 0;

        var normalized = text?.Trim().TrimEnd('€').TrimEnd();
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        var match = AmountPattern().Match(normalized);
        if (!match.Success)
        {
            return false;
        }

        decimalPlaces = match.Groups["fraction"].Length;
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint,
            German,
            out amount);
    }

    /// <summary>Liest einen Prozentwert wie „60“, „33,5“ oder „60 %“.</summary>
    public static bool TryParsePercentage(string? text, out decimal percentage)
    {
        percentage = 0;

        var normalized = text?.Trim().TrimEnd('%').TrimEnd();
        if (string.IsNullOrEmpty(normalized) || !PercentagePattern().IsMatch(normalized))
        {
            return false;
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            German,
            out percentage);
    }

    /// <summary>Liest ein Datum aus dem Datumsfeld (ISO) oder in deutscher Schreibweise.</summary>
    public static bool TryParseDate(string? text, out DateOnly date) =>
        DateOnly.TryParseExact(
            text?.Trim(),
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    /// <summary>Liest einen Ausführungstag als ganze Zahl.</summary>
    public static bool TryParseDay(string? text, out int day) =>
        int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out day);
}
