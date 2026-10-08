namespace FundFlow.Domain.Orders;

/// <summary>Status eines Änderungsauftrags (Fachkonzept, Abschnitt 9).</summary>
public enum ChangeRequestStatus
{
    /// <summary>fachlich geprüft – in Version 1 umgesetzt.</summary>
    BusinessValidated,

    /// <summary>ersetzt (BR-16) – in Version 1 umgesetzt.</summary>
    Replaced,

    /// <summary>storniert – nur modelliert (E-09).</summary>
    Cancelled,

    /// <summary>an Kernsystem übergeben – nur modelliert.</summary>
    TransferredToCoreSystem,

    /// <summary>wirksam – nur modelliert.</summary>
    Effective,

    /// <summary>vom Kernsystem abgelehnt – nur modelliert.</summary>
    RejectedByCoreSystem,
}

/// <summary>Zulässige Statusübergänge und deutsche Bezeichnungen (Fachkonzept, Abschnitt 9).</summary>
public static class ChangeRequestStatusModel
{
    private static readonly IReadOnlyDictionary<ChangeRequestStatus, ChangeRequestStatus[]> Transitions =
        new Dictionary<ChangeRequestStatus, ChangeRequestStatus[]>
        {
            [ChangeRequestStatus.BusinessValidated] =
            [
                ChangeRequestStatus.Replaced,
                ChangeRequestStatus.Cancelled,
                ChangeRequestStatus.TransferredToCoreSystem,
            ],
            [ChangeRequestStatus.TransferredToCoreSystem] =
            [
                ChangeRequestStatus.Effective,
                ChangeRequestStatus.RejectedByCoreSystem,
            ],
        };

    public static bool IsAllowed(ChangeRequestStatus from, ChangeRequestStatus to) =>
        Transitions.TryGetValue(from, out var targets) && targets.Contains(to);

    public static bool IsFinal(ChangeRequestStatus status) => !Transitions.ContainsKey(status);

    public static string Label(ChangeRequestStatus status) => status switch
    {
        ChangeRequestStatus.BusinessValidated => "fachlich geprüft",
        ChangeRequestStatus.Replaced => "ersetzt",
        ChangeRequestStatus.Cancelled => "storniert",
        ChangeRequestStatus.TransferredToCoreSystem => "an Kernsystem übergeben",
        ChangeRequestStatus.Effective => "wirksam",
        ChangeRequestStatus.RejectedByCoreSystem => "vom Kernsystem abgelehnt",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
