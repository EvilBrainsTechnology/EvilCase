using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

public class OwnerStampTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    [Test]
    public async Task ACommentStampsItsCase()
    {
        var @case = await this.Tenant.AddCase(Day);

        await this.Tenant.AddCaseComment(@case, "Poznámka");

        Assert.That(await this.CaseUpdated(@case.Id), Is.Not.Null, "a comment written on a case stamps the case's Updated");
    }

    [Test]
    public async Task ACommentOnAnActStampsTheActAndNotItsCase()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        await this.Tenant.AddActComment(act, "Poznámka");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.ActUpdated(act.Id), Is.Not.Null, "a comment written on an act stamps the act's Updated");
            Assert.That(await this.CaseUpdated(@case.Id), Is.Null, "a write on an act leaves the Updated of its case alone");
        }
    }

    [Test]
    public async Task AFileVersionUploadAndDeleteStampTheCase()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case);
        var afterFile = await this.CaseUpdated(@case.Id);

        var version = await this.Tenant.AddFileVersion(file);
        var afterUpload = await this.CaseUpdated(@case.Id);

        await this.Tenant.Context.FileVersions.Where(row => row.Id == version.Id).ExecuteDeleteAsync();
        var afterDelete = await this.CaseUpdated(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterFile, Is.Not.Null, "a file stored on a case stamps the case's Updated");
            Assert.That(afterUpload, Is.GreaterThan(afterFile.GetValueOrDefault()), "a new version of a file stamps the Updated of the file's case");
            Assert.That(afterDelete, Is.GreaterThan(afterUpload.GetValueOrDefault()), "a deleted version of a file stamps the Updated of the file's case");
        }
    }

    [Test]
    public async Task ALabelAssignmentStampsItsCaseAndTheLabelsDeletionDoesNot()
    {
        var @case = await this.Tenant.AddCase(Day);
        var label = await this.Tenant.AddLabel("Naléhavé");

        await this.Tenant.AddCaseLabel(@case, label);
        var afterAssignment = await this.CaseUpdated(@case.Id);

        await this.Tenant.Context.Labels.Where(row => row.Id == label.Id).ExecuteDeleteAsync();
        var afterLabelDeletion = await this.CaseUpdated(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterAssignment, Is.Not.Null, "a label assigned to a case stamps the case's Updated");
            Assert.That(afterLabelDeletion, Is.EqualTo(afterAssignment), "a label deleted in settings takes its assignments without stamping their cases");
        }
    }

    [Test]
    public async Task AnActLeavesItsCaseAlone()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        await this.Tenant.Context.Acts.Where(row => row.Id == act.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static row => row.Title, "Upravený úkon"));
        await this.Tenant.Context.Acts.Where(row => row.Id == act.Id).ExecuteDeleteAsync();

        Assert.That(await this.CaseUpdated(@case.Id), Is.Null, "creating, editing or deleting an act leaves the Updated of its case alone");
    }

    [Test]
    public async Task AChildCaseLeavesItsParentAlone()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);

        await this.Tenant.Context.Cases.Where(row => row.Id == child.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static row => row.Title, "Upravený podřízený"));

        Assert.That(await this.CaseUpdated(parent.Id), Is.Null, "creating or editing a subordinate case leaves the Updated of its parent alone");
    }

    private async Task<DateTime?> CaseUpdated(Guid caseId)
    {
        return await this.Tenant.Context.Cases.Where(row => row.Id == caseId).Select(static row => row.Updated).SingleAsync();
    }

    private async Task<DateTime?> ActUpdated(Guid actId)
    {
        return await this.Tenant.Context.Acts.Where(row => row.Id == actId).Select(static row => row.Updated).SingleAsync();
    }
}
