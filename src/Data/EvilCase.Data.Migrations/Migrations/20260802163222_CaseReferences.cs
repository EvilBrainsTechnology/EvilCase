using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseReferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddInternalReference(migrationBuilder);
        CreateCaseReferences(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseReferences");

        migrationBuilder.DropIndex(
            name: "IX_Cases_OwnerId_InternalReference",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "InternalReference",
            table: "Cases");
    }

    private static void AddInternalReference(MigrationBuilder migrationBuilder)
    {
        // The default is for rows that predate the column. Nothing creates a case yet, so in practice it
        // applies to none — and once #84 generates the mark, every new case arrives with a real one.
        migrationBuilder.AddColumn<string>(
            name: "InternalReference",
            table: "Cases",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_Cases_OwnerId_InternalReference",
            table: "Cases",
            columns: ["OwnerId", "InternalReference"],
            unique: true);
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
                AssignedByPartyId = table.Column<long>(type: "bigint", nullable: false),
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

        migrationBuilder.CreateIndex(
            name: "IX_CaseReferences_AssignedByPartyId",
            table: "CaseReferences",
            column: "AssignedByPartyId");

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
