using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileArchiveReaderTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private FakeFileBlobStore blobs = null!;

    private FileArchiveReader reader = null!;

    [SetUp]
    public void SetUpReader()
    {
        this.blobs = new FakeFileBlobStore();
        this.reader = new FileArchiveReader(this.Tenant.Session, this.blobs, NullLogger<FileArchiveReader>.Instance);
    }

    [Test]
    public async Task AnActArchiveHoldsTheCurrentVersionsOfTheActOwnFilesOnly()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, title: "Přestupek");
        var otherAct = await this.Tenant.AddAct(@case, Day);
        var smlouva = this.Stored(await this.Tenant.AddActFile(act, "Smlouva.pdf"), "smlouva 1");
        this.Stored(await this.Tenant.AddFileVersion(smlouva), "smlouva 2");
        this.Stored(await this.Tenant.AddActFile(act, "Plná moc.pdf"), "plna moc");
        this.Stored(await this.Tenant.AddActFile(otherAct, "Jiný úkon.pdf"), "jiny ukon");

        var archive = await this.reader.ReadActFileArchive(act.Id, includeVersions: false, CancellationToken.None);
        var entries = await this.Unpack(archive!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(archive!.FileName, Is.EqualTo("Přestupek.zip"));
            Assert.That(entries.Keys, Is.EqualTo(["Smlouva.pdf", "Plná moc.pdf"]), "never the files of another act");
            Assert.That(entries["Smlouva.pdf"], Is.EqualTo("smlouva 2"), "the current version");
        }
    }

    [Test]
    public async Task AnArchiveWithVersionsHoldsEveryVersionStampedWithItsUpload()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day, title: "Přestupek");
        var smlouva = this.Stored(await this.Tenant.AddActFile(act, "Smlouva.pdf"), "smlouva 1");
        this.Stored(await this.Tenant.AddFileVersion(smlouva), "smlouva 2");

        var archive = await this.reader.ReadActFileArchive(act.Id, includeVersions: true, CancellationToken.None);
        var target = new AsyncOnlyStream();
        await this.reader.WriteFileArchive(archive!, target, CancellationToken.None);

        var uploads = await this.Tenant.Context.FileVersions
            .Where(version => version.FileAssetId == smlouva.Id)
            .OrderBy(static version => version.Number)
            .Select(static version => version.Created)
            .ToListAsync();

        await using var zip = new ZipArchive(new MemoryStream(target.Written()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(archive!.FileName, Is.EqualTo("Přestupek (with versions).zip"));
            Assert.That(zip.Entries.Select(static entry => entry.FullName), Is.EqualTo(["Smlouva (version 1).pdf", "Smlouva.pdf"]));
            Assert.That(zip.Entries[0].LastWriteTime.DateTime, Is.EqualTo(PragueTime(uploads[0])).Within(TimeSpan.FromSeconds(2)), "an entry carries its version's upload moment in Prague time");
            Assert.That(zip.Entries[1].LastWriteTime.DateTime, Is.EqualTo(PragueTime(uploads[1])).Within(TimeSpan.FromSeconds(2)));
        }
    }

    [Test]
    public async Task AnUnknownOrForeignActHasNoArchive()
    {
        await using var other = await TestTenant.Create();
        var otherAct = await other.AddAct(await other.AddCase(Day), Day);
        await other.AddActFile(otherAct);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.reader.ReadActFileArchive(otherAct.Id, includeVersions: true, CancellationToken.None), Is.Null, "the act id alone never reaches another tenant's act");
            Assert.That(await this.reader.ReadActFileArchive(Guid.CreateVersion7(), includeVersions: false, CancellationToken.None), Is.Null);
        }
    }

    [Test]
    public async Task AnActWithoutFilesGetsAnEmptyArchive()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);

        var archive = await this.reader.ReadActFileArchive(act.Id, includeVersions: false, CancellationToken.None);

        Assert.That(await this.Unpack(archive!), Is.Empty);
    }

    [Test]
    public async Task ALargeFileStreamsThroughWithoutBeingHeldWhole()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var content = RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 4 * 1024 * 1024);
        this.Stored(await this.Tenant.AddActFile(act, "velký.txt"), content);

        var archive = await this.reader.ReadActFileArchive(act.Id, includeVersions: false, CancellationToken.None);
        var target = new AsyncOnlyStream();
        await this.reader.WriteFileArchive(archive!, target, CancellationToken.None);

        await using var zip = new ZipArchive(new MemoryStream(target.Written()));
        await using var entry = await zip.Entries.Single().OpenAsync();
        using var text = new StreamReader(entry, Encoding.UTF8);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(target.LargestWrite, Is.LessThan(256 * 1024), "what the entry's end writes synchronously is held only until the next write");
            Assert.That(await text.ReadToEndAsync(), Is.EqualTo(content));
        }
    }

    [Test]
    public async Task AMissingBlobFailsTheWriteAndLeavesTheArchiveWithoutItsEnd()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        this.Stored(await this.Tenant.AddActFile(act, "a.pdf"), "a");
        await this.Tenant.AddActFile(act, "b.pdf");

        var archive = await this.reader.ReadActFileArchive(act.Id, includeVersions: false, CancellationToken.None);
        var target = new AsyncOnlyStream();

        await Assert.ThrowsAsync<IOException>(async () => await this.reader.WriteFileArchive(archive!, target, CancellationToken.None));

        var written = target.Written();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(written, Is.Not.Empty, "the first file was already on its way");
            Assert.That(() => new ZipArchive(new MemoryStream(written)), Throws.InstanceOf<InvalidDataException>(), "a cut archive must never read as whole");
        }
    }

    private static DateTime PragueTime(DateTime utc)
    {
        return TimeZoneInfo.ConvertTimeFromUtc(utc, FileArchiveLayout.Prague);
    }

    private FileAsset Stored(FileAsset file, string content)
    {
        this.blobs.WrittenByPath[file.Versions.Single().StoragePath] = content;

        return file;
    }

    private void Stored(FileVersion version, string content)
    {
        this.blobs.WrittenByPath[version.StoragePath] = content;
    }

    /// <summary>
    /// Each entry's name to its text, in archive order.
    /// </summary>
    private async Task<Dictionary<string, string>> Unpack(FileArchive archive)
    {
        var target = new AsyncOnlyStream();
        await this.reader.WriteFileArchive(archive, target, CancellationToken.None);

        await using var zip = new ZipArchive(new MemoryStream(target.Written()));

        var entries = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var entry in zip.Entries)
        {
            await using var content = await entry.OpenAsync();
            using var text = new StreamReader(content, Encoding.UTF8);

            entries[entry.FullName] = await text.ReadToEndAsync();
        }

        return entries;
    }
}
