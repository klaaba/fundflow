using FundFlow.Domain.Model;
using FundFlow.Domain.Orders;

namespace FundFlow.Tests.Domain;

/// <summary>Statusmodell (Fachkonzept, Abschnitt 9).</summary>
public class ChangeRequestStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ChangeRequestStatus.BusinessValidated, ChangeRequestStatus.Replaced)]
    [InlineData(ChangeRequestStatus.BusinessValidated, ChangeRequestStatus.Cancelled)]
    [InlineData(ChangeRequestStatus.BusinessValidated, ChangeRequestStatus.TransferredToCoreSystem)]
    [InlineData(ChangeRequestStatus.TransferredToCoreSystem, ChangeRequestStatus.Effective)]
    [InlineData(ChangeRequestStatus.TransferredToCoreSystem, ChangeRequestStatus.RejectedByCoreSystem)]
    public void Zulaessige_Uebergaenge(ChangeRequestStatus from, ChangeRequestStatus to)
    {
        Assert.True(ChangeRequestStatusModel.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(ChangeRequestStatus.Replaced, ChangeRequestStatus.BusinessValidated)]
    [InlineData(ChangeRequestStatus.Replaced, ChangeRequestStatus.Cancelled)]
    [InlineData(ChangeRequestStatus.TransferredToCoreSystem, ChangeRequestStatus.Replaced)] // nach Übergabe nicht mehr ersetzbar
    [InlineData(ChangeRequestStatus.TransferredToCoreSystem, ChangeRequestStatus.Cancelled)]
    [InlineData(ChangeRequestStatus.BusinessValidated, ChangeRequestStatus.Effective)]
    [InlineData(ChangeRequestStatus.Effective, ChangeRequestStatus.Replaced)]
    public void Unzulaessige_Uebergaenge(ChangeRequestStatus from, ChangeRequestStatus to)
    {
        Assert.False(ChangeRequestStatusModel.IsAllowed(from, to));
    }

    [Theory]
    [InlineData(ChangeRequestStatus.Replaced)]
    [InlineData(ChangeRequestStatus.Cancelled)]
    [InlineData(ChangeRequestStatus.Effective)]
    [InlineData(ChangeRequestStatus.RejectedByCoreSystem)]
    public void Endstatus(ChangeRequestStatus status)
    {
        Assert.True(ChangeRequestStatusModel.IsFinal(status));
    }

    [Fact]
    public void Jeder_Status_hat_eine_deutsche_Bezeichnung()
    {
        Assert.All(Enum.GetValues<ChangeRequestStatus>(), s => Assert.False(string.IsNullOrWhiteSpace(ChangeRequestStatusModel.Label(s))));
        Assert.Equal("fachlich geprüft", ChangeRequestStatusModel.Label(ChangeRequestStatus.BusinessValidated));
    }

    [Fact]
    public void Statuswechsel_wird_im_Verlauf_festgehalten()
    {
        var request = NewRequest();
        request.Open(Now, "angelegt");

        request.ChangeStatus(ChangeRequestStatus.Replaced, Now.AddMinutes(5), "Ersetzt durch Auftrag CR-2026-000002");

        Assert.Equal(ChangeRequestStatus.Replaced, request.Status);
        Assert.Equal(
            [(null, ChangeRequestStatus.BusinessValidated), (ChangeRequestStatus.BusinessValidated, ChangeRequestStatus.Replaced)],
            request.StatusHistory.Select(h => (h.FromStatus, h.ToStatus)));
    }

    [Fact]
    public void Unzulaessiger_Statuswechsel_wird_verhindert_und_nicht_protokolliert()
    {
        var request = NewRequest();
        request.Open(Now, "angelegt");
        request.ChangeStatus(ChangeRequestStatus.Replaced, Now, "ersetzt");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            request.ChangeStatus(ChangeRequestStatus.Cancelled, Now, "storniert"));

        Assert.Contains("„ersetzt“ nach „storniert“", ex.Message);
        Assert.Equal(2, request.StatusHistory.Count);
    }

    [Fact]
    public void Auftrag_kann_nur_einmal_angelegt_werden()
    {
        var request = NewRequest();
        request.Open(Now, "angelegt");

        Assert.Throws<InvalidOperationException>(() => request.Open(Now, "nochmal"));
    }

    [Fact]
    public void Offen_ist_ein_fachlich_gepruefter_Auftrag_bis_zum_Tag_vor_dem_Wirksamkeitstermin()
    {
        var request = NewRequest();
        request.EffectiveDate = new DateOnly(2026, 10, 15);
        request.Open(Now, "angelegt");

        Assert.True(request.IsOpenOn(new DateOnly(2026, 10, 14)));
        Assert.False(request.IsOpenOn(new DateOnly(2026, 10, 15)));

        request.ChangeStatus(ChangeRequestStatus.Replaced, Now, "ersetzt");
        Assert.False(request.IsOpenOn(new DateOnly(2026, 10, 14)));
    }

    private static ChangeRequest NewRequest() => new() { RequestNumber = "CR-2026-000001" };
}
