using System.Text;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

/// <summary>
/// Uploads and deletes a case's files against a real PostgreSQL. Each test seeds a tenant of its own, so
/// none cleans up after itself.
/// </summary>
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.CaseId, Is.EqualTo(@case.Id), "an uploaded file hangs on the case it was uploaded to");
            Assert.That(stored.ActId, Is.Null, "a file uploaded to a case carries no act");
            Assert.That(stored.FileName, Is.EqualTo("smlouva.pdf"), "an uploaded file keeps the name it arrived under");
            Assert.That(stored.MediaType, Is.EqualTo("application/pdf"), "an uploaded file keeps the media type it arrived under");
            Assert.That(stored.ContentHash, Has.Length.EqualTo(64), "the store's hash is what the row carries");
            Assert.That(this.blobs.WrittenByPath[stored.StoragePath], Is.EqualTo("abc"), "the row's storage path resolves to what the store wrote");
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
    public async Task ADeletedFileLeavesItsBlobBehind()
    {
        var @case = await this.Tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);
        var storagePath = (await this.Reload(uploaded.File!.FileId)).StoragePath;

        var outcome = await this.writer.DeleteCaseFile(@case.Id, uploaded.File.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.File.FileId), Is.False, "a deleted file is out of every read");
            Assert.That(this.blobs.Deleted, Does.Not.Contain(storagePath), "the bytes stay, so a stamped file is a file that can come back");
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

        var result = await this.writer.UploadActFile(@case.Id, act.Id, Upload("smlouva.pdf", "application/pdf", "abc"), CancellationToken.None);

        var stored = await this.Reload(result.File!.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.ActId, Is.EqualTo(act.Id), "an uploaded file hangs on the act it was uploaded to");
            Assert.That(stored.CaseId, Is.Null, "a file uploaded to an act carries no case");
            Assert.That(stored.FileName, Is.EqualTo("smlouva.pdf"));
            Assert.That(stored.MediaType, Is.EqualTo("application/pdf"));
            Assert.That(stored.ContentHash, Has.Length.EqualTo(64));
            Assert.That(this.blobs.WrittenByPath[stored.StoragePath], Is.EqualTo("abc"));
        }
    }

    [Test]
    public async Task AnUploadOntoAMissingActIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);

        var result = await this.writer.UploadActFile(@case.Id, Guid.CreateVersion7(), Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(UploadFileOutcome.OwnerNotFound));
            Assert.That(result.File, Is.Null);
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(), Is.False);
            Assert.That(this.blobs.WrittenByPath, Is.Empty);
        }
    }

    [Test]
    public async Task AnUploadOntoAnActOfAnotherCaseIsRefused()
    {
        var caseA = await this.Tenant.AddCase(Day);
        var caseB = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(caseA, Day);

        var result = await this.writer.UploadActFile(caseB.Id, act.Id, Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(UploadFileOutcome.OwnerNotFound), "an act reached through the wrong case must not take an upload");
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(), Is.False, "an act reached through the wrong case must not take an upload");
        }
    }

    [Test]
    public async Task ADeletedActFileLeavesItsBlobBehind()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var uploaded = await this.writer.UploadActFile(@case.Id, act.Id, Upload("a.txt"), CancellationToken.None);
        var storagePath = (await this.Reload(uploaded.File!.FileId)).StoragePath;

        var outcome = await this.writer.DeleteActFile(@case.Id, act.Id, uploaded.File.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.File.FileId), Is.False);
            Assert.That(this.blobs.Deleted, Does.Not.Contain(storagePath), "the bytes stay, so a stamped file is a file that can come back");
        }
    }

    [Test]
    public async Task AFileOfAnotherActIsNotDeleted()
    {
        var @case = await this.Tenant.AddCase(Day);
        var actA = await this.Tenant.AddAct(@case, Day);
        var actB = await this.Tenant.AddAct(@case, Day);
        var uploaded = await this.writer.UploadActFile(@case.Id, actA.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.DeleteActFile(@case.Id, actB.Id, uploaded.File!.FileId, CancellationToken.None);

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
        var actFile = await this.writer.UploadActFile(@case.Id, act.Id, Upload("ukon.txt"), CancellationToken.None);

        var actDeleteOutcome = await this.writer.DeleteActFile(@case.Id, act.Id, caseFile.File!.FileId, CancellationToken.None);
        var caseDeleteOutcome = await this.writer.DeleteCaseFile(@case.Id, actFile.File!.FileId, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actDeleteOutcome, Is.EqualTo(DeleteOutcome.NotFound), "an act must not delete a file of its case");
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == caseFile.File.FileId), Is.True, "an act must not delete a file of its case");
            Assert.That(caseDeleteOutcome, Is.EqualTo(DeleteOutcome.NotFound), "a case must not delete a file of its act");
            Assert.That(await this.Tenant.Context.FileAssets.AnyAsync(file => file.Id == actFile.File.FileId), Is.True, "a case must not delete a file of its act");
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

    private sealed class RecordingTenantBlobStore : IFileBlobStore
    {
        public Guid WrittenUnderTenant { get; private set; }

        public async Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token)
        {
            this.WrittenUnderTenant = tenantId;

            return new FileBlobInfo { StoragePath = $"{tenantId}/{fileAssetId}", ContentHash = new string('a', 64), SizeBytes = 1 };
        }

        public Stream? ReadFileBlob(string storagePath)
        {
            return null;
        }

        public async Task DeleteFileBlob(string storagePath, CancellationToken token)
        { }
    }
}
