using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileWriterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private FakeFileBlobStore blobs = null!;

    private FileWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.blobs = new FakeFileBlobStore();
        this.writer = new FileWriter(new FixedDbSession(this.Tenant.Context), this.blobs, this.Tenant.UserContext, NullLogger<FileWriter>.Instance);
    }

    [Test]
    public async Task AnUploadedFileHangsOnItsCaseWithWhatTheStoreMeasured()
    {
        var @case = await this.Tenant.AddCase(Day);

        var result = await this.writer.UploadCaseFile(@case.Id, Upload("smlouva.pdf", "application/pdf", "abc"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(UploadFileOutcome.Uploaded));
            Assert.That(result.File, Is.Not.Null);
        }

        var stored = await this.Reload(result.File!.FileId);
        var version = await this.CurrentVersion(result.File.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.CaseId, Is.EqualTo(@case.Id), "an uploaded file hangs on the case it was uploaded to");
            Assert.That(stored.ActId, Is.Null, "a file uploaded to a case carries no act");
            Assert.That(stored.FileName, Is.EqualTo("smlouva.pdf"), "an uploaded file keeps the name it arrived under");
            Assert.That(version.Number, Is.EqualTo(1), "a new file starts at its first version");
            Assert.That(version.MediaType, Is.EqualTo("application/pdf"), "an uploaded file keeps the media type it arrived under");
            Assert.That(version.ContentHash, Has.Length.EqualTo(64), "the store's hash is what the row carries");
            Assert.That(this.blobs.WrittenByPath[version.StoragePath], Is.EqualTo("abc"), "the row's storage path resolves to what the store wrote");
            Assert.That(version.Id, Is.Not.EqualTo(stored.Id), "the blob is named after the version, not the file");
        }
    }

    [Test]
    public async Task AnUploadNamesTheTenantTheStoreWritesUnder()
    {
        var @case = await this.Tenant.AddCase(Day);
        var recording = new RecordingTenantBlobStore();
        var recordingWriter = new FileWriter(new FixedDbSession(this.Tenant.Context), recording, this.Tenant.UserContext, NullLogger<FileWriter>.Instance);

        await recordingWriter.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);

        Assert.That(recording.WrittenUnderTenant, Is.EqualTo(this.Tenant.UserContext.TenantId), "the upload writes the blob under the tenant IUserContext names");
    }

    [Test]
    public async Task AnUploadOntoAMissingCaseIsRefused()
    {
        var result = await this.writer.UploadCaseFile(Guid.CreateVersion7(), Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(UploadFileOutcome.OwnerNotFound), "an upload naming a case the tenant does not have must be refused");
            Assert.That(result.File, Is.Null);
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(), Is.False, "a refused upload must write no row");
            Assert.That(this.blobs.WrittenByPath, Is.Empty, "a refused upload must write no blob");
        }
    }

    [Test]
    public async Task ADeletedFileLeavesItsBlobOnDisk()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);
        var storagePath = (await this.CurrentVersion(uploaded.File!.FileId)).StoragePath;

        var outcome = await this.writer.DeleteCaseFile(@case.Id, uploaded.File.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.File.FileId), Is.False, "a deleted file leaves no row");
            Assert.That(this.blobs.WrittenByPath, Contains.Key(storagePath), "a deleted file's bytes stay on disk");
        }
    }

    [Test]
    public async Task AFileOfAnotherCaseIsNotDeleted()
    {
        var caseA = await this.Tenant.AddCase(Day);
        var caseB = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(caseA.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.DeleteCaseFile(caseB.Id, uploaded.File!.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.NotFound), "a file of another case must not be found for delete");
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.File.FileId), Is.True, "the file must survive a delete naming the wrong case");
        }
    }

    [Test]
    public async Task DeletingAMissingFileIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.DeleteCaseFile(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(DeleteOutcome.NotFound));
    }

    [Test]
    public async Task AnUploadedFileHangsOnItsActAndOnNoCase()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var result = await this.writer.UploadActFile(act.Id, Upload("smlouva.pdf", "application/pdf", "abc"), CancellationToken.None);

        var stored = await this.Reload(result.File!.FileId);
        var version = await this.CurrentVersion(result.File.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.ActId, Is.EqualTo(act.Id), "an uploaded file hangs on the act it was uploaded to");
            Assert.That(stored.CaseId, Is.Null, "a file uploaded to an act carries no case");
            Assert.That(stored.FileName, Is.EqualTo("smlouva.pdf"));
            Assert.That(version.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(version.ContentHash, Has.Length.EqualTo(64));
            Assert.That(this.blobs.WrittenByPath[version.StoragePath], Is.EqualTo("abc"));
        }
    }

    [Test]
    public async Task AnUploadOntoAMissingActIsRefused()
    {
        var result = await this.writer.UploadActFile(Guid.CreateVersion7(), Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(UploadFileOutcome.OwnerNotFound));
            Assert.That(result.File, Is.Null);
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(), Is.False);
            Assert.That(this.blobs.WrittenByPath, Is.Empty);
        }
    }

    [Test]
    public async Task AnUploadOntoAnActOfAnotherTenantIsRefused()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);

        var result = await this.writer.UploadActFile(otherAct.Id, Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(UploadFileOutcome.OwnerNotFound), "the act id alone never reaches another tenant's act");
            Assert.That(await other.Context.FileAssets.AnyAsync(), Is.False);
            Assert.That(this.blobs.WrittenByPath, Is.Empty);
        }
    }

    [Test]
    public async Task AFileOfAnotherTenantsActIsNotDeleted()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        var otherFile = await other.AddActFile(otherAct);

        var outcome = await this.writer.DeleteActFile(otherAct.Id, otherFile.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.NotFound), "the act id alone never reaches another tenant's file");
            Assert.That(await other.Context.FileAssets.AsNoTracking().AnyAsync(file => file.Id == otherFile.Id), Is.True);
        }
    }

    [Test]
    public async Task ADeletedActFileLeavesItsBlobOnDisk()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var uploaded = await this.writer.UploadActFile(act.Id, Upload("a.txt"), CancellationToken.None);
        var storagePath = (await this.CurrentVersion(uploaded.File!.FileId)).StoragePath;

        var outcome = await this.writer.DeleteActFile(act.Id, uploaded.File.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.File.FileId), Is.False);
            Assert.That(this.blobs.WrittenByPath, Contains.Key(storagePath), "a deleted act file's bytes stay on disk");
        }
    }

    [Test]
    public async Task AFileOfAnotherActIsNotDeleted()
    {
        var @case = await this.Tenant.AddCase(Day);
        var actA = await this.Tenant.AddAct(@case, Day);
        var actB = await this.Tenant.AddAct(@case, Day);
        var uploaded = await this.writer.UploadActFile(actA.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.DeleteActFile(actB.Id, uploaded.File!.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.NotFound));
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.File.FileId), Is.True);
        }
    }

    [Test]
    public async Task ACaseFileAndAnActFileNeverDeleteEachOther()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var caseFile = await this.writer.UploadCaseFile(@case.Id, Upload("spis.txt"), CancellationToken.None);
        var actFile = await this.writer.UploadActFile(act.Id, Upload("ukon.txt"), CancellationToken.None);

        var actDeleteOutcome = await this.writer.DeleteActFile(act.Id, caseFile.File!.FileId, CancellationToken.None);
        var caseDeleteOutcome = await this.writer.DeleteCaseFile(@case.Id, actFile.File!.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actDeleteOutcome, Is.EqualTo(DeleteOutcome.NotFound), "an act must not delete a file of its case");
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == caseFile.File.FileId), Is.True, "an act must not delete a file of its case");
            Assert.That(caseDeleteOutcome, Is.EqualTo(DeleteOutcome.NotFound), "a case must not delete a file of its act");
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == actFile.File.FileId), Is.True, "a case must not delete a file of its act");
        }
    }

    [Test]
    public async Task ARenamedCaseFileKeepsItsBytesAndTakesTheNewName()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("stary.txt", content: "abc"), CancellationToken.None);
        var before = await this.CurrentVersion(uploaded.File!.FileId);

        var outcome = await this.writer.RenameCaseFile(@case.Id, uploaded.File.FileId, "novy.txt", CancellationToken.None);

        var after = await this.Tenant.Context.FileAssets.AsNoTracking().SingleAsync(file => file.Id == uploaded.File.FileId);
        var versionAfter = await this.CurrentVersion(uploaded.File.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.Renamed));
            Assert.That(after.FileName, Is.EqualTo("novy.txt"));
            Assert.That(versionAfter.StoragePath, Is.EqualTo(before.StoragePath), "a rename never moves the blob");
            Assert.That(versionAfter.ContentHash, Is.EqualTo(before.ContentHash));
        }
    }

    [Test]
    public async Task ARenamedActFileTakesTheNewName()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var uploaded = await this.writer.UploadActFile(act.Id, Upload("stary.txt"), CancellationToken.None);

        var outcome = await this.writer.RenameActFile(act.Id, uploaded.File!.FileId, "novy.txt", CancellationToken.None);

        var after = await this.Tenant.Context.FileAssets.AsNoTracking().SingleAsync(file => file.Id == uploaded.File.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.Renamed));
            Assert.That(after.FileName, Is.EqualTo("novy.txt"));
        }
    }

    [Test]
    public async Task AFileOfAnotherCaseIsNotRenamed()
    {
        var caseA = await this.Tenant.AddCase(Day);
        var caseB = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(caseA.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.RenameCaseFile(caseB.Id, uploaded.File!.FileId, "b.txt", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.NotFound), "a file of another case must not be found for a rename");
            Assert.That((await this.Reload(uploaded.File.FileId)).FileName, Is.EqualTo("a.txt"));
        }
    }

    [Test]
    public async Task AFileOfAnotherActIsNotRenamed()
    {
        var @case = await this.Tenant.AddCase(Day);
        var actA = await this.Tenant.AddAct(@case, Day);
        var actB = await this.Tenant.AddAct(@case, Day);
        var uploaded = await this.writer.UploadActFile(actA.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.RenameActFile(actB.Id, uploaded.File!.FileId, "b.txt", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.NotFound));
            Assert.That((await this.Reload(uploaded.File.FileId)).FileName, Is.EqualTo("a.txt"));
        }
    }

    [Test]
    public async Task AFileOfAnotherTenantsActIsNotRenamed()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        var otherFile = await other.AddActFile(otherAct);
        var name = otherFile.FileName;

        var outcome = await this.writer.RenameActFile(otherAct.Id, otherFile.Id, "b.txt", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.NotFound), "the act id alone never reaches another tenant's file");
            Assert.That((await other.Context.FileAssets.AsNoTracking().SingleAsync(file => file.Id == otherFile.Id)).FileName, Is.EqualTo(name));
        }
    }

    [Test]
    public async Task RenamingAMissingFileIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.RenameCaseFile(@case.Id, Guid.CreateVersion7(), "a.txt", CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(FileRenameOutcome.NotFound));
    }

    [Test]
    public async Task AnUploadOfATakenNameAddsAVersionToThatFile()
    {
        var @case = await this.Tenant.AddCase(Day);
        var first = await this.writer.UploadCaseFile(@case.Id, Upload("smlouva.pdf", "application/pdf", "abc"), CancellationToken.None);

        var second = await this.writer.UploadCaseFile(@case.Id, Upload("SMLOUVA.PDF", "application/pdf", "abd"), CancellationToken.None);

        var files = await this.Tenant.Context.FileAssets.Where(file => file.CaseId == @case.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.Outcome, Is.EqualTo(UploadFileOutcome.VersionAdded));
            Assert.That(second.File?.FileId, Is.EqualTo(first.File!.FileId), "a taken name, ignoring case, is the same file");
            Assert.That(second.File?.Version, Is.EqualTo(2));
            Assert.That(second.File?.VersionCount, Is.EqualTo(2));
            Assert.That(files.Select(static file => file.FileName), Is.EqualTo(["smlouva.pdf"]), "the file keeps the name it was first uploaded under");
        }
    }

    [Test]
    public async Task AnUploadMatchesANameOnlyOnItsOwnCaseOrAct()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var otherCase = await this.Tenant.AddCase(Day);

        var onCase = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);
        var onAct = await this.writer.UploadActFile(act.Id, Upload("a.txt"), CancellationToken.None);
        var onOtherCase = await this.writer.UploadCaseFile(otherCase.Id, Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(onCase.Outcome, Is.EqualTo(UploadFileOutcome.Uploaded));
            Assert.That(onAct.Outcome, Is.EqualTo(UploadFileOutcome.Uploaded), "the acts of a case do not share its names");
            Assert.That(onOtherCase.Outcome, Is.EqualTo(UploadFileOutcome.Uploaded), "another case does not share the names");
        }
    }

    [Test]
    public async Task AnUploadEqualToTheCurrentVersionStoresNoVersion()
    {
        var @case = await this.Tenant.AddCase(Day);
        var first = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "abc"), CancellationToken.None);

        var second = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "abc"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.Outcome, Is.EqualTo(UploadFileOutcome.Unchanged));
            Assert.That(second.File?.FileId, Is.EqualTo(first.File!.FileId));
            Assert.That(second.File?.Version, Is.EqualTo(1));
            Assert.That(await this.VersionNumbers(first.File.FileId), Is.EqualTo([1]), "the same content stores no version");
        }
    }

    [Test]
    public async Task AnUploadEqualToAnOlderVersionIsANewVersion()
    {
        var @case = await this.Tenant.AddCase(Day);
        await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "abc"), CancellationToken.None);
        await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "abd"), CancellationToken.None);

        var third = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "abc"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(third.Outcome, Is.EqualTo(UploadFileOutcome.VersionAdded), "only the current version decides whether the content changed");
            Assert.That(third.File?.Version, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task ANewVersionKeepsTheFileNameAndTakesTheNextNumber()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("smlouva.pdf", "application/pdf", "abc"), CancellationToken.None);

        var result = await this.writer.UploadFileVersion(uploaded.File!.FileId, Upload("jiny.PDF", "application/x-test", "abd"), CancellationToken.None);

        var stored = await this.Reload(uploaded.File.FileId);
        var version = await this.CurrentVersion(uploaded.File.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(FileVersionUploadOutcome.VersionAdded));
            Assert.That(result.File?.Version, Is.EqualTo(2));
            Assert.That(stored.FileName, Is.EqualTo("smlouva.pdf"), "a new version does not use the name it arrived under");
            Assert.That(version.Number, Is.EqualTo(2));
            Assert.That(version.MediaType, Is.EqualTo("application/x-test"), "the media type comes from the upload that made the version");
        }
    }

    [Test]
    public async Task ANewVersionOfAnotherExtensionIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("smlouva.pdf", content: "abc"), CancellationToken.None);

        var result = await this.writer.UploadFileVersion(uploaded.File!.FileId, Upload("smlouva.docx", content: "abd"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(FileVersionUploadOutcome.ExtensionMismatch));
            Assert.That(await this.VersionNumbers(uploaded.File.FileId), Is.EqualTo([1]));
            Assert.That(this.blobs.Written, Has.Count.EqualTo(1), "a refused version writes no blob");
        }
    }

    [Test]
    public async Task ANewVersionOfAMissingFileIsNotFound()
    {
        var result = await this.writer.UploadFileVersion(Guid.CreateVersion7(), Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(FileVersionUploadOutcome.FileNotFound));
            Assert.That(result.File, Is.Null);
            Assert.That(this.blobs.Written, Is.Empty, "a refused version writes no blob");
        }
    }

    [Test]
    public async Task ANewVersionOfAnotherTenantsFileIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase, "cizi.txt");

        var result = await this.writer.UploadFileVersion(otherFile.Id, Upload("cizi.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(FileVersionUploadOutcome.FileNotFound), "the file id alone never reaches another tenant's file");
            Assert.That(this.blobs.Written, Is.Empty);
        }
    }

    [Test]
    public async Task ANewVersionEqualToTheCurrentIsUnchanged()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "abc"), CancellationToken.None);

        var result = await this.writer.UploadFileVersion(uploaded.File!.FileId, Upload("a.txt", content: "abc"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(FileVersionUploadOutcome.Unchanged));
            Assert.That(result.File?.Version, Is.EqualTo(1));
            Assert.That(await this.VersionNumbers(uploaded.File.FileId), Is.EqualTo([1]));
        }
    }

    [Test]
    public async Task AnotherUserOfTheTenantVersionsRenamesAndDeletesAFileTheyDidNotUpload()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("smlouva.pdf", content: "abc"), CancellationToken.None);
        var fileId = uploaded.File!.FileId;
        var colleague = await this.Tenant.AddUser();

        using (this.Tenant.EnterAs(colleague.Id))
        {
            var added = await this.writer.UploadFileVersion(fileId, Upload("smlouva.pdf", content: "abd"), CancellationToken.None);
            var uploader = (await this.CurrentVersion(fileId)).UserId;
            var renamed = await this.writer.RenameCaseFile(@case.Id, fileId, "nove.pdf", CancellationToken.None);
            var versionDeleted = await this.writer.DeleteFileVersion(fileId, 1, CancellationToken.None);
            var fileDeleted = await this.writer.DeleteCaseFile(@case.Id, fileId, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(added.Outcome, Is.EqualTo(FileVersionUploadOutcome.VersionAdded));
                Assert.That(uploader, Is.EqualTo(colleague.Id), "the version carries who uploaded it");
                Assert.That(renamed, Is.EqualTo(FileRenameOutcome.Renamed));
                Assert.That(versionDeleted, Is.EqualTo(FileVersionDeleteOutcome.Deleted));
                Assert.That(fileDeleted, Is.EqualTo(DeleteOutcome.Deleted));
            }
        }
    }

    [Test]
    public async Task DeletingAVersionKeepsTheOthersAndTheirNumbers()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "a"), CancellationToken.None);
        var fileId = uploaded.File!.FileId;
        await this.writer.UploadFileVersion(fileId, Upload("a.txt", content: "b"), CancellationToken.None);
        await this.writer.UploadFileVersion(fileId, Upload("a.txt", content: "c"), CancellationToken.None);

        var outcome = await this.writer.DeleteFileVersion(fileId, 2, CancellationToken.None);

        var item = await this.ListItem(fileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileVersionDeleteOutcome.Deleted));
            Assert.That(await this.VersionNumbers(fileId), Is.EqualTo([1, 3]), "the other versions keep their numbers");
            Assert.That(item.Version, Is.EqualTo(3));
        }
    }

    [Test]
    public async Task DeletingTheCurrentVersionMakesThePreviousCurrent()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "a.pdf", sizeBytes: 1);
        await this.Tenant.AddFileVersion(file, sizeBytes: 2);

        var outcome = await this.writer.DeleteFileVersion(file.Id, 2, CancellationToken.None);

        var item = await this.ListItem(file.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileVersionDeleteOutcome.Deleted));
            Assert.That(item.Version, Is.EqualTo(1));
            Assert.That(item.VersionCount, Is.EqualTo(1));
            Assert.That(item.SizeBytes, Is.EqualTo(1), "the file shows the size of the version that is current now");
        }
    }

    [Test]
    public async Task TheOnlyVersionIsNotDeletedAlone()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.DeleteFileVersion(uploaded.File!.FileId, 1, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileVersionDeleteOutcome.LastVersion));
            Assert.That(await this.VersionNumbers(uploaded.File.FileId), Is.EqualTo([1]), "a file without a version is deleted only with the file");
        }
    }

    [Test]
    public async Task DeletingAMissingVersionIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);
        await this.writer.UploadFileVersion(uploaded.File!.FileId, Upload("a.txt", content: "b"), CancellationToken.None);
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase);

        var unknownNumber = await this.writer.DeleteFileVersion(uploaded.File.FileId, 9, CancellationToken.None);
        var unknownFile = await this.writer.DeleteFileVersion(Guid.CreateVersion7(), 1, CancellationToken.None);
        var anotherTenants = await this.writer.DeleteFileVersion(otherFile.Id, 1, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unknownNumber, Is.EqualTo(FileVersionDeleteOutcome.NotFound));
            Assert.That(unknownFile, Is.EqualTo(FileVersionDeleteOutcome.NotFound));
            Assert.That(anotherTenants, Is.EqualTo(FileVersionDeleteOutcome.NotFound), "the file id alone never reaches another tenant's file");
        }
    }

    [Test]
    public async Task ADeletedVersionLeavesItsBlobOnDisk()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "a"), CancellationToken.None);
        var fileId = uploaded.File!.FileId;
        await this.writer.UploadFileVersion(fileId, Upload("a.txt", content: "b"), CancellationToken.None);
        var storagePath = (await this.CurrentVersion(fileId)).StoragePath;

        await this.writer.DeleteFileVersion(fileId, 2, CancellationToken.None);

        Assert.That(this.blobs.WrittenByPath, Contains.Key(storagePath), "a deleted version's bytes stay on disk");
    }

    [Test]
    public async Task ANewVersionAfterDeletingTheCurrentTakesTheNumberAfterTheHighestLeft()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "a"), CancellationToken.None);
        var fileId = uploaded.File!.FileId;
        await this.writer.UploadFileVersion(fileId, Upload("a.txt", content: "b"), CancellationToken.None);
        await this.writer.DeleteFileVersion(fileId, 2, CancellationToken.None);

        var result = await this.writer.UploadFileVersion(fileId, Upload("a.txt", content: "c"), CancellationToken.None);

        Assert.That(result.File?.Version, Is.EqualTo(2), "the number is the highest left plus one, not a counter");
    }

    [Test]
    public async Task DeletingAFileDeletesAllItsVersions()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "a"), CancellationToken.None);
        var fileId = uploaded.File!.FileId;
        await this.writer.UploadFileVersion(fileId, Upload("a.txt", content: "b"), CancellationToken.None);

        await this.writer.DeleteCaseFile(@case.Id, fileId, CancellationToken.None);

        Assert.That(await this.Tenant.Context.FileVersions.AnyAsync(version => version.FileAssetId == fileId), Is.False, "the versions go with the file");
    }

    [Test]
    public async Task RenamingOntoANameAnotherFileCarriesIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);
        var second = await this.writer.UploadCaseFile(@case.Id, Upload("b.txt"), CancellationToken.None);

        var outcome = await this.writer.RenameCaseFile(@case.Id, second.File!.FileId, "A.TXT", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.NameTaken), "a name is taken whatever its case");
            Assert.That((await this.Reload(second.File.FileId)).FileName, Is.EqualTo("b.txt"));
        }
    }

    [Test]
    public async Task RenamingOnlyTheCaseOfItsOwnNameIsAllowed()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.RenameCaseFile(@case.Id, uploaded.File!.FileId, "A.TXT", CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileRenameOutcome.Renamed));
            Assert.That((await this.Reload(uploaded.File.FileId)).FileName, Is.EqualTo("A.TXT"));
        }
    }

    [Test]
    public async Task RenamingOntoTheNameOfAFileOfAnotherOwnerIsAllowed()
    {
        var caseA = await this.Tenant.AddCase(Day);
        var caseB = await this.Tenant.AddCase(Day);
        await this.writer.UploadCaseFile(caseA.Id, Upload("a.txt"), CancellationToken.None);
        var other = await this.writer.UploadCaseFile(caseB.Id, Upload("b.txt"), CancellationToken.None);

        var outcome = await this.writer.RenameCaseFile(caseB.Id, other.File!.FileId, "a.txt", CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(FileRenameOutcome.Renamed), "a name is unique within one case or act only");
    }

    [Test]
    public async Task TwoConcurrentUploadsOfOneNameEndAsOneFileWithTwoVersions()
    {
        var @case = await this.Tenant.AddCase(Day);
        var store = new MeetingBlobStore(parties: 2);
        await using var second = TestDatabase.CreateMigratedAsHost(this.Tenant.UserContext);
        var first = this.WriterOver(this.Tenant.Context, store);
        var other = this.WriterOver(second, store);

        var results = await Task.WhenAll(
            first.UploadCaseFile(@case.Id, Upload("smlouva.pdf", content: "a"), CancellationToken.None),
            other.UploadCaseFile(@case.Id, Upload("Smlouva.pdf", content: "b"), CancellationToken.None));

        var files = await this.Tenant.Context.FileAssets.Where(file => file.CaseId == @case.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(files, Has.Count.EqualTo(1), "the losing upload of a name joins the winner's file");
            Assert.That(await this.VersionNumbers(files[0].Id), Is.EqualTo([1, 2]));
            Assert.That(results.Select(static result => result.Outcome), Is.EquivalentTo([UploadFileOutcome.Uploaded, UploadFileOutcome.VersionAdded]));
        }
    }

    [Test]
    public async Task TwoConcurrentVersionsOfOneFileTakeTheNextTwoNumbers()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt", content: "a"), CancellationToken.None);
        var fileId = uploaded.File!.FileId;
        var store = new MeetingBlobStore(parties: 2);
        await using var second = TestDatabase.CreateMigratedAsHost(this.Tenant.UserContext);
        var first = this.WriterOver(this.Tenant.Context, store);
        var other = this.WriterOver(second, store);

        var results = await Task.WhenAll(
            first.UploadFileVersion(fileId, Upload("a.txt", content: "b"), CancellationToken.None),
            other.UploadFileVersion(fileId, Upload("a.txt", content: "c"), CancellationToken.None));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.VersionNumbers(fileId), Is.EqualTo([1, 2, 3]), "the losing version takes the next number");
            Assert.That(results.Select(static result => result.Outcome), Is.All.EqualTo(FileVersionUploadOutcome.VersionAdded));
        }
    }

    private static FileUpload Upload(string fileName, string mediaType = "text/plain", string content = "a")
    {
        return new FileUpload { FileName = fileName, MediaType = mediaType, Content = new MemoryStream(Encoding.UTF8.GetBytes(content)) };
    }

    private async Task<FileAsset> Reload(Guid fileAssetId)
    {
        return await this.Tenant.Context.FileAssets.SingleAsync(file => file.Id == fileAssetId);
    }

    private async Task<FileVersion> CurrentVersion(Guid fileAssetId)
    {
        return await this.Tenant.Context.FileVersions
            .Where(version => version.FileAssetId == fileAssetId)
            .OrderByDescending(static version => version.Number)
            .FirstAsync();
    }

    private async Task<List<int>> VersionNumbers(Guid fileAssetId)
    {
        return await this.Tenant.Context.FileVersions
            .Where(version => version.FileAssetId == fileAssetId)
            .OrderBy(static version => version.Number)
            .Select(static version => version.Number)
            .ToListAsync();
    }

    private async Task<FileListItem> ListItem(Guid fileAssetId)
    {
        return await this.Tenant.Context.FileAssets
            .WithId(fileAssetId)
            .AsListItems()
            .SingleAsync();
    }

    private FileWriter WriterOver(ApplicationDbContext context, IFileBlobStore store)
    {
        return new FileWriter(new FixedDbSession(context), store, this.Tenant.UserContext, NullLogger<FileWriter>.Instance);
    }

    private sealed class MeetingBlobStore(int parties) : IFileBlobStore
    {
        private readonly TaskCompletionSource meeting = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private int arrived;

        public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileVersionId, Stream content, CancellationToken token)
        {
            using var reader = new StreamReader(content, Encoding.UTF8);
            var bytes = Encoding.UTF8.GetBytes(await reader.ReadToEndAsync(token));

            if (Interlocked.Increment(ref this.arrived) == parties)
                this.meeting.SetResult();

            await this.meeting.Task;

            return new FileBlobInfo
            {
                StoragePath = $"{tenantId}/{fileVersionId}",
                ContentHash = Convert.ToHexStringLower(SHA256.HashData(bytes)),
                SizeBytes = bytes.Length,
            };
        }

        public Stream? ReadFileBlob(string storagePath)
        {
            return null;
        }
    }

    private sealed class RecordingTenantBlobStore : IFileBlobStore
    {
        public Guid WrittenUnderTenant { get; private set; }

        public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileVersionId, Stream content, CancellationToken token)
        {
            this.WrittenUnderTenant = tenantId;

            return new FileBlobInfo { StoragePath = $"{tenantId}/{fileVersionId}", ContentHash = new string('a', 64), SizeBytes = 1 };
        }

        public Stream? ReadFileBlob(string storagePath)
        {
            return null;
        }
    }
}
