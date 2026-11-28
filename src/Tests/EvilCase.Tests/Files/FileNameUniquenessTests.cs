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
    public async Task TwoFilesOfOneActCannotShareANameInAnyCase(string first, string second)
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        await this.Tenant.AddActFile(act, first);

        await Assert.ThatAsync(
            async () => await this.Tenant.AddActFile(act, second),
            Throws.InstanceOf<DbUpdateException>().With.Matches<DbUpdateException>(static exception => exception!.IsUniqueViolation()),
            "the database refuses a second file of the name, whatever its case");
    }

    [Test]
    public async Task FilesOfDifferentActsMayShareAName()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var otherAct = await this.Tenant.AddAct(@case, Day);

        await this.Tenant.AddActFile(act, "a.txt");
        var onOtherAct = await this.Tenant.AddActFile(otherAct, "a.txt");

        Assert.That(onOtherAct.FileName, Is.EqualTo("a.txt"), "the acts of a case do not share their names");
    }

    [Test]
    public async Task AFileCannotHoldTwoVersionsOfOneNumber()
    {
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        var file = await this.Tenant.AddActFile(act, "a.txt");
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
        var act = await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day);
        await this.Tenant.AddActFile(act, "a.txt");
        var second = await this.Tenant.AddActFile(act, "b.txt");

        await Assert.ThatAsync(
            async () => await this.Tenant.Context.FileAssets
                .Where(file => file.Id == second.Id)
                .ExecuteUpdateAsync(static setters => setters.SetProperty(static file => file.FileName, "A.TXT")),
            Throws.InstanceOf<DbException>().With.Matches<DbException>(static exception => exception!.IsUniqueViolation()),
            "a bulk update surfaces the provider's own exception, which the rename catches");
    }
}
