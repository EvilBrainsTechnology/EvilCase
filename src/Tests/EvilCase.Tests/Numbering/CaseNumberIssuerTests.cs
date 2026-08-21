using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class CaseNumberIssuerTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    [Test]
    public async Task ARaceOnTheNumberTakesTheNextSequenceAndSavesAgain()
    {
        await using var context = ConflictingDbContext.Create(NumberConflict.CaseNumberIndex, conflicts: 1);
        var @case = new Case { UserId = Guid.CreateVersion7(), CaseNumber = "EC/20260807-001", Date = Day, Title = "test", Status = CaseStatus.Active };
        context.Cases.Add(@case);
        var issuer = new CaseNumberIssuer(new FixedDbSession(context));

        await issuer.Save(@case);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.CaseNumber, Is.EqualTo("EC/20260807-002"), "the retry composes the next sequence of the same day");
            Assert.That(context.Saves, Is.EqualTo(2), "the retry saves once more after the raced attempt");
        }
    }

    [Test]
    public void AHandWrittenNumberIsNeverRenumbered()
    {
        using var context = ConflictingDbContext.Create(NumberConflict.CaseNumberIndex, conflicts: 1);
        var @case = new Case { UserId = Guid.CreateVersion7(), CaseNumber = "spis 7/2026", Date = Day, Title = "test", Status = CaseStatus.Active };
        context.Cases.Add(@case);
        var issuer = new CaseNumberIssuer(new FixedDbSession(context));

        using (Assert.EnterMultipleScope())
        {
            Assert.ThrowsAsync<DbUpdateException>(() => issuer.Save(@case), "a hand-written number outside the format never gets renumbered");
            Assert.That(context.Saves, Is.EqualTo(1), "the retry never runs for a number it cannot parse");
        }
    }

    [Test]
    public void AConflictOnAnotherIndexSurfaces()
    {
        using var context = ConflictingDbContext.Create("IX_Cases_ParentCaseId", conflicts: 1);
        var @case = new Case { UserId = Guid.CreateVersion7(), CaseNumber = "EC/20260807-001", Date = Day, Title = "test", Status = CaseStatus.Active };
        context.Cases.Add(@case);
        var issuer = new CaseNumberIssuer(new FixedDbSession(context));

        using (Assert.EnterMultipleScope())
        {
            Assert.ThrowsAsync<DbUpdateException>(() => issuer.Save(@case), "a violation of another index is not the retry's to handle");
            Assert.That(context.Saves, Is.EqualTo(1), "the retry never runs for a conflict on another index");
        }
    }

    [Test]
    public void TheRetryGivesUp()
    {
        using var context = ConflictingDbContext.Create(NumberConflict.CaseNumberIndex, conflicts: 99);
        var @case = new Case { UserId = Guid.CreateVersion7(), CaseNumber = "EC/20260807-001", Date = Day, Title = "test", Status = CaseStatus.Active };
        context.Cases.Add(@case);
        var issuer = new CaseNumberIssuer(new FixedDbSession(context));

        using (Assert.EnterMultipleScope())
        {
            Assert.ThrowsAsync<DbUpdateException>(() => issuer.Save(@case), "the retry gives up after its attempts");
            Assert.That(context.Saves, Is.EqualTo(5), "at most five attempts are made");
        }
    }
}
