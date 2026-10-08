using FundFlow.Domain.Orders;

namespace FundFlow.Domain.Model;

// Kernobjekte gemäß Fachkonzept 10.1. Die Zuordnung zur Demo-Sitzung ist rein technisch
// und wird in der Datenhaltung ergänzt – sie ist kein Teil des fachlichen Modells.

public class Customer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<Portfolio> Portfolios { get; set; } = [];
}

public class Portfolio
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public required string DepotNumber { get; set; }
    public required string Status { get; set; }
    public List<SavingsPlan> SavingsPlans { get; set; } = [];
}

public class SavingsPlan
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public required string PlanNumber { get; set; }
    public required string Status { get; set; }
    public List<SavingsPlanVersion> Versions { get; set; } = [];
    public List<ChangeRequest> ChangeRequests { get; set; } = [];
}

/// <summary>Konditionen eines Sparplans in einem Zeitraum. Versionen werden nie gelöscht, sondern verworfen.</summary>
public class SavingsPlanVersion
{
    public int Id { get; set; }
    public int SavingsPlanId { get; set; }
    public int VersionNo { get; set; }
    public decimal MonthlyAmount { get; set; }
    public int ExecutionDay { get; set; }
    public DateOnly ValidFrom { get; set; }

    /// <summary>Letzter Gültigkeitstag; <c>null</c> = unbefristet.</summary>
    public DateOnly? ValidTo { get; set; }

    public bool IsDiscarded { get; set; }
    public List<Allocation> Allocations { get; set; } = [];

    /// <summary>Auftrag, der diese Version erzeugt hat; <c>null</c> bei Erstanlage.</summary>
    public ChangeRequest? CreatedByRequest { get; set; }

    public bool IsValidOn(DateOnly date) =>
        !IsDiscarded && ValidFrom <= date && (ValidTo is null || ValidTo >= date);

    public SavingsPlanTerms ToTerms() => new(
        MonthlyAmount,
        ExecutionDay,
        Allocations.Select(a => new AllocationLine(a.InstrumentId, a.Percentage)).ToList());
}

public class Allocation
{
    public int Id { get; set; }
    public int VersionId { get; set; }
    public required string InstrumentId { get; set; }
    public int Percentage { get; set; }
}

/// <summary>Änderungsauftrag. Statuswechsel nur über <see cref="ChangeStatus"/>, damit jeder Wechsel protokolliert wird.</summary>
public class ChangeRequest
{
    public int Id { get; set; }
    public required string RequestNumber { get; set; }
    public int SavingsPlanId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ChangeRequestStatus Status { get; private set; }
    public DateOnly RequestedFrom { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public bool EffectiveDateShifted { get; set; }

    public int? ReplacesRequestId { get; set; }
    public ChangeRequest? ReplacesRequest { get; set; }

    public int ResultingVersionId { get; set; }
    public SavingsPlanVersion? ResultingVersion { get; set; }

    public List<ChangeRequestStatusHistory> StatusHistory { get; set; } = [];
    public List<ChangeLogEntry> ChangeLog { get; set; } = [];

    /// <summary>Erster Status bei Anlage (BR-08, Schritt 5).</summary>
    public void Open(DateTimeOffset at, string reason)
    {
        if (StatusHistory.Count > 0)
        {
            throw new InvalidOperationException($"Auftrag {RequestNumber} wurde bereits angelegt.");
        }

        Status = ChangeRequestStatus.BusinessValidated;
        StatusHistory.Add(new ChangeRequestStatusHistory
        {
            FromStatus = null,
            ToStatus = Status,
            ChangedAt = at,
            Reason = reason,
        });
    }

    /// <summary>Statuswechsel gemäß Statusmodell (Fachkonzept, Abschnitt 9).</summary>
    public void ChangeStatus(ChangeRequestStatus to, DateTimeOffset at, string reason)
    {
        if (!ChangeRequestStatusModel.IsAllowed(Status, to))
        {
            throw new InvalidOperationException(
                $"Statuswechsel von „{ChangeRequestStatusModel.Label(Status)}“ nach " +
                $"„{ChangeRequestStatusModel.Label(to)}“ ist nicht zulässig.");
        }

        StatusHistory.Add(new ChangeRequestStatusHistory
        {
            FromStatus = Status,
            ToStatus = to,
            ChangedAt = at,
            Reason = reason,
        });
        Status = to;
    }

    /// <summary>Offener Auftrag: fachlich geprüft und Wirksamkeitstermin noch nicht erreicht (Glossar).</summary>
    public bool IsOpenOn(DateOnly date) =>
        Status == ChangeRequestStatus.BusinessValidated && EffectiveDate > date;
}

public class ChangeRequestStatusHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public ChangeRequestStatus? FromStatus { get; set; }
    public ChangeRequestStatus ToStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public required string Reason { get; set; }
}

public class ChangeLogEntry
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public required string FieldName { get; set; }
    public required string OldValue { get; set; }
    public required string NewValue { get; set; }
}
