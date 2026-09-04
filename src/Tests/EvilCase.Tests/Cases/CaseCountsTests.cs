using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseCountsTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task EachStatusIsCountedByTheDatabase()
    {
        await this.Tenant.AddCase(Day, "Aktivní 1", status: CaseStatus.Active);
        await this.Tenant.AddCase(Day, "Aktivní 2", status: CaseStatus.Active);
        await this.Tenant.AddCase(Day, "Čeká", status: CaseStatus.WaitingOnAuthority);
        await this.Tenant.AddCase(Day, "Uzavřená 1", status: CaseStatus.Closed);
        await this.Tenant.AddCase(Day, "Uzavřená 2", status: CaseStatus.Closed);
        await this.Tenant.AddCase(Day, "Uzavřená 3", status: CaseStatus.Closed);

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        var counts = await reader.CountCasesByStatus(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counts.Active, Is.EqualTo(2), "the counts come from the database, one number per status");
            Assert.That(counts.WaitingOnAuthority, Is.EqualTo(1), "the counts come from the database, one number per status");
            Assert.That(counts.Closed, Is.EqualTo(3), "the counts come from the database, one number per status");
            Assert.That(counts.Total, Is.EqualTo(6), "the counts come from the database, one number per status");
        }
    }

    [Test]
    public async Task AnEmptyTenantCountsZeroInEveryStatus()
    {
        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        var counts = await reader.CountCasesByStatus(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counts.Active, Is.Zero, "an empty tenant answers zero in every status, which is what puts the dashboard into its empty state");
            Assert.That(counts.WaitingOnAuthority, Is.Zero, "an empty tenant answers zero in every status, which is what puts the dashboard into its empty state");
            Assert.That(counts.Closed, Is.Zero, "an empty tenant answers zero in every status, which is what puts the dashboard into its empty state");
            Assert.That(counts.Total, Is.Zero, "an empty tenant answers zero in every status, which is what puts the dashboard into its empty state");
        }
    }

    [Test]
    public async Task TheCountsIgnoreWhatTheListNarrowsTo()
    {
        await this.Tenant.AddCase(Day, "Aktivní", status: CaseStatus.Active);
        await this.Tenant.AddCase(Day, "Uzavřená", status: CaseStatus.Closed);

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        var listed = await reader.ListCases(new CaseListRequest { Status = CaseStatusFilter.Active }, CancellationToken.None);
        var counts = await reader.CountCasesByStatus(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(listed, Has.Count.EqualTo(1), "the list narrows to what the request asks for");
            Assert.That(counts.Closed, Is.EqualTo(1), "the counts cover the whole tenant, whatever the list request narrows to");
        }
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNeverCounted()
    {
        await this.Tenant.AddCase(Day, "Moje věc", status: CaseStatus.Active);

        await using (var other = await TestTenant.Create())
        {
            await other.AddCase(Day, "Cizí 1", status: CaseStatus.Active);
            await other.AddCase(Day, "Cizí 2", status: CaseStatus.WaitingOnAuthority);
            await other.AddCase(Day, "Cizí 3", status: CaseStatus.Closed);
        }

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        var counts = await reader.CountCasesByStatus(CancellationToken.None);

        Assert.That(counts.Total, Is.EqualTo(1), "the tenant query filter is what keeps another tenant's cases out of the counts");
    }
}
