using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// The delete cascade against a real PostgreSQL: the foreign keys carry it, so no fake decides it
/// (SDD-007). Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ActDeleteTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private TestTenant tenant = null!;

    private FakeFileBlobStore blobs = null!;

    private ActWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.blobs = new FakeFileBlobStore();
        this.writer = new ActWriter(new FixedDbSession(this.tenant.Context), new FakeActNumberIssuer(), this.blobs, NullLogger<ActWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task DeletingAnActTakesItsCommentsExternalNumbersAndFiles()
    {
        var contact = await this.tenant.AddContact("Úřad");
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(seeded, Day);
        var comment = await this.tenant.AddActComment(act, "Poznámka k úkonu");
        var externalNumber = await this.tenant.AddExternalActNumber(act, "EXT-1", contact);
        var file = await this.tenant.AddActFile(act);

        var result = await this.writer.DeleteAct(seeded.Id, act.Id, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();

        var actExists = await this.tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);
        var commentExists = await this.tenant.Context.Comments.AnyAsync(row => row.Id == comment.Id);
        var externalNumberExists = await this.tenant.Context.ExternalActNumbers.AnyAsync(row => row.Id == externalNumber.Id);
        var fileExists = await this.tenant.Context.FileAssets.AnyAsync(row => row.Id == file.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(ActDeleteOutcome.Deleted));
            Assert.That(actExists, Is.False, "the cascade takes the act itself");
            Assert.That(commentExists, Is.False, "the cascade takes the act's comments");
            Assert.That(externalNumberExists, Is.False, "the cascade takes the act's external act numbers");
            Assert.That(fileExists, Is.False, "the cascade takes the act's files");
        }
    }

    [Test]
    public async Task TheCaseAndItsOtherActsAreLeftAlone()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(seeded, Day);
        await this.tenant.AddActComment(act, "Poznámka k prvnímu úkonu");
        await this.tenant.AddActFile(act);
        var otherAct = await this.tenant.AddAct(seeded, Day);
        var otherComment = await this.tenant.AddActComment(otherAct, "Poznámka k druhému úkonu");
        var otherFile = await this.tenant.AddActFile(otherAct);

        await this.writer.DeleteAct(seeded.Id, act.Id, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();

        var caseExists = await this.tenant.Context.Cases.AnyAsync(row => row.Id == seeded.Id);
        var otherActExists = await this.tenant.Context.Acts.AnyAsync(row => row.Id == otherAct.Id);
        var otherCommentExists = await this.tenant.Context.Comments.AnyAsync(row => row.Id == otherComment.Id);
        var otherFileExists = await this.tenant.Context.FileAssets.AnyAsync(row => row.Id == otherFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseExists, Is.True, "the cascade reaches only the deleted act");
            Assert.That(otherActExists, Is.True, "the cascade leaves the case's other acts");
            Assert.That(otherCommentExists, Is.True, "the cascade leaves the other act's comments");
            Assert.That(otherFileExists, Is.True, "the cascade leaves the other act's files");
            Assert.That(this.blobs.Deleted, Does.Not.Contain(otherFile.StoragePath));
        }
    }

    [Test]
    public async Task TheBlobsOfTheActGoWithTheRecord()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(seeded, Day);
        var firstFile = await this.tenant.AddActFile(act, "prvni.pdf");
        var secondFile = await this.tenant.AddActFile(act, "druhy.pdf");

        await this.writer.DeleteAct(seeded.Id, act.Id, CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.EquivalentTo([firstFile.StoragePath, secondFile.StoragePath]), "the bytes of every file the cascade takes go with the record");
    }

    [Test]
    public async Task AnUnknownActIsNotFound()
    {
        var result = await this.writer.DeleteAct(Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(ActDeleteOutcome.NotFound));
    }

    [Test]
    public async Task AnActOfAnotherCaseIsNotFound()
    {
        var caseA = await this.tenant.AddCase(Day, "Případ A");
        var act = await this.tenant.AddAct(caseA, Day);
        var caseB = await this.tenant.AddCase(Day, "Případ B");

        var result = await this.writer.DeleteAct(caseB.Id, act.Id, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();

        var actExists = await this.tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(ActDeleteOutcome.NotFound), "an act scoped to another case is not found");
            Assert.That(actExists, Is.True);
            Assert.That(this.blobs.Deleted, Is.Empty);
        }
    }

    [Test]
    public async Task AnActOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day, "Cizí spis");
        var otherAct = await other.AddAct(otherCase, Day);

        var result = await this.writer.DeleteAct(otherCase.Id, otherAct.Id, CancellationToken.None);

        other.Context.ChangeTracker.Clear();

        var otherActExists = await other.Context.Acts.AnyAsync(row => row.Id == otherAct.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(ActDeleteOutcome.NotFound), "the tenant query filter is what keeps another tenant's act out of a delete");
            Assert.That(otherActExists, Is.True);
        }
    }

    [Test]
    public async Task NoBlobIsDeletedWhereNoActIs()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(seeded, Day);
        await this.tenant.AddActFile(act);

        await this.writer.DeleteAct(seeded.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.Empty, "a delete that found no act takes no bytes");
    }
}
