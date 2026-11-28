using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

public class HistoryTenantTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    protected override bool AsHost => true;

    [Test]
    public async Task ARecordedCaseRowEqualsTheRowItCameFrom()
    {
        var @case = await this.Tenant.AddCase(Day);

        var equal = await this.Tenant.Context.Database
            .SqlQuery<bool>(
                $"""
                SELECT (to_jsonb(h) - 'HistoryId' - 'Operation' - 'OperationAt' - 'OperationUserId' = to_jsonb(p)) AS "Value"
                FROM history."Cases" h, public."Cases" p
                WHERE h."Id" = {@case.Id} AND p."Id" = {@case.Id} AND h."Operation" = 'INSERT'
                """)
            .SingleAsync();

        Assert.That(equal, Is.True, "a recorded row carries the same values as the row it mirrors, aside from the history columns");
    }

    [Test]
    public async Task ARecordedFileAssetRowEqualsTheRowItCameFrom()
    {
        var file = await this.Tenant.AddActFile(await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day));

        var equal = await this.Tenant.Context.Database
            .SqlQuery<bool>(
                $"""
                SELECT (to_jsonb(h) - 'HistoryId' - 'Operation' - 'OperationAt' - 'OperationUserId' - 'CaseId' - 'ContentHash' - 'SizeBytes' - 'MediaType' - 'StoragePath' = to_jsonb(p)) AS "Value"
                FROM history."FileAssets" h, public."FileAssets" p
                WHERE h."Id" = {file.Id} AND p."Id" = {file.Id} AND h."Operation" = 'INSERT'
                """)
            .SingleAsync();

        Assert.That(equal, Is.True, "a recorded row carries the same values as the row it mirrors, aside from the history columns and the columns the file no longer has");
    }

    [Test]
    public async Task ARecordedFileVersionRowEqualsTheRowItCameFrom()
    {
        var file = await this.Tenant.AddActFile(await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day));
        var versionId = file.Versions.Single().Id;

        var equal = await this.Tenant.Context.Database
            .SqlQuery<bool>(
                $"""
                SELECT (to_jsonb(h) - 'HistoryId' - 'Operation' - 'OperationAt' - 'OperationUserId' = to_jsonb(p)) AS "Value"
                FROM history."FileVersions" h, public."FileVersions" p
                WHERE h."Id" = {versionId} AND p."Id" = {versionId} AND h."Operation" = 'INSERT'
                """)
            .SingleAsync();

        Assert.That(equal, Is.True, "a recorded row carries the same values as the row it mirrors, aside from the history columns");
    }

    [Test]
    public async Task ARecordedFileAssetRowLeavesTheDroppedColumnsEmpty()
    {
        var file = await this.Tenant.AddActFile(await this.Tenant.AddAct(await this.Tenant.AddCase(Day), Day));

        var empty = await this.Tenant.Context.Database
            .SqlQuery<bool>(
                $"""
                SELECT (h."CaseId" IS NULL AND h."ContentHash" IS NULL AND h."SizeBytes" IS NULL AND h."StoragePath" IS NULL) AS "Value"
                FROM history."FileAssets" h
                WHERE h."Id" = {file.Id} AND h."Operation" = 'INSERT'
                """)
            .SingleAsync();

        Assert.That(empty, Is.True, "the case moved out of the file and the blob columns to the version, so the mirror's copies stay empty");
    }

    [Test]
    public async Task DeletingACaseRecordsDeleteRowsForItsActsCommentsAndFiles()
    {
        var context = this.Tenant.Context;
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var caseComment = await this.Tenant.AddCaseComment(@case, "Poznámka ke spisu");
        var actComment = await this.Tenant.AddActComment(act, "Poznámka k úkonu");
        var actFile = await this.Tenant.AddActFile(act);

        context.Cases.Remove(@case);
        await context.SaveChangesAsync();

        var actOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."Acts" WHERE "Id" = {act.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();
        var caseCommentOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."Comments" WHERE "Id" = {caseComment.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();
        var actCommentOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."Comments" WHERE "Id" = {actComment.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();
        var actFileOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."FileAssets" WHERE "Id" = {actFile.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();
        var actFileVersionOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."FileVersions" WHERE "FileAssetId" = {actFile.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actOperation, Is.EqualTo("DELETE"), "the cascade takes the case's acts with it, and history records each");
            Assert.That(caseCommentOperation, Is.EqualTo("DELETE"), "the cascade takes the case's own comments with it");
            Assert.That(actCommentOperation, Is.EqualTo("DELETE"), "the cascade takes an act's comments with it");
            Assert.That(actFileOperation, Is.EqualTo("DELETE"), "the cascade takes an act's files with it");
            Assert.That(actFileVersionOperation, Is.EqualTo("DELETE"), "the cascade takes a file's versions with it");
        }
    }

    [Test]
    public async Task OperationUserIdCarriesTheSignedInUserForAWriteInsideAUserScope()
    {
        var @case = await this.Tenant.AddCase(Day);

        var operationUserId = await this.Tenant.Context.Database
            .SqlQuery<Guid?>($"""SELECT "OperationUserId" AS "Value" FROM history."Cases" WHERE "Id" = {@case.Id} AND "Operation" = 'INSERT'""")
            .SingleAsync();

        Assert.That(operationUserId, Is.EqualTo(this.Tenant.UserId), "the row's own UserId is who owns it, not who wrote it, so history reads the operator from the session");
    }

    [Test]
    public async Task OperationUserIdIsNullForAWriteMadeWithoutAUserScope()
    {
        var userContext = new StubUserContext();
        await using var context = TestDatabase.CreateMigratedAsHost(userContext);

        var account = new Account { Name = "no scope" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var operationUserId = await context.Database
            .SqlQuery<Guid?>($"""SELECT "OperationUserId" AS "Value" FROM history."Accounts" WHERE "Id" = {account.Id}""")
            .SingleAsync();

        Assert.That(operationUserId, Is.Null, "a write made with nobody signed in carries no operator");
    }
}
