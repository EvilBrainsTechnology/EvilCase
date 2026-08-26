using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Files;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileWriterTests
{
    [Test]
    public async Task AnUploadedFileHangsOnItsCaseWithWhatTheStoreMeasured()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var @case = await SeedCase(context, tenantId, userId);

        var blobs = new FakeFileBlobStore();
        var writer = new FileWriter(new FixedDbSession(context), blobs, userContext, NullLogger<FileWriter>.Instance);

        var upload = new FileUpload { FileName = "smlouva.pdf", MediaType = "application/pdf", Content = new MemoryStream("abc"u8.ToArray()) };

        var result = await writer.UploadCaseFile(@case.Id, upload, CancellationToken.None);

        Assert.That(result, Is.Not.Null);

        var stored = await context.FileAssets.SingleAsync(file => file.Id == result!.Id);
        var blobInfo = blobs.WrittenByPath[stored.StoragePath];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stored.CaseId, Is.EqualTo(@case.Id), "an uploaded file hangs on the case it was uploaded to");
            Assert.That(stored.ActId, Is.Null, "a file uploaded to a case carries no act");
            Assert.That(stored.FileName, Is.EqualTo("smlouva.pdf"), "an uploaded file keeps the name it arrived under");
            Assert.That(stored.MediaType, Is.EqualTo("application/pdf"), "an uploaded file keeps the media type it arrived under");
            Assert.That(stored.ContentHash, Has.Length.EqualTo(64), "the store's hash is what the row carries");
            Assert.That(blobInfo, Is.EqualTo("abc"), "the row's storage path resolves to what the store wrote");
        }
    }

    [Test]
    public async Task AnUploadNamesTheTenantTheStoreWritesUnder()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var @case = await SeedCase(context, tenantId, userId);

        var blobs = new RecordingTenantBlobStore();
        var writer = new FileWriter(new FixedDbSession(context), blobs, userContext, NullLogger<FileWriter>.Instance);

        await writer.UploadCaseFile(@case.Id, new FileUpload { FileName = "a.txt", MediaType = "text/plain", Content = new MemoryStream("a"u8.ToArray()) }, CancellationToken.None);

        Assert.That(blobs.WrittenUnderTenant, Is.EqualTo(tenantId), "the upload writes the blob under the tenant IUserContext names");
    }

    [Test]
    public async Task AnUploadOntoAMissingCaseIsRefused()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);

        var blobs = new FakeFileBlobStore();
        var writer = new FileWriter(new FixedDbSession(context), blobs, userContext, NullLogger<FileWriter>.Instance);

        var result = await writer.UploadCaseFile(Guid.CreateVersion7(), new FileUpload { FileName = "a.txt", MediaType = "text/plain", Content = new MemoryStream("a"u8.ToArray()) }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.Null, "an upload onto a case the tenant has no such case must be refused");
            Assert.That(await context.FileAssets.AnyAsync(), Is.False, "a refused upload must write no row");
            Assert.That(blobs.WrittenByPath, Is.Empty, "a refused upload must write no blob");
        }
    }

    [Test]
    public async Task ADeletedFileTakesItsBlobWithIt()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var @case = await SeedCase(context, tenantId, userId);

        var blobs = new FakeFileBlobStore();
        var writer = new FileWriter(new FixedDbSession(context), blobs, userContext, NullLogger<FileWriter>.Instance);

        var uploaded = await writer.UploadCaseFile(@case.Id, new FileUpload { FileName = "a.txt", MediaType = "text/plain", Content = new MemoryStream("a"u8.ToArray()) }, CancellationToken.None);
        var storagePath = (await context.FileAssets.SingleAsync(file => file.Id == uploaded!.Id)).StoragePath;

        var outcome = await writer.DeleteCaseFile(@case.Id, uploaded!.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileDeleteOutcome.Deleted));
            Assert.That(await context.FileAssets.AnyAsync(file => file.Id == uploaded.Id), Is.False, "a deleted file leaves no row");
            Assert.That(blobs.Deleted, Does.Contain(storagePath), "a deleted file takes its blob with it");
        }
    }

    [Test]
    public async Task AFileOfAnotherCaseIsNotDeleted()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var caseA = await SeedCase(context, tenantId, userId, "EC/20260821-001");
        var caseB = await SeedCase(context, tenantId, userId, "EC/20260821-002");

        var blobs = new FakeFileBlobStore();
        var writer = new FileWriter(new FixedDbSession(context), blobs, userContext, NullLogger<FileWriter>.Instance);

        var uploaded = await writer.UploadCaseFile(caseA.Id, new FileUpload { FileName = "a.txt", MediaType = "text/plain", Content = new MemoryStream("a"u8.ToArray()) }, CancellationToken.None);

        var outcome = await writer.DeleteCaseFile(caseB.Id, uploaded!.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(FileDeleteOutcome.NotFound), "a file of another case must not be found for delete");
            Assert.That(await context.FileAssets.AnyAsync(file => file.Id == uploaded.Id), Is.True, "the file must survive a delete naming the wrong case");
        }
    }

    [Test]
    public async Task DeletingAMissingFileIsNotFound()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var @case = await SeedCase(context, tenantId, userId);

        var writer = new FileWriter(new FixedDbSession(context), new FakeFileBlobStore(), userContext, NullLogger<FileWriter>.Instance);

        var outcome = await writer.DeleteCaseFile(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(FileDeleteOutcome.NotFound));
    }

    private static async Task<Case> SeedCase(ApplicationDbContext context, Guid tenantId, Guid userId, string caseNumber = "EC/20260821-001")
    {
        var account = new Account { Name = "file writer" };
        var tenant = new Tenant { Id = tenantId, AccountId = account.Id, Name = "tenant" };
        var @case = new Case
        {
            TenantId = tenantId,
            UserId = userId,
            CaseNumber = caseNumber,
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        context.Accounts.Add(account);
        context.Tenants.Add(tenant);
        context.Cases.Add(@case);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        return @case;
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
