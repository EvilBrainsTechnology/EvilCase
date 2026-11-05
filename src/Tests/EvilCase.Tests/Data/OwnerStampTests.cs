using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Labels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Data;

public class OwnerStampTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    [Test]
    public async Task ACommentAddedEditedAndDeletedStampsItsCase()
    {
        var @case = await this.Tenant.AddCase(Day);

        var comment = await this.Tenant.AddCaseComment(@case, "Poznámka");
        var afterAdd = await this.CaseUpdated(@case.Id);

        await this.Tenant.Context.Comments.Where(row => row.Id == comment.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static row => row.Body, "Upravená poznámka"));
        var afterEdit = await this.CaseUpdated(@case.Id);

        await this.Tenant.Context.Comments.Where(row => row.Id == comment.Id).ExecuteDeleteAsync();
        var afterDelete = await this.CaseUpdated(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterAdd, Is.Not.Null, "a comment written on a case stamps the case's Updated");
            Assert.That(afterEdit, Is.GreaterThan(afterAdd.GetValueOrDefault()), "an edited comment stamps the Updated of its case");
            Assert.That(afterDelete, Is.GreaterThan(afterEdit.GetValueOrDefault()), "a deleted comment stamps the Updated of its case");
        }
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
    public async Task ARenamedOrMainFileStampsItsAct()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var file = await this.Tenant.AddActFile(act);
        var afterFile = await this.ActUpdated(act.Id);

        await this.Tenant.Context.FileAssets.Where(row => row.Id == file.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static row => row.FileName, "přejmenovaný.pdf"));
        var afterRename = await this.ActUpdated(act.Id);

        await this.Tenant.Context.FileAssets.Where(row => row.Id == file.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static row => row.IsMain, valueExpression: true));
        var afterMain = await this.ActUpdated(act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterRename, Is.GreaterThan(afterFile.GetValueOrDefault()), "a renamed file stamps the Updated of its act");
            Assert.That(afterMain, Is.GreaterThan(afterRename.GetValueOrDefault()), "a file made main stamps the Updated of its act");
        }
    }

    [Test]
    public async Task ALabelAddedOrTakenOffStampsItsCaseAndTheLabelsDeletionDoesNot()
    {
        var @case = await this.Tenant.AddCase(Day);
        var kept = await this.Tenant.AddLabel("Naléhavé");
        var dropped = await this.Tenant.AddLabel("Soud");

        await this.Tenant.AddCaseLabel(@case, kept);
        await this.Tenant.AddCaseLabel(@case, dropped);
        var afterAssignment = await this.CaseUpdated(@case.Id);

        var writer = new LabelAssignmentWriter(this.Tenant.Session, NullLogger<LabelAssignmentWriter>.Instance);
        await writer.SetCaseLabels(@case.Id, new LabelAssignmentRequest { LabelIds = [kept.Id] }, CancellationToken.None);
        var afterTakeOff = await this.CaseUpdated(@case.Id);

        await this.Tenant.Context.Labels.Where(row => row.Id == kept.Id).ExecuteDeleteAsync();
        var afterLabelDeletion = await this.CaseUpdated(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(afterAssignment, Is.Not.Null, "a label assigned to a case stamps the case's Updated");
            Assert.That(afterTakeOff, Is.GreaterThan(afterAssignment.GetValueOrDefault()), "a label taken off a case stamps the case's Updated");
            Assert.That(afterLabelDeletion, Is.EqualTo(afterTakeOff), "a label deleted in settings takes its assignments without stamping their cases");
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
