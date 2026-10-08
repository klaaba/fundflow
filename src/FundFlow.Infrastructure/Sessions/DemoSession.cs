namespace FundFlow.Infrastructure.Sessions;

/// <summary>Technisches Objekt für den Demo-Betrieb (Fachkonzept, Abschnitt 14) – kein Teil des fachlichen Modells.</summary>
public class DemoSession
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}

/// <summary>Liefert die Demo-Sitzung der aktuellen Anfrage.</summary>
public interface IDemoSessionAccessor
{
    Guid SessionId { get; }
}

/// <summary>Feste Sitzung, z. B. für Tests oder Hintergrundaufgaben.</summary>
public sealed class FixedDemoSessionAccessor(Guid sessionId) : IDemoSessionAccessor
{
    public Guid SessionId { get; } = sessionId;
}
