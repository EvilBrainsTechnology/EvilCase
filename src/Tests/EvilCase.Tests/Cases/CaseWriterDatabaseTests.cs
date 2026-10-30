using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseWriterDatabaseTests : TenantFixture
{
    protected override bool AsHost => true;

    [Test]
    public async Task ANumberTakenWhileTheCaseIsFiledIsIssuedAgain()
    {
        const string taken = "EC/20260821-001";
        const string free = "EC/20260821-002";

        await this.Tenant.AddCase(new DateOnly(2026, 8, 21), caseNumber: taken);

        var writer = new CaseWriter(new FixedDbSession(this.Tenant.Context), new QueuedCaseNumberIssuer([taken, free]), NullLogger<CaseWriter>.Instance);

        var result = await writer.CreateCase(new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek" }, CancellationToken.None);

        var cases = await this.Tenant.Context.Cases.ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(CaseCreateOutcome.Created));
            Assert.That(result.Case?.CaseNumber, Is.EqualTo(free), "the loser of the race files under the next free number");
            Assert.That(cases, Has.Count.EqualTo(2), "the row that lost the race is not written twice");
        }
    }

    [Test]
    public void AFailureThatIsNotATakenNumberReachesTheCaller()
    {
        var tooLong = new string('9', 65);
        var writer = new CaseWriter(new FixedDbSession(this.Tenant.Context), new QueuedCaseNumberIssuer([tooLong]), NullLogger<CaseWriter>.Instance);

        Assert.That(
            async () => await writer.CreateCase(new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek" }, CancellationToken.None),
            Throws.InstanceOf<DbUpdateException>(),
            "only a taken number is issued again");
    }

    private sealed class QueuedCaseNumberIssuer(IReadOnlyList<string> caseNumbers) : ICaseNumberIssuer
    {
        private int issued;

        public async Task<string> NextCaseNumber(DateOnly date, CancellationToken token)
        {
            return caseNumbers[this.issued++];
        }
    }
}
