using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class ActStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Status",
            table: "Acts",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);

        // The record trigger inserts the row by position, so the mirror takes the column last as well.
        migrationBuilder.Sql("""ALTER TABLE history."Acts" ADD COLUMN "Status" character varying(10);""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""ALTER TABLE history."Acts" DROP COLUMN "Status";""");

        migrationBuilder.DropColumn(name: "Status", table: "Acts");
    }
}
