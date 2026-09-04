using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class FileStoragePath : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StoragePath",
            table: "FileAssets",
            type: "character varying(256)",
            maxLength: 256,
            nullable: false,
            defaultValue: "");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "StoragePath",
            table: "FileAssets");
    }
}
