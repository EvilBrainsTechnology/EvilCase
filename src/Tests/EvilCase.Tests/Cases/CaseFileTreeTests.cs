using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseFileTreeTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    [Test]
    public async Task ATreeHoldsEveryCaseBelowWithItsActsAndFiles()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var earlier = await this.Tenant.AddCase(Day.AddDays(1), "Dřívější", parentCaseId: root.Id);
        var later = await this.Tenant.AddCase(Day.AddDays(2), "Pozdější", parentCaseId: root.Id);
        var deep = await this.Tenant.AddCase(Day.AddDays(3), "Hluboký", parentCaseId: earlier.Id);
        var second = await this.Tenant.AddAct(root, Day.AddDays(5), "Druhý");
        var first = await this.Tenant.AddAct(root, Day.AddDays(4), "První");
        await this.Tenant.AddActFile(second, "b.pdf");
        await this.Tenant.AddActFile(second, "a.pdf");
        await this.Tenant.AddActFile(await this.Tenant.AddAct(deep, Day, "Hluboký úkon"), "hluboký.pdf");
        await this.Tenant.AddActFile(await this.Tenant.AddAct(await this.Tenant.AddCase(Day, "Cizí"), Day), "cizí.pdf");

        var tree = await new CaseReader(this.Tenant.Session).GetCaseFileTree(root.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tree!.SubCases.Select(static node => node.Case.CaseId), Is.EqualTo([later.Id, earlier.Id]), "sub-cases in the order of the sub-case list, the latest date first");
            Assert.That(tree.SubCases[1].SubCases.Single().Case.CaseId, Is.EqualTo(deep.Id), "the tree goes to any depth");
            Assert.That(tree.SubCases[1].SubCases.Single().Acts.Single().Files.Select(static file => file.FileName), Is.EqualTo(["hluboký.pdf"]));
            Assert.That(tree.Acts.Select(static node => node.Act.ActId), Is.EqualTo([first.Id, second.Id]), "acts in the order of the act list, the earliest date first");
            Assert.That(tree.Acts[0].Files, Is.Empty, "an act without files stays in the tree");
            Assert.That(tree.Acts[1].Files.Select(static file => file.FileName), Is.EqualTo(["b.pdf", "a.pdf"]), "files in upload order");
            Assert.That(tree.SubCases[1].Case.HasFiles, Is.True, "a file deeper in the tree counts");
            Assert.That(tree.SubCases[0].Case.HasFiles, Is.False, "a case without files below it has none, whatever another tree holds");
        }
    }

    [Test]
    public async Task AnUnknownOrForeignCaseHasNoTree()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        await other.AddCase(Day, parentCaseId: otherCase.Id);
        await other.AddActFile(await other.AddAct(otherCase, Day));

        var reader = new CaseReader(this.Tenant.Session);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await reader.GetCaseFileTree(otherCase.Id, CancellationToken.None), Is.Null, "the case id alone never reaches another tenant's tree");
            Assert.That(await reader.GetCaseFileTree(Guid.CreateVersion7(), CancellationToken.None), Is.Null);
        }
    }

    [Test]
    public async Task AParentCycleEndsTheTreeAtTheCaseAskedFor()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var child = await this.Tenant.AddCase(Day, "Podspis", parentCaseId: root.Id);
        await this.Tenant.Session.Current.Cases
            .Where(@case => @case.Id == root.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(static @case => @case.ParentCaseId, (Guid?)child.Id));

        var tree = await new CaseReader(this.Tenant.Session).GetCaseFileTree(root.Id, CancellationToken.None);

        Assert.That(tree!.SubCases.Single().SubCases, Is.Empty, "a cycle stored by concurrent edits never leads back into the tree");
    }

    [Test]
    public async Task ACaseCountsTheFilesOfItsTreeAndTheActsHoldingThem()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var child = await this.Tenant.AddCase(Day, "Podspis", parentCaseId: root.Id);
        var act = await this.Tenant.AddAct(root, Day);
        await this.Tenant.AddActFile(act, "a.pdf");
        await this.Tenant.AddFileVersion(await this.Tenant.AddActFile(act, "b.pdf"));
        await this.Tenant.AddActFile(await this.Tenant.AddAct(child, Day), "c.pdf");
        await this.Tenant.AddAct(child, Day);

        var reader = new CaseReader(this.Tenant.Session);
        var detail = await reader.GetCaseDetail(root.Id, CancellationToken.None);
        var listed = await reader.ListCases(new CaseListRequest { Take = 10 }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.FileCount, Is.EqualTo(3), "a file counts once, whatever its versions");
            Assert.That(detail.FileActCount, Is.EqualTo(2), "only acts that hold a file");
            Assert.That(listed.Items.Single(item => item.CaseId == root.Id).HasFiles, Is.True, "a file of a sub-case's act counts for the list");
        }
    }
}
