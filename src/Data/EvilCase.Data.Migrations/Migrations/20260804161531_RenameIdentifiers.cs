using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class RenameIdentifiers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        RenameCaseNumber(migrationBuilder, from: "InternalCaseReference", to: "CaseNumber");
        RenameActNumber(migrationBuilder, from: "FileNumber", to: "ExternalActNumber");
        RenameExternalCaseNumbers(migrationBuilder, from: "CaseReferences", to: "ExternalCaseNumbers");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RenameExternalCaseNumbers(migrationBuilder, from: "ExternalCaseNumbers", to: "CaseReferences");
        RenameActNumber(migrationBuilder, from: "ExternalActNumber", to: "FileNumber");
        RenameCaseNumber(migrationBuilder, from: "CaseNumber", to: "InternalCaseReference");
    }

    private static void RenameCaseNumber(MigrationBuilder migrationBuilder, string from, string to)
    {
        migrationBuilder.RenameColumn(
            name: from,
            table: "Cases",
            newName: to);

        migrationBuilder.RenameIndex(
            name: $"IX_Cases_OwnerId_{from}",
            newName: $"IX_Cases_OwnerId_{to}",
            table: "Cases");
    }

    private static void RenameActNumber(MigrationBuilder migrationBuilder, string from, string to)
    {
        migrationBuilder.RenameColumn(
            name: from,
            table: "Acts",
            newName: to);

        migrationBuilder.RenameIndex(
            name: $"IX_Acts_{from}",
            newName: $"IX_Acts_{to}",
            table: "Acts");
    }

    private static void RenameExternalCaseNumbers(MigrationBuilder migrationBuilder, string from, string to)
    {
        migrationBuilder.RenameTable(
            name: from,
            newName: to);

        // Renaming the index of a primary key renames its constraint with it.
        migrationBuilder.RenameIndex(
            name: $"PK_{from}",
            newName: $"PK_{to}",
            table: to);

        foreach (var suffix in new[] { "AssignedByPartyId", "CaseId_Value", "Value" })
        {
            migrationBuilder.RenameIndex(
                name: $"IX_{from}_{suffix}",
                newName: $"IX_{to}_{suffix}",
                table: to);
        }

        // A foreign key has no index to rename, and EF models no rename for it.
        foreach (var suffix in new[] { "Cases_CaseId", "Parties_AssignedByPartyId" })
        {
            migrationBuilder.Sql(
                $"""ALTER TABLE "{to}" RENAME CONSTRAINT "FK_{from}_{suffix}" TO "FK_{to}_{suffix}";""");
        }
    }
}
