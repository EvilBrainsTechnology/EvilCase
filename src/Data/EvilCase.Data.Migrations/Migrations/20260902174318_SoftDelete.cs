using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class SoftDelete : Migration
{
    private static readonly string[] StampedTables =
    [
        "Accounts",
        "Acts",
        "Cases",
        "Comments",
        "Contacts",
        "ExternalActNumbers",
        "ExternalCaseNumbers",
        "FileAssets",
        "Tenants",
        "Users",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in StampedTables)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deleted",
                table: table,
                type: "timestamp with time zone",
                nullable: true);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in StampedTables)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: table);
        }
    }
}
