using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The delete cascade against a real PostgreSQL. Each test seeds a tenant of its own, so none cleans
/// up after itself.
/// </summary>
public class CaseDeleteTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private CaseWriter writer = null!;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new CaseWriter(
            new FixedDbSession(this.Tenant.Context), new FakeCaseNumberIssuer(), NullLogger<CaseWriter>.Instance);
    }

    [Test]
    public async Task DeletingACaseTakesItsActsCommentsMarksAndFiles()
    {
        var contact = await this.Tenant.AddContact("Úřad");
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        var caseComment = await this.Tenant.AddCaseComment(seeded, "Poznámka ke spisu");
        var actComment = await this.Tenant.AddActComment(act, "Poznámka k úkonu");
        await this.Tenant.AddExternalCaseNumber(seeded, "EXT-1", contact);
        await this.Tenant.AddExternalActNumber(act, "EXT-2", contact);
        var caseFile = await this.Tenant.AddCaseFile(seeded);
        var actFile = await this.Tenant.AddActFile(act);

        var result = await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var caseExists = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == seeded.Id);
        var actExists = await this.Tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);
        var commentsExist = await this.Tenant.Context.Comments.AnyAsync(row => row.Id == caseComment.Id || row.Id == actComment.Id);
        var externalCaseNumbersExist = await this.Tenant.Context.ExternalCaseNumbers.AnyAsync(row => row.CaseId == seeded.Id);
        var externalActNumbersExist = await this.Tenant.Context.ExternalActNumbers.AnyAsync(row => row.ActId == act.Id);
        var filesExist = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == caseFile.Id || row.Id == actFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(caseExists, Is.False, "the cascade takes the case itself");
            Assert.That(actExists, Is.False, "the cascade takes the case's acts");
            Assert.That(commentsExist, Is.False, "the cascade takes the comments of the case and of its acts");
            Assert.That(externalCaseNumbersExist, Is.False, "the cascade takes the case's external marks");
            Assert.That(externalActNumbersExist, Is.False, "the cascade takes the external numbers of the case's acts");
            Assert.That(filesExist, Is.False, "the cascade takes the files of the case and of its acts");
        }
    }

    [Test]
    public async Task EverythingTheCascadeTakesCarriesOneMoment()
    {
        var contact = await this.Tenant.AddContact("Úřad");
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        await this.Tenant.AddCaseComment(seeded, "Poznámka ke spisu");
        await this.Tenant.AddActComment(act, "Poznámka k úkonu");
        await this.Tenant.AddExternalCaseNumber(seeded, "EXT-1", contact);
        await this.Tenant.AddExternalActNumber(act, "EXT-2", contact);
        await this.Tenant.AddCaseFile(seeded);
        await this.Tenant.AddActFile(act);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var stamps = await this.Stamps(seeded.Id, act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stamps, Has.Count.EqualTo(8), "every row the cascade reaches carries a stamp");
            Assert.That(stamps.Distinct().ToList(), Has.Count.EqualTo(1), "one transaction stamps the whole cascade with one moment");
        }
    }

    [Test]
    public async Task ASubordinateCaseGoesWithItsParent()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);
        var grandChild = await this.Tenant.AddCase(Day, "Vnuk", parentCaseId: child.Id);

        var result = await this.writer.DeleteCase(parent.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var descendantsExist = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == child.Id || row.Id == grandChild.Id);
        var stamped = await this.Tenant.Context.Cases
            .IncludingDeleted()
            .Where(row => row.Id == child.Id || row.Id == grandChild.Id)
            .ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(descendantsExist, Is.False, "the cascade follows the hierarchy to the bottom");
            Assert.That(stamped.Select(static row => row.ParentCaseId), Has.No.Null, "a stamped case keeps its place, so restoring the parent restores the tree");
        }
    }

    [Test]
    public async Task AStampedRowKeepsTheMomentAnEarlierDeleteGaveIt()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        var acts = new ActWriter(
            new FixedDbSession(this.Tenant.Context), new FakeActNumberIssuer(), NullLogger<ActWriter>.Instance);

        await acts.DeleteAct(seeded.Id, act.Id, CancellationToken.None);
        var actStamp = await this.Stamp(act.Id);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        Assert.That(await this.Stamp(act.Id), Is.EqualTo(actStamp), "the filter keeps the cascade off a row an earlier delete already took");
    }

    [Test]
    public async Task AnotherCasesRowsAreLeftAlone()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        await this.Tenant.AddCaseFile(seeded);
        var other = await this.Tenant.AddCase(Day, "Jiný");
        var otherFile = await this.Tenant.AddCaseFile(other);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var otherFileExists = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == otherFile.Id);

        Assert.That(otherFileExists, Is.True, "the cascade reaches only the files of the deleted case and its acts");
    }

    [Test]
    public async Task AnUnknownCaseIsNotFound()
    {
        var result = await this.writer.DeleteCase(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(DeleteOutcome.NotFound));
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day, "Cizí spis");

        var result = await this.writer.DeleteCase(otherCase.Id, CancellationToken.None);

        other.Context.ChangeTracker.Clear();

        var otherCaseExists = await other.Context.Cases.AnyAsync(row => row.Id == otherCase.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.NotFound), "the tenant query filter is what keeps another tenant's case out of a delete");
            Assert.That(otherCaseExists, Is.True);
        }
    }

    private async Task<List<DateTime>> Stamps(Guid caseId, Guid actId)
    {
        var context = this.Tenant.Context;

        var cases = await context.Cases.IncludingDeleted().Where(row => row.Id == caseId).Select(static row => row.Deleted).ToListAsync();
        var acts = await context.Acts.IncludingDeleted().Where(row => row.Id == actId).Select(static row => row.Deleted).ToListAsync();
        var comments = await context.Comments.IncludingDeleted().Select(static row => row.Deleted).ToListAsync();
        var caseNumbers = await context.ExternalCaseNumbers.IncludingDeleted().Select(static row => row.Deleted).ToListAsync();
        var actNumbers = await context.ExternalActNumbers.IncludingDeleted().Select(static row => row.Deleted).ToListAsync();
        var files = await context.FileAssets.IncludingDeleted().Select(static row => row.Deleted).ToListAsync();

        return [.. cases.Concat(acts).Concat(comments).Concat(caseNumbers).Concat(actNumbers).Concat(files).OfType<DateTime>()];
    }

    private async Task<DateTime?> Stamp(Guid actId)
    {
        return await this.Tenant.Context.Acts
            .IncludingDeleted()
            .Where(row => row.Id == actId)
            .Select(static row => row.Deleted)
            .SingleAsync();
    }
}
