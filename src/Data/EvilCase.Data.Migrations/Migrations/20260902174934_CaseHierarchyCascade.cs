using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseHierarchyCascade : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        RecreateParentCaseForeignKey(migrationBuilder, ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RecreateParentCaseForeignKey(migrationBuilder, ReferentialAction.SetNull);
    }

    private static void RecreateParentCaseForeignKey(MigrationBuilder migrationBuilder, ReferentialAction onDelete)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases");

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId",
            principalTable: "Cases",
            principalColumn: "Id",
            onDelete: onDelete);
    }
}
