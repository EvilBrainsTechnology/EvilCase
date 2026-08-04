using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class ActDate : Migration
{
    /// <summary>
    /// What the acts that already exist get, so the column can be <c>NOT NULL</c> — Postgres writes it as
    /// <c>-infinity</c>, which sorts before every real act date until someone fills it in.
    /// </summary>
    private static readonly DateOnly Backfill = DateOnly.MinValue;

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Acts_CaseId",
            table: "Acts");

        foreach (var column in new[] { "Ordinal", "Drafted", "Sent", "Delivered", "Received" })
        {
            migrationBuilder.DropColumn(
                name: column,
                table: "Acts");
        }

        migrationBuilder.AddColumn<DateOnly>(
            name: "Date",
            table: "Acts",
            type: "date",
            nullable: false,
            defaultValue: Backfill);

        // The backfill filled the rows that were already there; left as a column default it would also let
        // a later insert omit the date the model requires.
        migrationBuilder.Sql("""ALTER TABLE "Acts" ALTER COLUMN "Date" DROP DEFAULT;""");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_CaseId_Date",
            table: "Acts",
            columns: ["CaseId", "Date"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Acts_CaseId_Date",
            table: "Acts");

        migrationBuilder.DropColumn(
            name: "Date",
            table: "Acts");

        migrationBuilder.AddColumn<int>(
            name: "Ordinal",
            table: "Acts",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        foreach (var column in new[] { "Drafted", "Sent", "Delivered", "Received" })
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: column,
                table: "Acts",
                type: "date",
                nullable: true);
        }

        migrationBuilder.CreateIndex(
            name: "IX_Acts_CaseId",
            table: "Acts",
            column: "CaseId");
    }
}
