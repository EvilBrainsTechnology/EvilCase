using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActDetailQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheDetailNamesTheCaseTheActSitsIn()
    {
        var @case = await this.Tenant.AddCase(Day, status: CaseStatus.WaitingOnAuthority);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.CaseId, Is.EqualTo(@case.Id), "the detail carries the case id the screens build their links from");
            Assert.That(detail.CaseNumber, Is.EqualTo(@case.CaseNumber), "the detail carries the case number the link back to the case reads");
            Assert.That(detail.CaseStatus, Is.EqualTo(CaseStatus.WaitingOnAuthority), "the sidebar's Spis link reads the case status");
        }
    }

    [Test]
    public async Task TheDetailNamesTheCasesContactEvenWhereTheActNamesNone()
    {
        var caseContact = await this.Tenant.AddContact("Městský úřad Vzorov");
        var @case = await this.Tenant.AddCase(Day, contact: caseContact);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.Contact, Is.Null);
            Assert.That(detail.CaseContact!.ContactId, Is.EqualTo(caseContact.Id), "the warning on the act is built from the case's contact");
        }
    }

    [Test]
    public async Task AnUnknownIdIsNoDetail()
    {
        var detail = await this.Tenant.Context.Acts.DetailOf(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task AnActOfAnotherTenantIsNoDetail()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(otherAct.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "the tenant query filter is what turns another tenant's id into nothing");
    }
}
