using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The delete cascade against a real PostgreSQL: the foreign keys carry it, so no fake decides it
/// (SDD-007). Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class CaseDeleteTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private TestTenant tenant = null!;

    private FakeFileBlobStore blobs = null!;

    private CaseWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.blobs = new FakeFileBlobStore();
        this.writer = new CaseWriter(new FixedDbSession(this.tenant.Context), new FakeCaseNumberIssuer(), this.blobs, NullLogger<CaseWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task DeletingACaseTakesItsActsCommentsMarksAndFiles()
    {
        var contact = await this.tenant.AddContact("Úřad");
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(seeded, Day);
        var caseComment = await this.tenant.AddCaseComment(seeded, "Poznámka ke spisu");
        var actComment = await this.tenant.AddActComment(act, "Poznámka k úkonu");
        await this.tenant.AddExternalCaseNumber(seeded, "EXT-1", contact);
        await this.tenant.AddExternalActNumber(act, "EXT-2", contact);
        var caseFile = await this.tenant.AddCaseFile(seeded);
        var actFile = await this.tenant.AddActFile(act);

        var result = await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();

        var caseExists = await this.tenant.Context.Cases.AnyAsync(row => row.Id == seeded.Id);
        var actExists = await this.tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);
        var commentsExist = await this.tenant.Context.Comments.AnyAsync(row => row.Id == caseComment.Id || row.Id == actComment.Id);
        var externalCaseNumbersExist = await this.tenant.Context.ExternalCaseNumbers.AnyAsync(row => row.CaseId == seeded.Id);
        var externalActNumbersExist = await this.tenant.Context.ExternalActNumbers.AnyAsync(row => row.ActId == act.Id);
        var filesExist = await this.tenant.Context.FileAssets.AnyAsync(row => row.Id == caseFile.Id || row.Id == actFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CaseDeleteOutcome.Deleted));
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
        var parent = await this.tenant.AddCase(Day, "Rodič");
        var child = await this.tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);

        var result = await this.writer.DeleteCase(parent.Id, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();

        var reloadedChild = await this.tenant.Context.Cases.SingleOrDefaultAsync(row => row.Id == child.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(CaseDeleteOutcome.Deleted));
            Assert.That(reloadedChild, Is.Not.Null, "a subordinate case survives the delete of its parent");
            Assert.That(reloadedChild!.ParentCaseId, Is.Null, "the surviving subordinate case is left without a parent");
        }
    }

    [Test]
    public async Task TheBlobsOfTheCaseAndItsActsGoWithTheRecords()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(seeded, Day);
        var caseFile = await this.tenant.AddCaseFile(seeded);
        var actFile = await this.tenant.AddActFile(act);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.EquivalentTo([caseFile.StoragePath, actFile.StoragePath]), "the bytes of every file the cascade takes go with the record");
    }

    [Test]
    public async Task AnotherCasesFilesAndBlobsAreLeftAlone()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        await this.tenant.AddCaseFile(seeded);
        var other = await this.tenant.AddCase(Day, "Jiný");
        var otherFile = await this.tenant.AddCaseFile(other);

        await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();

        var otherFileExists = await this.tenant.Context.FileAssets.AnyAsync(row => row.Id == otherFile.Id);

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

        Assert.That(result, Is.EqualTo(CaseDeleteOutcome.NotFound));
    }

    [Test]
    public async Task NoBlobIsDeletedWhereNoCaseIs()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        await this.tenant.AddCaseFile(seeded);

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
            Assert.That(result, Is.EqualTo(CaseDeleteOutcome.NotFound), "the tenant query filter is what keeps another tenant's case out of a delete");
            Assert.That(otherCaseExists, Is.True);
        }
    }
}
