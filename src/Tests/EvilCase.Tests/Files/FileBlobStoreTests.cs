using EvilBrains.EvilCase.Files;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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
        this.store = new FileBlobStore(Options.Create(new FileSettings { RootPath = this.root }), NullLogger<FileBlobStore>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(this.root))
            Directory.Delete(this.root, recursive: true);
    }

    [Test]
    public async Task AWrittenBlobLandsAtItsStoragePath()
    {
        var content = "abc"u8.ToArray();

        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream(content));
        var path = Path.Combine(this.root, info.StoragePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(path), Is.True, "the blob must land under its returned storage path");
            Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(content), "the written bytes must equal what went in");
        }
    }

    [Test]
    public async Task TheWriteReturnsAPathRelativeToTheRoot()
    {
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream("abc"u8.ToArray()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Path.IsPathRooted(info.StoragePath), Is.False, "a stored absolute path would pin the blobs to one machine");
            Assert.That(File.Exists(Path.Combine(this.root, info.StoragePath)), Is.True);
        }
    }

    [Test]
    public async Task TheBlobLandsUnderTwoDirectoryLevels()
    {
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream("abc"u8.ToArray()));
        var path = Path.Combine(this.root, info.StoragePath);

        var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
        var hex = this.fileAssetId.ToString("N", CultureInfo.InvariantCulture);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(directory.Name, Is.EqualTo(hex[^4..^2]), "one directory holding every blob is what the fan-out exists to prevent");
            Assert.That(directory.Parent?.Name, Is.EqualTo(hex[^2..]), "one directory holding every blob is what the fan-out exists to prevent");
        }
    }

    [Test]
    public async Task TheBlobKeepsTheExtensionOfTheUploadedName()
    {
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.PDF", new MemoryStream("abc"u8.ToArray()));

        Assert.That(info.StoragePath, Does.EndWith($"{this.fileAssetId:D}.PDF"), "the stored blob must carry the uploaded name's extension");
    }

    [Test]
    public async Task AnExtensionlessNameLeavesTheBlobExtensionless()
    {
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol", new MemoryStream("abc"u8.ToArray()));

        Assert.That(info.StoragePath, Does.EndWith($"{this.fileAssetId:D}"), "a name with no extension must store no extension");
    }

    [Test]
    public async Task AHostileNameStaysInsideTheTenantDirectory()
    {
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "../../evil.sh", new MemoryStream("abc"u8.ToArray()));
        var path = Path.Combine(this.root, info.StoragePath);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Path.GetFullPath(path), Does.StartWith(Path.GetFullPath(this.root) + Path.DirectorySeparatorChar), "a hostile name must never move the blob outside the storage root");
            Assert.That(File.Exists(path), Is.True, "the blob must still land at its computed path");
        }
    }

    [Test]
    public async Task AWriteReturnsTheSizeAndTheSha256OfWhatItWrote()
    {
        var content = "abc"u8.ToArray();

        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream(content));

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

        await Assert.ThatAsync(() => this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", failing), Throws.InstanceOf<IOException>());

        var storagePath = FileBlobPathFor(this.tenantId, this.fileAssetId) + ".pdf";
        var directory = Path.GetDirectoryName(Path.Combine(this.root, storagePath));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(Path.Combine(this.root, storagePath)), Is.False, "a failed write must not leave a blob at the final path");
            Assert.That(Directory.Exists(directory) ? Directory.GetFiles(directory) : [], Is.Empty, "a failed write must leave no temporary file behind");
        }
    }

    [Test]
    public async Task AWriteAfterAFailedOneStillLands()
    {
        var failing = new FailingStream("ab"u8.ToArray());
        await Assert.ThatAsync(() => this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", failing), Throws.InstanceOf<IOException>());

        var content = "abc"u8.ToArray();
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream(content));

        var path = Path.Combine(this.root, info.StoragePath);
        var bytesOnDisk = await File.ReadAllBytesAsync(path);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(File.Exists(path), Is.True, "a stale temp file must not block the retry");
            Assert.That(bytesOnDisk, Is.EqualTo(content), "the retry must write the new content");
            Assert.That(info.ContentHash, Is.EqualTo(ContentHash), "the retry must return the hash of the new content");
        }
    }

    [Test]
    public async Task AWriteLeavesABlobNamedLikeATemporaryFileAlone()
    {
        var stored = "abc"u8.ToArray();
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.tmp", new MemoryStream(stored));
        var path = Path.Combine(this.root, info.StoragePath);

        var failing = new FailingStream("ab"u8.ToArray());
        await Assert.ThatAsync(() => this.store.Write(this.tenantId, this.fileAssetId, "protokol", failing), Throws.InstanceOf<IOException>());

        Assert.That(File.Exists(path) ? await File.ReadAllBytesAsync(path) : [], Is.EqualTo(stored), "a temporary file must never take the name a stored blob can have");
    }

    [Test]
    public async Task TheDeleteFollowsTheStoredPath()
    {
        var info = await this.store.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream("abc"u8.ToArray()));
        var path = Path.Combine(this.root, info.StoragePath);

        await this.store.Delete(info.StoragePath);

        Assert.That(File.Exists(path), Is.False, "the blob must be gone from disk");

        Assert.DoesNotThrowAsync(() => this.store.Delete(info.StoragePath), "deleting a missing blob must not throw");
    }

    [Test]
    public async Task ARootWrittenWithATrailingSeparatorStillTakesBlobs()
    {
        var settings = new FileSettings { RootPath = this.root + Path.DirectorySeparatorChar };
        var trailing = new FileBlobStore(Options.Create(settings), NullLogger<FileBlobStore>.Instance);

        var info = await trailing.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream("abc"u8.ToArray()));

        Assert.That(File.Exists(Path.Combine(this.root, info.StoragePath)), Is.True, "a separator the operator typed must not make every path leave the root");
    }

    [Test]
    public async Task APathLeavingTheRootIsRefused()
    {
        await Assert.ThatAsync(() => this.store.Delete("../outside"), Throws.ArgumentException, "a path read back from the database must not reach outside the root");
    }

    [Test]
    public async Task ARelativeRootResolvesAgainstTheApplicationDirectory()
    {
        var relativeRoot = "files-" + Guid.CreateVersion7().ToString("N", CultureInfo.InvariantCulture);
        var fullRoot = Path.Combine(AppContext.BaseDirectory, relativeRoot);

        try
        {
            var relative = new FileBlobStore(Options.Create(new FileSettings { RootPath = relativeRoot }), NullLogger<FileBlobStore>.Instance);

            var info = await relative.Write(this.tenantId, this.fileAssetId, "protokol.pdf", new MemoryStream("abc"u8.ToArray()));

            Assert.That(File.Exists(Path.Combine(fullRoot, info.StoragePath)), Is.True, "a relative root must resolve against the application directory, not the working directory");
        }
        finally
        {
            if (Directory.Exists(fullRoot))
                Directory.Delete(fullRoot, recursive: true);
        }
    }

    private static string FileBlobPathFor(in Guid tenantId, in Guid fileAssetId)
    {
        var hex = fileAssetId.ToString("N", CultureInfo.InvariantCulture);

        return $"{tenantId:D}/{hex[^2..]}/{hex[^4..^2]}/{fileAssetId:D}";
    }
}
