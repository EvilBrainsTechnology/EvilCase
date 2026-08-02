using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class Acts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateActs(migrationBuilder);
        CreateActIndexes(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Acts");
    }

    private static void CreateActs(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Acts",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CaseId = table.Column<long>(type: "bigint", nullable: false),
                Ordinal = table.Column<int>(type: "integer", nullable: false),
                Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                FileNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Drafted = table.Column<DateOnly>(type: "date", nullable: true),
                Sent = table.Column<DateOnly>(type: "date", nullable: true),
                Delivered = table.Column<DateOnly>(type: "date", nullable: true),
                Received = table.Column<DateOnly>(type: "date", nullable: true),
                Summary = table.Column<string>(type: "text", nullable: true),
                IssuedByPartyId = table.Column<long>(type: "bigint", nullable: true),
                AddressedToPartyId = table.Column<long>(type: "bigint", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Acts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Acts_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Acts_Parties_AddressedToPartyId",
                    column: x => x.AddressedToPartyId,
                    principalTable: "Parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Acts_Parties_IssuedByPartyId",
                    column: x => x.IssuedByPartyId,
                    principalTable: "Parties",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateActIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Acts_AddressedToPartyId",
            table: "Acts",
            column: "AddressedToPartyId");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_CaseId",
            table: "Acts",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_FileNumber",
            table: "Acts",
            column: "FileNumber");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_IssuedByPartyId",
            table: "Acts",
            column: "IssuedByPartyId");
    }
}
