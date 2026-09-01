using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The delete cascade against a real PostgreSQL: the foreign keys carry it, so no fake decides it
/// (SDD-007). Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class CaseDeleteTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private FakeFileBlobStore blobs = null!;

    private CaseWriter writer = null!;

    [SetUp]
    public void SetUpWriter()
    {
        this.blobs = new FakeFileBlobStore();
        this.writer = new CaseWriter(
            new FixedDbSession(this.Tenant.Context), new FakeCaseNumberIssuer(), this.blobs, NullLogger<CaseWriter>.Instance);
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
    public async Task ASubordinateCaseSurvivesWithoutAParent()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);

        var result = await this.writer.DeleteCase(parent.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var reloadedChild = await this.Tenant.Context.Cases.SingleOrDefaultAsync(row => row.Id == child.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(reloadedChild, Is.Not.Null, "a subordinate case survives the delete of its parent");
            Assert.That(reloadedChild!.ParentCaseId, Is.Null, "the surviving subordinate case is left without a parent");
        }
    }

    [Test]
    public async Task TheBlobsOfTheCaseAndItsActsGoWithTheRecords()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        var caseFile = await this.Tenant.AddCaseFile(seeded);
        var actFile = await this.Tenant.AddActFile(act);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.EquivalentTo([caseFile.StoragePath, actFile.StoragePath]), "the bytes of every file the cascade takes go with the record");
    }

    [Test]
    public async Task AnotherCasesFilesAndBlobsAreLeftAlone()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        await this.Tenant.AddCaseFile(seeded);
        var other = await this.Tenant.AddCase(Day, "Jiný");
        var otherFile = await this.Tenant.AddCaseFile(other);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var otherFileExists = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == otherFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(otherFileExists, Is.True, "the cascade reaches only the files of the deleted case and its acts");
            Assert.That(this.blobs.Deleted, Does.Not.Contain(otherFile.StoragePath));
        }
    }

    [Test]
    public async Task AnUnknownCaseIsNotFound()
    {
        var result = await this.writer.DeleteCase(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(DeleteOutcome.NotFound));
    }

    [Test]
    public async Task NoBlobIsDeletedWhereNoCaseIs()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        await this.Tenant.AddCaseFile(seeded);

        await this.writer.DeleteCase(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.Empty, "a delete that found no case takes no bytes");
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

}
