using EvilBrains.EvilCase.Business.Files;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileListQueryTests
{
    [Test]
    public async Task TheFilesOfACaseComeOldestFirst()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var @case = await SeedCase(context, tenantId, userId);

        var writer = new FileWriter(new FixedDbSession(context), new FakeFileBlobStore(), userContext, NullLogger<FileWriter>.Instance);
        await writer.UploadCaseFile(@case.Id, Upload("prvni.txt"), CancellationToken.None);
        await writer.UploadCaseFile(@case.Id, Upload("druhy.txt"), CancellationToken.None);
        await writer.UploadCaseFile(@case.Id, Upload("treti.txt"), CancellationToken.None);

        var reader = new FileReader(new FixedDbSession(context), new FakeFileBlobStore());
        var items = await reader.ListCaseFiles(@case.Id, CancellationToken.None);

        Assert.That(items!.Select(item => item.FileName), Is.EqualTo(["prvni.txt", "druhy.txt", "treti.txt"]), "the files of a case come back oldest first");
    }

    [Test]
    public async Task AFileOfAnotherCaseIsNotListed()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var caseA = await SeedCase(context, tenantId, userId, "EC/20260821-001");
        var caseB = await SeedCase(context, tenantId, userId, "EC/20260821-002");

        var writer = new FileWriter(new FixedDbSession(context), new FakeFileBlobStore(), userContext, NullLogger<FileWriter>.Instance);
        await writer.UploadCaseFile(caseA.Id, Upload("a.txt"), CancellationToken.None);
        await writer.UploadCaseFile(caseB.Id, Upload("b.txt"), CancellationToken.None);

        var reader = new FileReader(new FixedDbSession(context), new FakeFileBlobStore());
        var items = await reader.ListCaseFiles(caseA.Id, CancellationToken.None);

        Assert.That(items!.Select(item => item.FileName), Is.EqualTo(["a.txt"]), "a file of another case must not be listed");
    }

    [Test]
    public async Task ListingTheFilesOfAMissingCaseAnswersWithNothing()
    {
        var userContext = new StubUserContext();
        using var entered = userContext.Enter(Guid.CreateVersion7(), Guid.CreateVersion7());

        await using var context = TestDatabase.CreateMigrated(userContext);
        var reader = new FileReader(new FixedDbSession(context), new FakeFileBlobStore());

        var items = await reader.ListCaseFiles(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(items, Is.Null, "a tenant with no such case must get nothing rather than an empty list");
    }

    private static FileUpload Upload(string fileName)
    {
        return new FileUpload { FileName = fileName, MediaType = "text/plain", Content = new MemoryStream("a"u8.ToArray()) };
    }

    private static async Task<Case> SeedCase(ApplicationDbContext context, Guid tenantId, Guid userId, string caseNumber = "EC/20260821-001")
    {
        var account = new Account { Name = "file list query" };
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
}
