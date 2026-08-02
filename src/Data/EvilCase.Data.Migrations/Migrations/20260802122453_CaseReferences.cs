using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseReferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateCaseReferences(migrationBuilder);
        CreateCaseReferenceIndexes(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseReferences");
    }

    private static void CreateCaseReferences(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseReferences",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CaseId = table.Column<long>(type: "bigint", nullable: false),
                Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AssignedByPartyId = table.Column<long>(type: "bigint", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseReferences", x => x.Id);
                table.ForeignKey(
                    name: "FK_CaseReferences_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CaseReferences_Parties_AssignedByPartyId",
                    column: x => x.AssignedByPartyId,
                    principalTable: "Parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateCaseReferenceIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_CaseReferences_AssignedByPartyId",
            table: "CaseReferences",
            column: "AssignedByPartyId");

        // At most one mark per case with no assigning authority: the case's own internal mark.
        migrationBuilder.CreateIndex(
            name: "IX_CaseReferences_CaseId_Internal",
            table: "CaseReferences",
            column: "CaseId",
            unique: true,
            filter: "\"AssignedByPartyId\" IS NULL");

        migrationBuilder.CreateIndex(
            name: "IX_CaseReferences_CaseId_Value",
            table: "CaseReferences",
            columns: ["CaseId", "Value"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CaseReferences_Value",
            table: "CaseReferences",
            column: "Value");
    }
}
