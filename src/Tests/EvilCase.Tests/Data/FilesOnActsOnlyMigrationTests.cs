using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EvilBrains.EvilCase.Tests.Data;

public class FilesOnActsOnlyMigrationTests
{
    [Test]
    public async Task TheMigrationDeletesTheFilesOfCasesWithTheirUploadLinksAndKeepsTheActFiles()
    {
        var name = $"evilcase_migration_{Guid.NewGuid().ToString("N")[..8]}";
        var scratch = await TestDatabase.CreateScratchDatabase(name);

        try
        {
            await using var context = new ApplicationDbContextFactory().CreateDbContext([]);
            context.Database.SetConnectionString(scratch);

            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("DropUnusedSearchIndexes");

            var seeded = await Seed(context);
            var caseUpdatedBefore = await CaseUpdated(context, seeded.CaseId);

            await migrator.MigrateAsync("FilesOnActsOnly");

            var fileIds = await context.Database
                .SqlQuery<Guid>($"""SELECT "Id" AS "Value" FROM "FileAssets" """)
                .ToListAsync();
            var versionFileIds = await context.Database
                .SqlQuery<Guid>($"""SELECT "FileAssetId" AS "Value" FROM "FileVersions" """)
                .ToListAsync();
            var linkKinds = await context.Database
                .SqlQuery<string>($"""SELECT "Kind" AS "Value" FROM "FileLinks" """)
                .ToListAsync();
            var recordedDeletes = await context.Database
                .SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM history."FileAssets" WHERE "Id" = {seeded.DroppedFileId} AND "Operation" = 'DELETE'""")
                .SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fileIds, Is.EqualTo([seeded.ActFileId]), "a case's file goes and an act's file stays");
                Assert.That(versionFileIds, Is.EqualTo([seeded.ActFileId]), "the versions of a case's file go with it");
                Assert.That(linkKinds, Is.EqualTo(["ActUpload"]), "an upload link onto a case goes, one onto an act stays");
                Assert.That(recordedDeletes, Is.EqualTo(1), "the history records the deleted case file");
                Assert.That(await CaseUpdated(context, seeded.CaseId), Is.EqualTo(caseUpdatedBefore), "deleting the case's files is no user's change of the case");
            }
        }
        finally
        {
            await TestDatabase.DropScratchDatabase(name);
        }
    }

    private static async Task<DateTime?> CaseUpdated(ApplicationDbContext context, Guid caseId)
    {
        return await context.Database
            .SqlQuery<DateTime?>($"""SELECT "Updated" AS "Value" FROM "Cases" WHERE "Id" = {caseId}""")
            .SingleAsync();
    }

    private static async Task<SeededFiles> Seed(ApplicationDbContext context)
    {
        var accountId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var tokenId = Guid.CreateVersion7();
        var caseId = Guid.CreateVersion7();
        var actId = Guid.CreateVersion7();
        var droppedFileId = Guid.CreateVersion7();
        var actFileId = Guid.CreateVersion7();

        await context.Database.ExecuteSqlAsync($"""INSERT INTO "Accounts" ("Id", "Name") VALUES ({accountId}, 'migration')""");
        await context.Database.ExecuteSqlAsync($"""INSERT INTO "Tenants" ("Id", "AccountId", "Name") VALUES ({tenantId}, {accountId}, 'migration')""");
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Users" ("Id", "TenantId", "Email", "PasswordHash", "Role", "FailedLoginAttempts")
            VALUES ({userId}, {tenantId}, {userId + "@example.com"}, 'hash', 'User', 0)
            """);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "ApiTokens" ("Id", "UserId", "Name", "Scope", "TokenHash", "Expires")
            VALUES ({tokenId}, {userId}, 'migration', 'ReadWrite', repeat('t', 64), now() + interval '1 day')
            """);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Cases" ("Id", "TenantId", "UserId", "CaseNumber", "Date", "Title", "Status")
            VALUES ({caseId}, {tenantId}, {userId}, 'EC/20261128-001', '2026-11-28', 'Spis', 'Active')
            """);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "Acts" ("Id", "TenantId", "UserId", "CaseId", "ActNumber", "Date", "Title")
            VALUES ({actId}, {tenantId}, {userId}, {caseId}, 'EC/20261128-001/20261128-001', '2026-11-28', 'Úkon')
            """);

        await AddFile(context, droppedFileId, tenantId, userId, caseId, actId: null, versions: 2);
        await AddFile(context, actFileId, tenantId, userId, caseId: null, actId, versions: 1);

        await AddLink(context, tenantId, userId, tokenId, "CaseUpload", caseId);
        await AddLink(context, tenantId, userId, tokenId, "ActUpload", actId);

        return new SeededFiles(caseId, droppedFileId, actFileId);
    }

    private static async Task AddFile(ApplicationDbContext context, Guid fileId, Guid tenantId, Guid userId, Guid? caseId, Guid? actId, int versions)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "FileAssets" ("Id", "TenantId", "UserId", "CaseId", "ActId", "FileName")
            VALUES ({fileId}, {tenantId}, {userId}, {caseId}, {actId}, 'smlouva.pdf')
            """);

        for (var number = 1; number <= versions; number++)
        {
            var versionId = Guid.CreateVersion7();

            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO "FileVersions" ("Id", "TenantId", "UserId", "FileAssetId", "Number", "ContentHash", "SizeBytes", "StoragePath")
                VALUES ({versionId}, {tenantId}, {userId}, {fileId}, {number}, repeat('a', 64), 1, {tenantId + "/" + versionId})
                """);
        }
    }

    private static async Task AddLink(ApplicationDbContext context, Guid tenantId, Guid userId, Guid tokenId, string kind, Guid targetId)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "FileLinks" ("Id", "SecretHash", "TenantId", "UserId", "ApiTokenId", "Kind", "TargetId", "Expires")
            VALUES ({Guid.CreateVersion7()}, {kind.PadRight(64, 's')}, {tenantId}, {userId}, {tokenId}, {kind}, {targetId}, now() + interval '1 day')
            """);
    }

    private sealed record SeededFiles(Guid CaseId, Guid DroppedFileId, Guid ActFileId);
}
