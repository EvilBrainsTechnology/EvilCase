using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseRelations : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        DropCaseHierarchy(migrationBuilder);
        CreateCaseRelations(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseRelations");

        migrationBuilder.AddColumn<long>(
            name: "ParentCaseId",
            table: "Cases",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId");

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId",
            principalTable: "Cases",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <summary>
    /// The column goes with the sub-case tree. Nothing creates a case yet, so no row loses a parent.
    /// </summary>
    private static void DropCaseHierarchy(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases");

        migrationBuilder.DropIndex(
            name: "IX_Cases_ParentCaseId",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "ParentCaseId",
            table: "Cases");
    }

    private static void CreateCaseRelations(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseRelations",
            columns: table => new
            {
                CaseId = table.Column<long>(type: "bigint", nullable: false),
                RelatedCaseId = table.Column<long>(type: "bigint", nullable: false),
            },
            constraints: table =>
            {
                // The pair is the key, so it is one row whichever end asks.
                table.PrimaryKey("PK_CaseRelations", x => new { x.CaseId, x.RelatedCaseId });

                // The pair is stored once, lower identifier first; that is what refuses the mirror row
                // and the row relating a case to itself.
                table.CheckConstraint("CK_CaseRelations_Ordered", "\"CaseId\" < \"RelatedCaseId\"");

                table.ForeignKey(
                    name: "FK_CaseRelations_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CaseRelations_Cases_RelatedCaseId",
                    column: x => x.RelatedCaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // The key already leads with CaseId; the other end needs its own.
        migrationBuilder.CreateIndex(
            name: "IX_CaseRelations_RelatedCaseId",
            table: "CaseRelations",
            column: "RelatedCaseId");
    }
}
