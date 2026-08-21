using EvilBrains.EvilCase.Data.Files;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileBlobStoreTests
{
    private const string ContentHash = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";

    private readonly Guid tenantId = Guid.CreateVersion7();

    private readonly Guid fileAssetId = Guid.CreateVersion7();

    private string root = "";

    private FileBlobStore store = null!;

    [SetUp]
    public void SetUp()
    {
        this.root = Path.Combine(Path.GetTempPath(), "evilcase-files-" + Guid.CreateVersion7().ToString("N", CultureInfo.InvariantCulture));
        this.store = new FileBlobStore(new FileStorageSettings(this.root), NullLogger<FileBlobStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this.root))
            Directory.Delete(this.root, recursive: true);
    }

    [Test]
    public async Task AWrittenBlobLandsUnderItsTenantAndItsId()
    {
        var content = "abc"u8.ToArray();

        await this.store.Write(this.tenantId, this.fileAssetId, new MemoryStream(content));

        var path = this.BlobPath();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(path), Is.True, "the blob must land at {root}/{tenantId}/{fileAssetId}");
            Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(content), "the written bytes must equal what went in");
        }
    }

    [Test]
    public async Task AWriteReturnsTheSizeAndTheSha256OfWhatItWrote()
    {
        var content = "abc"u8.ToArray();

        var info = await this.store.Write(this.tenantId, this.fileAssetId, new MemoryStream(content));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.SizeBytes, Is.EqualTo(3), "the returned size must equal the byte count written");
            Assert.That(info.ContentHash, Is.EqualTo(ContentHash), "the returned hash must equal the SHA-256 of what was written");
        }
    }

    [Test]
    public async Task AFailedWriteLeavesNoBlob()
    {
        var failing = new FailingStream("ab"u8.ToArray());

        await Assert.ThatAsync(() => this.store.Write(this.tenantId, this.fileAssetId, failing), Throws.InstanceOf<IOException>());

        Assert.That(File.Exists(this.BlobPath()), Is.False, "a failed write must not leave a blob at the final path");
    }

    [Test]
    public async Task AWriteAfterAFailedOneStillLands()
    {
        var failing = new FailingStream("ab"u8.ToArray());
        await Assert.ThatAsync(() => this.store.Write(this.tenantId, this.fileAssetId, failing), Throws.InstanceOf<IOException>());

        var content = "abc"u8.ToArray();
        var info = await this.store.Write(this.tenantId, this.fileAssetId, new MemoryStream(content));

        var path = this.BlobPath();
        var bytesOnDisk = await File.ReadAllBytesAsync(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(path), Is.True, "a stale temp file must not block the retry");
            Assert.That(bytesOnDisk, Is.EqualTo(content), "the retry must write the new content");
            Assert.That(info.ContentHash, Is.EqualTo(ContentHash), "the retry must return the hash of the new content");
        }
    }

    [Test]
    public async Task DeletingABlobRemovesItFromDisk()
    {
        await this.store.Write(this.tenantId, this.fileAssetId, new MemoryStream("abc"u8.ToArray()));

        var deleted = this.store.Delete(this.tenantId, this.fileAssetId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.True, "deleting an existing blob must return true");
            Assert.That(File.Exists(this.BlobPath()), Is.False, "the blob must be gone from disk");
        }
    }

    [Test]
    public void DeletingABlobThatIsNotThereIsNotAnError()
    {
        var deleted = false;

        Assert.DoesNotThrow(() => deleted = this.store.Delete(this.tenantId, this.fileAssetId), "deleting a missing blob must not throw");
        Assert.That(deleted, Is.False, "deleting a missing blob must return false");
    }

    private string BlobPath() =>
        Path.Combine(
            this.root,
            this.tenantId.ToString("D", CultureInfo.InvariantCulture),
            this.fileAssetId.ToString("D", CultureInfo.InvariantCulture));
}
