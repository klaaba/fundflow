using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using FundFlow.Domain.Model;
using FundFlow.Domain.Orders;
using FundFlow.Domain.Scheduling;
using FundFlow.Infrastructure.Seed;

namespace FundFlow.Web.Demo;

/// <summary>Einheitliche deutsche Darstellung von Datum, Zeit, Beträgen und Status in der Oberfläche.</summary>
public static partial class Display
{
    [GeneratedRegex(@"\b(?:CR-\d{4}-\d{6}|(?:INS|BR|TC|SP|DEMO)-\d+|XXDEMO\d+)\b")]
    private static partial Regex IdentifierPattern();

    /// <summary>
    /// Text mit Kennungen wie CR-2026-000001 oder INS-02, die nicht am Bindestrich umbrechen sollen.
    /// Der Text wird kodiert; Kennungen werden in <c>span.nowrap</c> gefasst.
    /// </summary>
    public static Microsoft.AspNetCore.Html.HtmlString Text(string text, HtmlEncoder encoder)
    {
        var html = new System.Text.StringBuilder();
        var last = 0;
        foreach (Match match in IdentifierPattern().Matches(text))
        {
            html.Append(encoder.Encode(text[last..match.Index]));
            html.Append("<span class=\"nowrap\">").Append(encoder.Encode(match.Value)).Append("</span>");
            last = match.Index + match.Length;
        }

        html.Append(encoder.Encode(text[last..]));
        return new Microsoft.AspNetCore.Html.HtmlString(html.ToString());
    }

    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", German);

    public static string DateTime(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, BusinessCalendar.BerlinTimeZone).ToString("dd.MM.yyyy, HH:mm 'Uhr'", German);

    public static string Amount(decimal amount) => ChangeLogBuilder.FormatAmount(amount);

    public static string AmountInput(decimal amount) => amount.ToString("N2", German);

    public static string Status(ChangeRequestStatus status) => ChangeRequestStatusModel.Label(status);

    public static string InstrumentName(string instrumentId) =>
        DemoData.InstrumentsById.TryGetValue(instrumentId, out var instrument) ? instrument.Name : instrumentId;

    public static string IsinLike(string instrumentId) =>
        DemoData.InstrumentsById.TryGetValue(instrumentId, out var instrument) ? instrument.IsinLikeId : "";

    /// <summary>Stand einer Sparplan-Version aus Sicht des heutigen Tages.</summary>
    public static VersionState StateOf(SavingsPlanVersion version, DateOnly today) => version switch
    {
        { IsDiscarded: true } => VersionState.Discarded,
        _ when version.IsValidOn(today) => VersionState.Current,
        _ when version.ValidFrom > today => VersionState.Planned,
        _ => VersionState.Expired,
    };

    public static string Label(VersionState state) => state switch
    {
        VersionState.Current => "gültig",
        VersionState.Planned => "geplant",
        VersionState.Discarded => "verworfen",
        VersionState.Expired => "abgelaufen",
        _ => state.ToString(),
    };
}

public enum VersionState
{
    Current,
    Planned,
    Discarded,
    Expired,
}
