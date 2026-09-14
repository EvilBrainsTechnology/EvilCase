using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CascadeSubordinateCases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases");

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId",
            principalTable: "Cases",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases");

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId",
            principalTable: "Cases",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }
}
