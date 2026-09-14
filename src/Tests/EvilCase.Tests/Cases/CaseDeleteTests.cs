using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

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
    public async Task DeletingACaseTakesItsActsCommentsAndFiles()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var act = await this.Tenant.AddAct(seeded, Day);
        var caseComment = await this.Tenant.AddCaseComment(seeded, "Poznámka ke spisu");
        var actComment = await this.Tenant.AddActComment(act, "Poznámka k úkonu");
        var caseFile = await this.Tenant.AddCaseFile(seeded);
        var actFile = await this.Tenant.AddActFile(act);

        var result = await this.writer.DeleteCase(seeded.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var caseExists = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == seeded.Id);
        var actExists = await this.Tenant.Context.Acts.AnyAsync(row => row.Id == act.Id);
        var commentsExist = await this.Tenant.Context.Comments.AnyAsync(row => row.Id == caseComment.Id || row.Id == actComment.Id);
        var filesExist = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == caseFile.Id || row.Id == actFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(caseExists, Is.False, "the cascade takes the case itself");
            Assert.That(actExists, Is.False, "the cascade takes the case's acts");
            Assert.That(commentsExist, Is.False, "the cascade takes the comments of the case and of its acts");
            Assert.That(filesExist, Is.False, "the cascade takes the files of the case and of its acts");
        }
    }

    [Test]
    public async Task TheSubordinateCasesGoWithTheirParent()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);
        var grandchild = await this.Tenant.AddCase(Day, "Podřízený podřízeného", parentCaseId: child.Id);

        var result = await this.writer.DeleteCase(parent.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var childExists = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == child.Id);
        var grandchildExists = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == grandchild.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(childExists, Is.False, "a subordinate case goes with its parent");
            Assert.That(grandchildExists, Is.False, "the cascade runs down the whole subtree");
        }
    }

    [Test]
    public async Task ASubordinateCaseLosesItsActsCommentsAndFiles()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);
        var childAct = await this.Tenant.AddAct(child, Day);
        var childComment = await this.Tenant.AddCaseComment(child, "Poznámka k podřízenému spisu");
        var childActComment = await this.Tenant.AddActComment(childAct, "Poznámka k úkonu podřízeného spisu");
        var childFile = await this.Tenant.AddCaseFile(child);
        var childActFile = await this.Tenant.AddActFile(childAct);

        await this.writer.DeleteCase(parent.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var actExists = await this.Tenant.Context.Acts.AnyAsync(row => row.Id == childAct.Id);
        var commentsExist = await this.Tenant.Context.Comments.AnyAsync(row => row.Id == childComment.Id || row.Id == childActComment.Id);
        var filesExist = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == childFile.Id || row.Id == childActFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actExists, Is.False, "the cascade takes the acts of a subordinate case");
            Assert.That(commentsExist, Is.False, "the cascade takes the comments of a subordinate case and of its acts");
            Assert.That(filesExist, Is.False, "the cascade takes the files of a subordinate case and of its acts");
        }
    }

    [Test]
    public async Task ACaseOutsideTheSubtreeKeepsItsRows()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);
        var outside = await this.Tenant.AddCase(Day, "Mimo podstrom");
        var outsideAct = await this.Tenant.AddAct(outside, Day);
        var outsideFile = await this.Tenant.AddActFile(outsideAct);

        await this.writer.DeleteCase(parent.Id, CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var outsideExists = await this.Tenant.Context.Cases.AnyAsync(row => row.Id == outside.Id);
        var outsideFileExists = await this.Tenant.Context.FileAssets.AnyAsync(row => row.Id == outsideFile.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outsideExists, Is.True, "the cascade reaches only the subtree of the deleted case");
            Assert.That(outsideFileExists, Is.True);
        }
    }

    [Test]
    public async Task AnotherCasesFilesAreLeftAlone()
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

}
