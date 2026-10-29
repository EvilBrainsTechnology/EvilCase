using System.Data.Common;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileNameUniquenessTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    [Test]
    [TestCase("Smlouva.pdf", "SMLOUVA.PDF")]
    [TestCase("Řízení.pdf", "řízení.pdf")]
    public async Task TwoFilesOfOneCaseCannotShareANameInAnyCase(string first, string second)
    {
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddCaseFile(@case, first);

        await Assert.ThatAsync(
            async () => await this.Tenant.AddCaseFile(@case, second),
            Throws.InstanceOf<DbUpdateException>().With.Matches<DbUpdateException>(static exception => exception!.IsUniqueViolation()),
            "the database refuses a second file of the name, whatever its case");
    }

    [Test]
    public async Task TwoFilesOfOneActCannotShareAName()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        await this.Tenant.AddActFile(act, "Spis.txt");

        await Assert.ThatAsync(
            async () => await this.Tenant.AddActFile(act, "SPIS.TXT"),
            Throws.InstanceOf<DbUpdateException>().With.Matches<DbUpdateException>(static exception => exception!.IsUniqueViolation()));
    }

    [Test]
    public async Task FilesOfDifferentOwnersMayShareAName()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var otherCase = await this.Tenant.AddCase(Day);

        var onCase = await this.Tenant.AddCaseFile(@case, "a.txt");
        var onAct = await this.Tenant.AddActFile(act, "a.txt");
        var onOtherCase = await this.Tenant.AddCaseFile(otherCase, "a.txt");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(onCase.FileName, Is.EqualTo("a.txt"));
            Assert.That(onAct.FileName, Is.EqualTo("a.txt"), "the acts of a case do not share its names");
            Assert.That(onOtherCase.FileName, Is.EqualTo("a.txt"), "another case does not share the names");
        }
    }

    [Test]
    public async Task AFileCannotHoldTwoVersionsOfOneNumber()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "a.txt");
        var duplicate = new FileVersion
        {
            TenantId = file.TenantId,
            UserId = this.Tenant.UserId,
            FileAssetId = file.Id,
            Number = 1,
            ContentHash = new string('b', 64),
            SizeBytes = 1,
            StoragePath = "tenant/duplicate",
        };

        this.Tenant.Context.FileVersions.Add(duplicate);

        await Assert.ThatAsync(
            async () => await this.Tenant.Context.SaveChangesAsync(),
            Throws.InstanceOf<DbUpdateException>().With.Matches<DbUpdateException>(static exception => exception!.IsUniqueViolation()),
            "two versions of one file must not share a number");
    }

    [Test]
    public async Task ABulkRenameOntoATakenNameFailsWithAUniqueViolation()
    {
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddCaseFile(@case, "a.txt");
        var second = await this.Tenant.AddCaseFile(@case, "b.txt");

        await Assert.ThatAsync(
            async () => await this.Tenant.Context.FileAssets
                .Where(file => file.Id == second.Id)
                .ExecuteUpdateAsync(static setters => setters.SetProperty(static file => file.FileName, "A.TXT")),
            Throws.InstanceOf<DbException>().With.Matches<DbException>(static exception => exception!.IsUniqueViolation()),
            "a bulk update surfaces the provider's own exception, which the rename catches");
    }
}
