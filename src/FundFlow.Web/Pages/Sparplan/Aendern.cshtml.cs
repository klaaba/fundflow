using FundFlow.Domain.Model;
using FundFlow.Domain.Orders;
using FundFlow.Domain.Rules;
using FundFlow.Infrastructure.Orders;
using FundFlow.Infrastructure.Seed;
using FundFlow.Web.Demo;
using Microsoft.AspNetCore.Mvc;

namespace FundFlow.Web.Pages.Sparplan;

/// <summary>
/// Änderung erfassen und bestätigen (Fachkonzept, Abschnitt 7, Schritte 2–8).
/// Ansicht „Erfassen“ und „Prüfen und bestätigen“ teilen sich eine Seite; die Werte reisen als Formularfelder mit.
/// </summary>
public class AendernModel(ChangeRequestService orders) : DemoPageModel
{
    public const int MaxRows = SavingsPlanLimits.MaxAllocations + 1; // eine Zeile mehr, damit BR-11 erlebbar bleibt

    public enum ViewMode
    {
        Edit,
        Review,
    }

    [BindProperty]
    public ChangeForm Form { get; set; } = new();

    /// <summary>Wirksamkeitstermin aus der angezeigten Zusammenfassung im ISO-Format (Prozessschritt 6).</summary>
    [BindProperty]
    public string? ConfirmedEffectiveDate { get; set; }

    public ViewMode Mode { get; private set; } = ViewMode.Edit;
    public SavingsPlanState State { get; private set; } = null!;
    public IReadOnlyList<ValidationIssue> Issues { get; private set; } = [];
    public PreparedChange? Prepared { get; private set; }

    /// <summary>Hinweis, dass sich der Wirksamkeitstermin seit der Zusammenfassung geändert hat.</summary>
    public bool EffectiveDateChanged { get; private set; }

    public static IReadOnlyList<Instrument> EligibleInstruments { get; } =
        DemoData.Instruments.Where(i => i.SavingsPlanEligible).ToList();

    public IReadOnlyList<int> ExecutionDays { get; } = [.. SavingsPlanLimits.AllowedExecutionDays.Order()];

    public string? OpenRequestNumber => State.OpenRequest?.RequestNumber;

    /// <summary>Schritt 2: Startwerte aus dem offenen Auftrag, sonst aus der gültigen Version.</summary>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadStateAsync(cancellationToken);
        var start = State.OpenRequest?.ResultingVersion?.ToTerms() ?? State.CurrentVersion.ToTerms();
        Form = ChangeForm.From(start, Today);
    }

    /// <summary>Schritte 4 und 5: prüfen und Zusammenfassung zeigen.</summary>
    public async Task<IActionResult> OnPostCheckAsync(CancellationToken cancellationToken)
    {
        var planId = await LoadStateAsync(cancellationToken);
        Form.RemoveEmptyRows();

        var result = await orders.PrepareAsync(planId, Form.ToInput(), cancellationToken);
        if (result.Prepared is null)
        {
            Issues = result.Issues;
            return Page();
        }

        ShowReview(result.Prepared);
        return Page();
    }

    /// <summary>Schritte 6 und 7: erneut prüfen und Auftrag anlegen.</summary>
    public async Task<IActionResult> OnPostSubmitAsync(CancellationToken cancellationToken)
    {
        var planId = await LoadStateAsync(cancellationToken);
        Form.RemoveEmptyRows();

        DateOnly? confirmed = DateOnly.TryParseExact(ConfirmedEffectiveDate, "yyyy-MM-dd", out var date) ? date : null;
        var result = await orders.SubmitAsync(planId, Form.ToInput(), confirmed, cancellationToken);
        switch (result.Outcome)
        {
            case SubmitOutcome.Created:
                TempData[Auftraege.DetailModel.CreatedKey] = true;
                TempData[Auftraege.DetailModel.ShiftHintKey] = result.Prepared?.Schedule.ShiftHint;
                return RedirectToPage("/Auftraege/Detail", new { number = result.Request!.RequestNumber });

            case SubmitOutcome.EffectiveDateChanged:
                EffectiveDateChanged = true;
                ShowReview(result.Prepared!);
                return Page();

            default:
                Issues = result.Issues;
                return Page();
        }
    }

    public async Task<IActionResult> OnPostEditAsync(CancellationToken cancellationToken)
    {
        await LoadStateAsync(cancellationToken);
        return Page();
    }

    /// <summary>„Fonds hinzufügen“ ohne JavaScript.</summary>
    public async Task<IActionResult> OnPostAddRowAsync(CancellationToken cancellationToken)
    {
        await LoadStateAsync(cancellationToken);
        if (Form.Allocations.Count < MaxRows)
        {
            Form.Allocations.Add(new AllocationRow());
        }

        return Page();
    }

    /// <summary>„Entfernen“ ohne JavaScript.</summary>
    public async Task<IActionResult> OnPostRemoveRowAsync(int index, CancellationToken cancellationToken)
    {
        await LoadStateAsync(cancellationToken);
        if (index >= 0 && index < Form.Allocations.Count)
        {
            Form.Allocations.RemoveAt(index);
        }

        return Page();
    }

    public IEnumerable<ValidationIssue> IssuesFor(string field) => Issues.Where(i => i.Field == field);

    public bool HasIssue(string field) => Issues.Any(i => i.Field == field);

    /// <summary>Summe der lesbaren Anteile für die Anzeige „Summe“ (US-02, Kriterium 3).</summary>
    public decimal? CurrentSum()
    {
        decimal sum = 0;
        foreach (var row in Form.Allocations)
        {
            if (string.IsNullOrWhiteSpace(row.Percentage))
            {
                continue;
            }

            if (!InputParser.TryParsePercentage(row.Percentage, out var value))
            {
                return null;
            }

            sum += value;
        }

        return sum;
    }

    private void ShowReview(PreparedChange prepared)
    {
        Prepared = prepared;
        ConfirmedEffectiveDate = prepared.Schedule.EffectiveDate.ToString("yyyy-MM-dd");
        Mode = ViewMode.Review;
    }

    private async Task<int> LoadStateAsync(CancellationToken cancellationToken)
    {
        var planId = await orders.GetDemoPlanIdAsync(cancellationToken);
        State = await orders.LoadStateAsync(planId, cancellationToken);
        return planId;
    }
}
