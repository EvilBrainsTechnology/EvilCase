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
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case);

        var equal = await this.Tenant.Context.Database
            .SqlQuery<bool>(
                $"""
                SELECT (to_jsonb(h) - 'HistoryId' - 'Operation' - 'OperationAt' - 'OperationUserId' = to_jsonb(p)) AS "Value"
                FROM history."FileAssets" h, public."FileAssets" p
                WHERE h."Id" = {file.Id} AND p."Id" = {file.Id} AND h."Operation" = 'INSERT'
                """)
            .SingleAsync();

        Assert.That(equal, Is.True, "a recorded row carries the same values as the row it mirrors, aside from the history columns");
    }

    [Test]
    public async Task DeletingACaseRecordsDeleteRowsForItsActsCommentsAndFiles()
    {
        var context = this.Tenant.Context;
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var caseComment = await this.Tenant.AddCaseComment(@case, "Poznámka ke spisu");
        var actComment = await this.Tenant.AddActComment(act, "Poznámka k úkonu");
        var caseFile = await this.Tenant.AddCaseFile(@case);
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
        var caseFileOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."FileAssets" WHERE "Id" = {caseFile.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();
        var actFileOperation = await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."FileAssets" WHERE "Id" = {actFile.Id} ORDER BY "OperationAt" DESC LIMIT 1""")
            .SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(actOperation, Is.EqualTo("DELETE"), "the cascade takes the case's acts with it, and history records each");
            Assert.That(caseCommentOperation, Is.EqualTo("DELETE"), "the cascade takes the case's own comments with it");
            Assert.That(actCommentOperation, Is.EqualTo("DELETE"), "the cascade takes an act's comments with it");
            Assert.That(caseFileOperation, Is.EqualTo("DELETE"), "the cascade takes the case's own files with it");
            Assert.That(actFileOperation, Is.EqualTo("DELETE"), "the cascade takes an act's files with it");
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
