using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseWriterTests : TenantFixture
{
    protected override bool AsHost => true;

    [Test]
    public void ABlankDescriptionIsFiledAsNothing()
    {
        var blank = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "   " };
        var withText = blank with { Description = "text" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseWriter.BuildCase(blank, "EC/20260821-001").Description, Is.Null);
            Assert.That(CaseWriter.BuildCase(withText, "EC/20260821-001").Description, Is.EqualTo("text"));
        }
    }

    [Test]
    public void ATitleAndDescriptionAreStoredTrimmed()
    {
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "  Přestupek  ", Description = "  text  " };

        var @case = CaseWriter.BuildCase(request, "EC/20260821-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.Title, Is.EqualTo("Přestupek"), "a created case stores its title trimmed");
            Assert.That(@case.Description, Is.EqualTo("text"), "a created case stores its description trimmed");
        }
    }

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

    private sealed class QueuedCaseNumberIssuer(IReadOnlyList<string> caseNumbers) : ICaseNumberIssuer
    {
        private int issued;

        public async Task<string> NextCaseNumber(DateOnly date, CancellationToken token)
        {
            return caseNumbers[this.issued++];
        }
    }
}
