using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EvilBrains.EvilCase.Tests.Data;

public class MainFilesMigrationTests
{
    [Test]
    public async Task TheMigrationMakesTheOldestFileOfEachCaseAndActMain()
    {
        var name = $"evilcase_migration_{Guid.NewGuid().ToString("N")[..8]}";
        var scratch = await TestDatabase.CreateScratchDatabase(name);

        try
        {
            await using var context = new ApplicationDbContextFactory().CreateDbContext([]);
            context.Database.SetConnectionString(scratch);

            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("FileLinks");

            var (caseOldest, actOldest) = await SeedFiles(context);

            await migrator.MigrateAsync();

            var mainFileIds = await context.Database
                .SqlQuery<Guid>($"""SELECT "Id" AS "Value" FROM "FileAssets" WHERE "IsMain" """)
                .ToListAsync();
            var stamped = await context.Database
                .SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "FileAssets" WHERE "Updated" IS NOT NULL""")
                .SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mainFileIds, Is.EquivalentTo([caseOldest, actOldest]), "the oldest file by upload, not by id, of each case and act becomes main");
                Assert.That(stamped, Is.Zero, "marking the main files is no user's change of a file");
            }
        }
        finally
        {
            await TestDatabase.DropScratchDatabase(name);
        }
    }

    // The ids run against the upload order, so only the upload moment can pick the oldest.
    private static async Task<(Guid CaseOldest, Guid ActOldest)> SeedFiles(ApplicationDbContext context)
    {
        var accountId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var fileIds = Enumerable.Range(0, 4).Select(static _ => Guid.CreateVersion7()).Order().ToList();

        await context.Database.ExecuteSqlAsync($"""INSERT INTO "Accounts" ("Id", "Name") VALUES ({accountId}, 'migration')""");
        await context.Database.ExecuteSqlAsync($"""INSERT INTO "Tenants" ("Id", "AccountId", "Name") VALUES ({tenantId}, {accountId}, 'migration')""");
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Users" ("Id", "TenantId", "Email", "PasswordHash", "Role", "FailedLoginAttempts")
            VALUES ({userId}, {tenantId}, {userId + "@example.com"}, 'hash', 'User', 0)
            """);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Cases" ("Id", "TenantId", "UserId", "CaseNumber", "Date", "Title", "Status")
            VALUES ({caseId}, {tenantId}, {userId}, 'EC/20261104-001', '2026-11-04', 'Spis', 'Active')
            """);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Acts" ("Id", "TenantId", "UserId", "CaseId", "ActNumber", "Date", "Title")
            VALUES ({actId}, {tenantId}, {userId}, {caseId}, 'EC/20261104-001/20261104-001', '2026-11-04', 'Úkon')
            """);

        await AddFile(context, fileIds[3], tenantId, userId, caseId, actId: null, "b.pdf");
        await AddFile(context, fileIds[2], tenantId, userId, caseId, actId: null, "a.pdf");
        await AddFile(context, fileIds[1], tenantId, userId, caseId: null, actId, "d.pdf");
        await AddFile(context, fileIds[0], tenantId, userId, caseId: null, actId, "c.pdf");

        return (fileIds[3], fileIds[1]);
    }

    private static async Task AddFile(ApplicationDbContext context, Guid fileId, Guid tenantId, Guid userId, Guid? caseId, Guid? actId, string fileName)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "FileAssets" ("Id", "TenantId", "UserId", "CaseId", "ActId", "FileName")
            VALUES ({fileId}, {tenantId}, {userId}, {caseId}, {actId}, {fileName})
            """);
    }
}
