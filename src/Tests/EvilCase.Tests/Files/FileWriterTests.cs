using System.Text;
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
public class FileWriterTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private TestTenant tenant = null!;

    private FakeFileBlobStore blobs = null!;

    private FileWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create(asHost: true);
        this.blobs = new FakeFileBlobStore();
        this.writer = new FileWriter(new FixedDbSession(this.tenant.Context), this.blobs, this.tenant.UserContext, NullLogger<FileWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task AnUploadedFileHangsOnItsCaseWithWhatTheStoreMeasured()
    {
        var @case = await this.tenant.AddCase(Day);

        var result = await this.writer.UploadCaseFile(@case.Id, Upload("smlouva.pdf", "application/pdf", "abc"), CancellationToken.None);

        Assert.That(result, Is.Not.Null);

        var stored = await this.Reload(result!.Id);

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
        var @case = await this.tenant.AddCase(Day);
        var recording = new RecordingTenantBlobStore();
        var recordingWriter = new FileWriter(new FixedDbSession(this.tenant.Context), recording, this.tenant.UserContext, NullLogger<FileWriter>.Instance);

        await recordingWriter.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);

        Assert.That(recording.WrittenUnderTenant, Is.EqualTo(this.tenant.UserContext.TenantId), "the upload writes the blob under the tenant IUserContext names");
    }

    [Test]
    public async Task AnUploadOntoAMissingCaseIsRefused()
    {
        var result = await this.writer.UploadCaseFile(Guid.CreateVersion7(), Upload("a.txt"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null, "an upload onto a case the tenant does not have must be refused");
            Assert.That(await this.tenant.Context.FileAssets.AnyAsync(), Is.False, "a refused upload must write no row");
            Assert.That(this.blobs.WrittenByPath, Is.Empty, "a refused upload must write no blob");
        }
    }

    [Test]
    public async Task ADeletedFileTakesItsBlobWithIt()
    {
        var @case = await this.tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(@case.Id, Upload("a.txt"), CancellationToken.None);
        var storagePath = (await this.Reload(uploaded!.Id)).StoragePath;

        var outcome = await this.writer.DeleteCaseFile(@case.Id, uploaded.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileDeleteOutcome.Deleted));
            Assert.That(await this.tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.Id), Is.False, "a deleted file leaves no row");
            Assert.That(this.blobs.Deleted, Does.Contain(storagePath), "a deleted file takes its blob with it");
        }
    }

    [Test]
    public async Task AFileOfAnotherCaseIsNotDeleted()
    {
        var caseA = await this.tenant.AddCase(Day);
        var caseB = await this.tenant.AddCase(Day);
        var uploaded = await this.writer.UploadCaseFile(caseA.Id, Upload("a.txt"), CancellationToken.None);

        var outcome = await this.writer.DeleteCaseFile(caseB.Id, uploaded!.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileDeleteOutcome.NotFound), "a file of another case must not be found for delete");
            Assert.That(await this.tenant.Context.FileAssets.AnyAsync(file => file.Id == uploaded.Id), Is.True, "the file must survive a delete naming the wrong case");
        }
    }

    [Test]
    public async Task DeletingAMissingFileIsNotFound()
    {
        var @case = await this.tenant.AddCase(Day);

        var outcome = await this.writer.DeleteCaseFile(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(FileDeleteOutcome.NotFound));
    }

    private static FileUpload Upload(string fileName, string mediaType = "text/plain", string content = "a")
    {
        return new FileUpload { FileName = fileName, MediaType = mediaType, Content = new MemoryStream(Encoding.UTF8.GetBytes(content)) };
    }

    private Task<FileAsset> Reload(Guid fileAssetId)
    {
        return this.tenant.Context.FileAssets.SingleAsync(file => file.Id == fileAssetId);
    }

    private sealed class RecordingTenantBlobStore : IFileBlobStore
    {
        public Guid WrittenUnderTenant { get; private set; }

        public Task<FileBlobInfo> WriteFileBlob(Guid tenantId, Guid fileAssetId, Stream content, CancellationToken token)
        {
            this.WrittenUnderTenant = tenantId;

            return Task.FromResult(new FileBlobInfo { StoragePath = $"{tenantId}/{fileAssetId}", ContentHash = new string('a', 64), SizeBytes = 1 });
        }

        public Stream? ReadFileBlob(string storagePath)
        {
            return null;
        }

        public Task DeleteFileBlob(string storagePath, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
