using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;

namespace EvilBrains.EvilCase.Tests.Files;

/// <summary>
/// The file list on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself.
/// </summary>
public class FileListQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private TestTenant tenant = null!;

    private FileReader reader = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.reader = new FileReader(new FixedDbSession(this.tenant.Context), new FakeFileBlobStore());
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task TheFilesOfACaseComeOldestFirst()
    {
        var @case = await this.tenant.AddCase(Day);

        await this.tenant.AddCaseFile(@case, "prvni.txt");
        await this.tenant.AddCaseFile(@case, "druhy.txt");
        await this.tenant.AddCaseFile(@case, "treti.txt");

        var items = await this.reader.ListCaseFiles(@case.Id, CancellationToken.None);

        Assert.That(items!.Select(item => item.FileName), Is.EqualTo(["prvni.txt", "druhy.txt", "treti.txt"]), "the files of a case come back oldest first");
    }

    [Test]
    public async Task AFileOfAnotherCaseIsNotListed()
    {
        var caseA = await this.tenant.AddCase(Day);
        var caseB = await this.tenant.AddCase(Day);

        await this.tenant.AddCaseFile(caseA, "a.txt");
        await this.tenant.AddCaseFile(caseB, "b.txt");

        var items = await this.reader.ListCaseFiles(caseA.Id, CancellationToken.None);

        Assert.That(items!.Select(item => item.FileName), Is.EqualTo(["a.txt"]), "a file of another case must not be listed");
    }

    [Test]
    public async Task ListingTheFilesOfAMissingCaseAnswersWithNothing()
    {
        var items = await this.reader.ListCaseFiles(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(items, Is.Null, "a tenant with no such case must get nothing rather than an empty list");
    }
}
