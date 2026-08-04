using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class Numbering : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddTheActNumber(migrationBuilder);
        CreateThePatterns(migrationBuilder);
        CreateTheSeries(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NumberingSettings");

        migrationBuilder.DropTable(
            name: "NumberSequences");

        migrationBuilder.DropIndex(
            name: "IX_Acts_CaseId_ActNumber",
            table: "Acts");

        migrationBuilder.DropColumn(
            name: "ActNumber",
            table: "Acts");
    }

    // Nothing creates an act yet, so the column lands empty and the unique index holds from the first
    // act ever written.
    private static void AddTheActNumber(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ActNumber",
            table: "Acts",
            type: "character varying(128)",
            maxLength: 128,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_CaseId_ActNumber",
            table: "Acts",
            columns: ["CaseId", "ActNumber"],
            unique: true);
    }

    private static void CreateThePatterns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NumberingSettings",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CaseNumberPattern = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ActNumberPattern = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NumberingSettings", x => x.Id);
            });

        migrationBuilder.InsertData(
            table: "NumberingSettings",
            columns: ["Id", "ActNumberPattern", "CaseNumberPattern", "Updated"],
            values: [1L, "{case-number}-{year}{month}{day}-{seq}", "EC-{year}{month}{day}-{seq}", null]);
    }

    private static void CreateTheSeries(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NumberSequences",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OwnerId = table.Column<long>(type: "bigint", nullable: false),
                Scope = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                LastValue = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NumberSequences", x => x.Id);
                table.ForeignKey(
                    name: "FK_NumberSequences_Users_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // The upsert that advances a series names these columns, so this index is what makes it atomic.
        migrationBuilder.CreateIndex(
            name: "IX_NumberSequences_OwnerId_Scope",
            table: "NumberSequences",
            columns: ["OwnerId", "Scope"],
            unique: true);
    }
}
