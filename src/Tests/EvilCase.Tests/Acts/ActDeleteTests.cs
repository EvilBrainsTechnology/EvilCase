using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActDeleteTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private FakeFileBlobStore blobs = null!;

    private ActWriter writer = null!;

    [SetUp]
    public void SetUpWriter()
    {
        this.blobs = new FakeFileBlobStore();
        this.writer = new ActWriter(
            new FixedDbSession(this.Tenant.Context), new FakeActNumberIssuer(), this.blobs, NullLogger<ActWriter>.Instance);
    }

    [Test]
    public async Task DeletingAnActTakesItsCommentsAndFiles()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        var comment = await this.Tenant.AddActComment(act, "Poznámka k úkonu");
        var file = await this.Tenant.AddActFile(act);

        var result = await this.writer.DeleteAct(seeded.Id, act.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var actExists = await this.Tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);
        var commentExists = await this.Tenant.Context.Comments.AnyAsync(row => row.Id == comment.Id);
        var fileExists = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == file.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(actExists, Is.False, "the cascade takes the act itself");
            Assert.That(commentExists, Is.False, "the cascade takes the act's comments");
            Assert.That(fileExists, Is.False, "the cascade takes the act's files");
        }
    }

    [Test]
    public async Task TheCaseAndItsOtherActsAreLeftAlone()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        await this.Tenant.AddActComment(act, "Poznámka k prvnímu úkonu");
        await this.Tenant.AddActFile(act);
        var otherAct = await this.Tenant.AddAct(seeded, Day);
        var otherComment = await this.Tenant.AddActComment(otherAct, "Poznámka k druhému úkonu");
        var otherFile = await this.Tenant.AddActFile(otherAct);

        await this.writer.DeleteAct(seeded.Id, act.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var caseExists = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == seeded.Id);
        var otherActExists = await this.Tenant.Context.Acts.AnyAsync(row => row.Id == otherAct.Id);
        var otherCommentExists = await this.Tenant.Context.Comments.AnyAsync(row => row.Id == otherComment.Id);
        var otherFileExists = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == otherFile.Id);

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
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        var firstFile = await this.Tenant.AddActFile(act, "prvni.pdf");
        var secondFile = await this.Tenant.AddActFile(act, "druhy.pdf");

        await this.writer.DeleteAct(seeded.Id, act.Id, CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.EquivalentTo([firstFile.StoragePath, secondFile.StoragePath]), "the bytes of every file the cascade takes go with the record");
    }

    [Test]
    public async Task AnUnknownActIsNotFound()
    {
        var result = await this.writer.DeleteAct(Guid.CreateVersion7(), Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(DeleteOutcome.NotFound));
    }

    [Test]
    public async Task AnActOfAnotherCaseIsNotFound()
    {
        var caseA = await this.Tenant.AddCase(Day, "Případ A");
        var act = await this.Tenant.AddAct(caseA, Day);
        var caseB = await this.Tenant.AddCase(Day, "Případ B");

        var result = await this.writer.DeleteAct(caseB.Id, act.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var actExists = await this.Tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.NotFound), "an act scoped to another case is not found");
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
            Assert.That(result, Is.EqualTo(DeleteOutcome.NotFound), "the tenant query filter is what keeps another tenant's act out of a delete");
            Assert.That(otherActExists, Is.True);
        }
    }

    [Test]
    public async Task NoBlobIsDeletedWhereNoActIs()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        await this.Tenant.AddActFile(act);

        await this.writer.DeleteAct(seeded.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(this.blobs.Deleted, Is.Empty, "a delete that found no act takes no bytes");
    }

}
