using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class Cases : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateCases(migrationBuilder);
        CreateCaseTags(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CaseTags");

        migrationBuilder.DropTable(
            name: "Cases");
    }

    private static void CreateCases(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Cases",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OwnerId = table.Column<long>(type: "bigint", nullable: false),
                ParentCaseId = table.Column<long>(type: "bigint", nullable: true),
                Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Subject = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", x => x.Id);
                table.ForeignKey(
                    name: "FK_Cases_Cases_ParentCaseId",
                    column: x => x.ParentCaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Cases_Users_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Cases_OwnerId",
            table: "Cases",
            column: "OwnerId");

        migrationBuilder.CreateIndex(
            name: "IX_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId");
    }

    private static void CreateCaseTags(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CaseTags",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CaseId = table.Column<long>(type: "bigint", nullable: false),
                Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CaseTags", x => x.Id);
                table.ForeignKey(
                    name: "FK_CaseTags_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CaseTags_CaseId_Value",
            table: "CaseTags",
            columns: ["CaseId", "Value"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CaseTags_Value",
            table: "CaseTags",
            column: "Value");
    }
}
