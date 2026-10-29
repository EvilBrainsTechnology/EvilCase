using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EvilBrains.EvilCase.Tests.Data;

public class FileVersionsMigrationTests
{
    [Test]
    public async Task TheMigrationMovesEachFilesBlobIntoItsFirstVersion()
    {
        var name = $"evilcase_migration_{Guid.NewGuid().ToString("N")[..8]}";
        var scratch = await TestDatabase.CreateScratchDatabase(name);

        try
        {
            await using var context = new ApplicationDbContextFactory().CreateDbContext([]);
            context.Database.SetConnectionString(scratch);

            var migrator = context.GetService<IMigrator>();
            await migrator.MigrateAsync("CaseAncestors");

            var (tenantId, userId, fileId) = await SeedFile(context);

            await migrator.MigrateAsync();

            var movedVersions = await context.Database
                .SqlQuery<int>(
                    $"""
                    SELECT count(*)::int AS "Value"
                    FROM "FileVersions" v
                    JOIN "FileAssets" f ON f."Id" = v."FileAssetId"
                    WHERE v."Id" = {fileId} AND v."FileAssetId" = {fileId} AND v."Number" = 1
                        AND v."ContentHash" = repeat('a', 64) AND v."SizeBytes" = 3 AND v."MediaType" = 'application/pdf'
                        AND v."StoragePath" = 'tenant/blob' AND v."TenantId" = {tenantId} AND v."UserId" = {userId}
                        AND v."Created" = f."Created"
                    """)
                .SingleAsync();
            var allVersions = await context.Database
                .SqlQuery<int>($"""SELECT count(*)::int AS "Value" FROM "FileVersions" """)
                .SingleAsync();
            var blobColumns = await context.Database
                .SqlQuery<int>(
                    $"""
                    SELECT count(*)::int AS "Value" FROM information_schema.columns
                    WHERE table_schema = 'public' AND table_name = 'FileAssets' AND column_name = 'StoragePath'
                    """)
                .SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(movedVersions, Is.EqualTo(1), "the file's blob becomes version 1 under the file's own id, with the upload moment");
                Assert.That(allVersions, Is.EqualTo(1), "each file gets exactly one first version");
                Assert.That(blobColumns, Is.Zero, "the blob columns leave the file");
            }
        }
        finally
        {
            await TestDatabase.DropScratchDatabase(name);
        }
    }

    private static async Task<(Guid TenantId, Guid UserId, Guid FileId)> SeedFile(ApplicationDbContext context)
    {
        var accountId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        var caseId = Guid.CreateVersion7();
        var fileId = Guid.CreateVersion7();

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
            VALUES ({caseId}, {tenantId}, {userId}, 'EC/20260821-001', '2026-08-21', 'Spis', 'Active')
            """);
        await context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO "FileAssets" ("Id", "TenantId", "UserId", "CaseId", "FileName", "ContentHash", "SizeBytes", "MediaType", "StoragePath")
            VALUES ({fileId}, {tenantId}, {userId}, {caseId}, 'smlouva.pdf', repeat('a', 64), 3, 'application/pdf', 'tenant/blob')
            """);

        return (tenantId, userId, fileId);
    }
}
