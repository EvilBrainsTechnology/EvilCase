using System.IO.Compression;
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
        this.reader = new FileArchiveReader(new FixedDbSession(this.Tenant.Context), this.blobs, NullLogger<FileArchiveReader>.Instance);
    }

    [Test]
    public async Task ACaseArchiveHoldsTheCurrentVersionsOfTheCaseOwnFilesOnly()
    {
        var @case = await this.Tenant.AddCase(Day, title: "Přestupek");
        var subCase = await this.Tenant.AddCase(Day, parentCaseId: @case.Id);
        var act = await this.Tenant.AddAct(@case, Day);
        var smlouva = this.Stored(await this.Tenant.AddCaseFile(@case, "Smlouva.pdf"), "smlouva 1");
        this.Stored(await this.Tenant.AddFileVersion(smlouva), "smlouva 2");
        this.Stored(await this.Tenant.AddCaseFile(@case, "Plná moc.pdf"), "plna moc");
        this.Stored(await this.Tenant.AddCaseFile(subCase, "Podřízený.pdf"), "podrizeny");
        this.Stored(await this.Tenant.AddActFile(act, "Úkon.pdf"), "ukon");

        var archive = await this.reader.ReadCaseFileArchive(@case.Id, includeVersions: false, CancellationToken.None);
        var entries = await this.Unpack(archive!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(archive!.FileName, Is.EqualTo("Přestupek.zip"));
            Assert.That(entries.Keys, Is.EqualTo(["Smlouva.pdf", "Plná moc.pdf"]), "never the files of a sub-case or of the case's acts");
            Assert.That(entries["Smlouva.pdf"], Is.EqualTo("smlouva 2"), "the current version");
        }
    }

    [Test]
    public async Task AnArchiveWithVersionsHoldsEveryVersionStampedWithItsUpload()
    {
        var @case = await this.Tenant.AddCase(Day, title: "Přestupek");
        var smlouva = this.Stored(await this.Tenant.AddCaseFile(@case, "Smlouva.pdf"), "smlouva 1");
        this.Stored(await this.Tenant.AddFileVersion(smlouva), "smlouva 2");

        var archive = await this.reader.ReadCaseFileArchive(@case.Id, includeVersions: true, CancellationToken.None);
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
            Assert.That(archive!.FileName, Is.EqualTo("Přestupek (včetně verzí).zip"));
            Assert.That(zip.Entries.Select(static entry => entry.FullName), Is.EqualTo(["Smlouva (verze 1).pdf", "Smlouva.pdf"]));
            Assert.That(zip.Entries[0].LastWriteTime.DateTime, Is.EqualTo(uploads[0]).Within(TimeSpan.FromSeconds(2)), "an entry carries its version's upload moment");
            Assert.That(zip.Entries[1].LastWriteTime.DateTime, Is.EqualTo(uploads[1]).Within(TimeSpan.FromSeconds(2)));
        }
    }

    [Test]
    public async Task AnActArchiveHoldsTheActFilesAndNotItsCase()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, title: "Odvolání");
        this.Stored(await this.Tenant.AddCaseFile(@case, "Spis.pdf"), "spis");
        this.Stored(await this.Tenant.AddActFile(act, "Odvolání.pdf"), "odvolani");

        var archive = await this.reader.ReadActFileArchive(act.Id, includeVersions: false, CancellationToken.None);
        var entries = await this.Unpack(archive!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(archive!.FileName, Is.EqualTo("Odvolání.zip"));
            Assert.That(entries, Is.EqualTo(new Dictionary<string, string> { ["Odvolání.pdf"] = "odvolani" }));
        }
    }

    [Test]
    public async Task AnUnknownOrForeignOwnerHasNoArchive()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        await other.AddCaseFile(otherCase);
        await other.AddActFile(otherAct);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.reader.ReadCaseFileArchive(otherCase.Id, includeVersions: true, CancellationToken.None), Is.Null, "the case id alone never reaches another tenant's case");
            Assert.That(await this.reader.ReadActFileArchive(otherAct.Id, includeVersions: true, CancellationToken.None), Is.Null, "the act id alone never reaches another tenant's act");
            Assert.That(await this.reader.ReadCaseFileArchive(Guid.CreateVersion7(), includeVersions: false, CancellationToken.None), Is.Null);
            Assert.That(await this.reader.ReadActFileArchive(Guid.CreateVersion7(), includeVersions: false, CancellationToken.None), Is.Null);
        }
    }

    [Test]
    public async Task AnOwnerWithoutFilesGetsAnEmptyArchive()
    {
        var @case = await this.Tenant.AddCase(Day);

        var archive = await this.reader.ReadCaseFileArchive(@case.Id, includeVersions: false, CancellationToken.None);

        Assert.That(await this.Unpack(archive!), Is.Empty);
    }

    [Test]
    public async Task AMissingBlobFailsTheWriteAndLeavesTheArchiveWithoutItsEnd()
    {
        var @case = await this.Tenant.AddCase(Day);
        this.Stored(await this.Tenant.AddCaseFile(@case, "a.pdf"), "a");
        await this.Tenant.AddCaseFile(@case, "b.pdf");

        var archive = await this.reader.ReadCaseFileArchive(@case.Id, includeVersions: false, CancellationToken.None);
        var target = new AsyncOnlyStream();

        await Assert.ThrowsAsync<IOException>(async () => await this.reader.WriteFileArchive(archive!, target, CancellationToken.None));

        var written = target.Written();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(written, Is.Not.Empty, "the first file was already on its way");
            Assert.That(() => new ZipArchive(new MemoryStream(written)), Throws.InstanceOf<InvalidDataException>(), "a cut archive must never read as whole");
        }
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
