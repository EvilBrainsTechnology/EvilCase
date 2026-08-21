using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class ActNumberIssuerTests
{
    private static readonly DateOnly ActDay = new(2026, 8, 12);

    [Test]
    public async Task ARaceOnTheNumberTakesTheNextSequenceAndSavesAgain()
    {
        using var context = ConflictingDbContext.Create(NumberConflict.ActNumberIndex, conflicts: 1);
        var act = new Act
        {
            UserId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            ActNumber = "EC/20260807-001/20260812-001",
            Direction = ActDirection.Outgoing,
            Title = "test",
            Date = ActDay,
            IssuedByContactId = Guid.CreateVersion7(),
        };
        context.Acts.Add(act);
        var issuer = new ActNumberIssuer(new FixedDbSession(context));

        await issuer.Save(act);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.ActNumber, Is.EqualTo("EC/20260807-001/20260812-002"), "the retry composes the next sequence of the same case and day");
            Assert.That(context.Saves, Is.EqualTo(2), "the retry saves once more after the raced attempt");
        }
    }

    [Test]
    public void AHandWrittenNumberIsNeverRenumbered()
    {
        using var context = ConflictingDbContext.Create(NumberConflict.ActNumberIndex, conflicts: 1);
        var act = new Act
        {
            UserId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            ActNumber = "cj 7/2026",
            Direction = ActDirection.Outgoing,
            Title = "test",
            Date = ActDay,
            IssuedByContactId = Guid.CreateVersion7(),
        };
        context.Acts.Add(act);
        var issuer = new ActNumberIssuer(new FixedDbSession(context));

        using (Assert.EnterMultipleScope())
        {
            Assert.ThrowsAsync<DbUpdateException>(() => issuer.Save(act), "a hand-written number outside the format never gets renumbered");
            Assert.That(context.Saves, Is.EqualTo(1), "the retry never runs for a number it cannot parse");
        }
    }

    [Test]
    public void AConflictOnAnotherIndexSurfaces()
    {
        using var context = ConflictingDbContext.Create("IX_Acts_CaseId", conflicts: 1);
        var act = new Act
        {
            UserId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            ActNumber = "EC/20260807-001/20260812-001",
            Direction = ActDirection.Outgoing,
            Title = "test",
            Date = ActDay,
            IssuedByContactId = Guid.CreateVersion7(),
        };
        context.Acts.Add(act);
        var issuer = new ActNumberIssuer(new FixedDbSession(context));

        using (Assert.EnterMultipleScope())
        {
            Assert.ThrowsAsync<DbUpdateException>(() => issuer.Save(act), "a violation of another index is not the retry's to handle");
            Assert.That(context.Saves, Is.EqualTo(1), "the retry never runs for a conflict on another index");
        }
    }

    [Test]
    public void TheRetryGivesUp()
    {
        using var context = ConflictingDbContext.Create(NumberConflict.ActNumberIndex, conflicts: 99);
        var act = new Act
        {
            UserId = Guid.CreateVersion7(),
            CaseId = Guid.CreateVersion7(),
            ActNumber = "EC/20260807-001/20260812-001",
            Direction = ActDirection.Outgoing,
            Title = "test",
            Date = ActDay,
            IssuedByContactId = Guid.CreateVersion7(),
        };
        context.Acts.Add(act);
        var issuer = new ActNumberIssuer(new FixedDbSession(context));

        using (Assert.EnterMultipleScope())
        {
            Assert.ThrowsAsync<DbUpdateException>(() => issuer.Save(act), "the retry gives up after its attempts");
            Assert.That(context.Saves, Is.EqualTo(5), "at most five attempts are made");
        }
    }
}
