using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseWriterTests
{
    [Test]
    public void ANewCaseIsActiveAndHangsUnderNothing()
    {
        var request = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = null };

        var @case = CaseWriter.Build(request, "EC/20260821-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.Status, Is.EqualTo(CaseStatus.Active));
            Assert.That(@case.ParentCaseId, Is.Null);
            Assert.That(@case.CaseNumber, Is.EqualTo("EC/20260821-001"));
            Assert.That(@case.Date, Is.EqualTo(request.Date));
            Assert.That(@case.Title, Is.EqualTo(request.Title));
        }
    }

    [Test]
    public void ABlankDescriptionIsFiledAsNothing()
    {
        var blank = new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek", Description = "   " };
        var withText = blank with { Description = "text" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(CaseWriter.Build(blank, "EC/20260821-001").Description, Is.Null);
            Assert.That(CaseWriter.Build(withText, "EC/20260821-001").Description, Is.EqualTo("text"));
        }
    }

    [Test]
    public async Task ANumberTakenWhileTheCaseIsFiledIsIssuedAgain()
    {
        var userContext = new StubUserContext();
        using var entered = userContext.Enter(Guid.CreateVersion7(), Guid.CreateVersion7());
        await using var context = FakeApplicationDbContext.Create(userContext);
        context.FailNextSave = new DbUpdateException(
            "duplicate key",
            new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        var numbers = new QueuedCaseNumberIssuer(["EC/20260821-001", "EC/20260821-002"]);
        var writer = new CaseWriter(new FixedDbSession(context), numbers, NullLogger<CaseWriter>.Instance);

        var created = await writer.Create(new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek" }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(created.CaseNumber, Is.EqualTo("EC/20260821-002"), "the loser of the race files under the next free number");
            Assert.That(context.Saves, Is.EqualTo(2));
            Assert.That(context.Added<Case>().Count(), Is.EqualTo(1), "the row that lost the race is not written twice");
        }
    }

    [Test]
    public void AFailureThatIsNotATakenNumberReachesTheCaller()
    {
        var userContext = new StubUserContext();
        using var entered = userContext.Enter(Guid.CreateVersion7(), Guid.CreateVersion7());
        using var context = FakeApplicationDbContext.Create(userContext);
        context.FailNextSave = new DbUpdateException("the row is gone");

        var numbers = new QueuedCaseNumberIssuer(["EC/20260821-001"]);
        var writer = new CaseWriter(new FixedDbSession(context), numbers, NullLogger<CaseWriter>.Instance);

        Assert.That(
            async () => await writer.Create(new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Přestupek" }, CancellationToken.None),
            Throws.InstanceOf<DbUpdateException>());
    }

    private sealed class QueuedCaseNumberIssuer(IReadOnlyList<string> caseNumbers) : ICaseNumberIssuer
    {
        private int issued;

        public Task<string> NextCaseNumber(DateOnly date, CancellationToken cancellationToken)
        {
            return Task.FromResult(caseNumbers[this.issued++]);
        }
    }
}
