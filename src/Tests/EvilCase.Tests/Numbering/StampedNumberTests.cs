using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// A deleted row is stamped, not removed, so the unique index still holds its number. The issuers read
/// past the filter to see it. Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class StampedNumberTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    [Test]
    public async Task ADeletedCaseKeepsItsNumber()
    {
        var session = new FixedDbSession(this.Tenant.Context);
        var writer = new CaseWriter(session, new FakeCaseNumberIssuer(), NullLogger<CaseWriter>.Instance);
        var issuer = new CaseNumberIssuer(session);

        var seeded = await this.Tenant.AddCase(Day);

        await writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var next = await issuer.NextCaseNumber(Day, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seeded.CaseNumber, Is.EqualTo(CaseNumberFormat.Compose(Day, 1)));
            Assert.That(next, Is.EqualTo(CaseNumberFormat.Compose(Day, 2)), "the index still holds the deleted case's number, so issuing it again would refuse the write");
        }
    }

    [Test]
    public async Task ADeletedActKeepsItsNumber()
    {
        var session = new FixedDbSession(this.Tenant.Context);
        var writer = new ActWriter(session, new FakeActNumberIssuer(), NullLogger<ActWriter>.Instance);
        var issuer = new ActNumberIssuer(session);

        var @case = await this.Tenant.AddCase(Day);
        var seeded = await this.Tenant.AddAct(@case, Day);

        await writer.DeleteAct(@case.Id, seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var next = await issuer.NextActNumber(@case, Day, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(seeded.ActNumber, Is.EqualTo(ActNumberFormat.Compose(@case.CaseNumber, Day, 1)));
            Assert.That(next, Is.EqualTo(ActNumberFormat.Compose(@case.CaseNumber, Day, 2)), "the index still holds the deleted act's number, so issuing it again would refuse the write");
        }
    }
}
