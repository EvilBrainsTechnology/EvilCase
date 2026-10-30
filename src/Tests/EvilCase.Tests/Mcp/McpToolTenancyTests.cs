using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Api.Mcp;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Files.Text;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;

namespace EvilBrains.EvilCase.Tests.Mcp;

/// <summary>
/// The tools over the real business services: what the tenant filter keeps out of REST stays out of a tool.
/// </summary>
public class McpToolTenancyTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 10, 30);

    [Test]
    public async Task ACaseOfAnotherTenantIsNotFoundThroughATool()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day, title: "Cizí spis");
        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        Assert.That(
            async () => await CaseTools.GetCase(reader, otherCase.Id, CancellationToken.None),
            Throws.InstanceOf<McpException>().With.Message.Contains(CaseProblems.NotFound),
            "a case id alone never reaches another tenant's case through a tool");
    }

    [Test]
    public async Task TheTextOfAFileOfAnotherTenantIsNotFoundThroughATool()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherFile = await other.AddCaseFile(otherCase, "zapis.txt");
        var blobs = new FakeFileBlobStore();
        blobs.WrittenByPath[otherFile.Versions.Single().StoragePath] = "cizí obsah";
        var reader = new FileTextReader(new FixedDbSession(this.Tenant.Context), blobs, NullLogger<FileTextReader>.Instance);

        Assert.That(
            async () => await FileTools.ReadFileText(reader, otherFile.Id, version: null, page: 1, CancellationToken.None),
            Throws.InstanceOf<McpException>().With.Message.Contains(FileProblems.NotFound),
            "a file id alone never reaches another tenant's content through a tool");
    }

    [Test]
    public async Task TheTextOfAFileOfTheTenantIsRead()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "zapis.txt");
        var blobs = new FakeFileBlobStore();
        blobs.WrittenByPath[file.Versions.Single().StoragePath] = "vlastní obsah";
        var reader = new FileTextReader(new FixedDbSession(this.Tenant.Context), blobs, NullLogger<FileTextReader>.Instance);

        var page = await FileTools.ReadFileText(reader, file.Id, version: null, page: 1, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Text, Is.EqualTo("vlastní obsah"));
            Assert.That(page.TotalLength, Is.EqualTo("vlastní obsah".Length));
            Assert.That(page.PageCount, Is.EqualTo(1));
        }
    }
}
