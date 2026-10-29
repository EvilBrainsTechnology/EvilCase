using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileVersionReaderTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private FakeFileBlobStore blobs = null!;

    private FileReader reader = null!;

    [SetUp]
    public void SetUpReader()
    {
        this.blobs = new FakeFileBlobStore();
        this.reader = new FileReader(new FixedDbSession(this.Tenant.Context), this.blobs);
    }

    [Test]
    public async Task TheVersionsOfAFileComeNewestFirstWithWhoUploadedThem()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "a.pdf", sizeBytes: 3);
        var colleague = await this.Tenant.AddUser();
        await this.Tenant.AddFileVersion(file, sizeBytes: 5, uploaderId: colleague.Id);
        var ownEmail = await this.Tenant.Context.Users
            .Where(user => user.Id == this.Tenant.UserId)
            .Select(static user => user.Email)
            .SingleAsync();

        var items = await this.reader.ListFileVersions(file.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items!.Select(static item => item.Number), Is.EqualTo([2, 1]), "the newest version comes first");
            Assert.That(items!.Select(static item => item.SizeBytes), Is.EqualTo([5L, 3L]));
            Assert.That(items!.Select(static item => item.UploaderEmail), Is.EqualTo([colleague.Email, ownEmail]));
        }
    }

    [Test]
    public async Task ListingTheVersionsOfAMissingFileAnswersWithNothing()
    {
        var items = await this.reader.ListFileVersions(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(items, Is.Null, "a tenant with no such file must get nothing rather than an empty list");
    }

    [Test]
    public async Task ListingTheVersionsOfAnotherTenantsFileAnswersWithNothing()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase);

        var items = await this.reader.ListFileVersions(otherFile.Id, CancellationToken.None);

        Assert.That(items, Is.Null, "the file id alone never reaches another tenant's file");
    }

    [Test]
    public async Task TheContentIsTheCurrentVersionUnlessTheReadNamesAnother()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "a.txt");
        var first = file.Versions.Single();
        var second = await this.Tenant.AddFileVersion(file);
        this.blobs.WrittenByPath[first.StoragePath] = "prvni";
        this.blobs.WrittenByPath[second.StoragePath] = "druha";

        var current = await this.reader.OpenFileContent(file.Id, version: null, CancellationToken.None);
        var named = await this.reader.OpenFileContent(file.Id, version: 1, CancellationToken.None);
        var missing = await this.reader.OpenFileContent(file.Id, version: 3, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await ReadText(current), Is.EqualTo("druha"), "a read that names no version gets the highest");
            Assert.That(await ReadText(named), Is.EqualTo("prvni"));
            Assert.That(missing, Is.Null, "a version the file never had is not found");
        }
    }

    [Test]
    public async Task AVersionNumberIsLookedUpThroughItsOwnFile()
    {
        var @case = await this.Tenant.AddCase(Day);
        var fileA = await this.Tenant.AddCaseFile(@case, "a.txt");
        var fileB = await this.Tenant.AddCaseFile(@case, "b.txt");
        var secondOfB = await this.Tenant.AddFileVersion(fileB);
        this.blobs.WrittenByPath[fileA.Versions.Single().StoragePath] = "a";
        this.blobs.WrittenByPath[fileB.Versions.First().StoragePath] = "b1";
        this.blobs.WrittenByPath[secondOfB.StoragePath] = "b2";

        var download = await this.reader.OpenFileContent(fileA.Id, version: 2, CancellationToken.None);

        Assert.That(download, Is.Null, "version 2 of another file is not version 2 of this one");
    }

    private static async Task<string?> ReadText(FileDownload? download)
    {
        if (download is null)
            return null;

        await using var content = download.Content;
        using var text = new StreamReader(content);

        return await text.ReadToEndAsync();
    }
}
