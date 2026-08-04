using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class FileReferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        DropTheRoleAndTheOriginatingAct(migrationBuilder);
        RenameLinksToReferences(migrationBuilder);
        AddThePrimaryActAndTheOriginalName(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Down brings the columns back empty: nothing stores a role or an originating act after Up.
        DropThePrimaryActAndTheOriginalName(migrationBuilder);
        RenameReferencesToLinks(migrationBuilder);
        RestoreTheRoleAndTheOriginatingAct(migrationBuilder);
    }

    private static void DropTheRoleAndTheOriginatingAct(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ActFileLinks_Acts_OriginatingActId",
            table: "ActFileLinks");

        migrationBuilder.DropIndex(
            name: "IX_ActFileLinks_OriginatingActId",
            table: "ActFileLinks");

        foreach (var column in new[] { "Role", "OriginatingActId" })
        {
            migrationBuilder.DropColumn(
                name: column,
                table: "ActFileLinks");
        }
    }

    private static void RenameLinksToReferences(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "ActFileLinks",
            newName: "ActFileReferences");

        // Renaming the index of a primary key renames its constraint with it.
        migrationBuilder.RenameIndex(
            name: "PK_ActFileLinks",
            newName: "PK_ActFileReferences",
            table: "ActFileReferences");

        foreach (var suffix in new[] { "ActId", "FileAssetId" })
        {
            migrationBuilder.RenameIndex(
                name: $"IX_ActFileLinks_{suffix}",
                newName: $"IX_ActFileReferences_{suffix}",
                table: "ActFileReferences");
        }

        // A foreign key has no index to rename, and EF models no rename for it.
        foreach (var suffix in new[] { "Acts_ActId", "FileAssets_FileAssetId" })
        {
            migrationBuilder.Sql(
                $"""ALTER TABLE "ActFileReferences" RENAME CONSTRAINT "FK_ActFileLinks_{suffix}" TO "FK_ActFileReferences_{suffix}";""");
        }
    }

    private static void AddThePrimaryActAndTheOriginalName(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>(
            name: "ActId",
            table: "FileAssets",
            type: "bigint",
            nullable: false);

        migrationBuilder.AddColumn<string>(
            name: "FileName",
            table: "FileAssets",
            type: "character varying(256)",
            maxLength: 256,
            nullable: false);

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_ActId",
            table: "FileAssets",
            column: "ActId");

        migrationBuilder.AddForeignKey(
            name: "FK_FileAssets_Acts_ActId",
            table: "FileAssets",
            column: "ActId",
            principalTable: "Acts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    private static void DropThePrimaryActAndTheOriginalName(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_FileAssets_Acts_ActId",
            table: "FileAssets");

        migrationBuilder.DropIndex(
            name: "IX_FileAssets_ActId",
            table: "FileAssets");

        foreach (var column in new[] { "ActId", "FileName" })
        {
            migrationBuilder.DropColumn(
                name: column,
                table: "FileAssets");
        }
    }

    private static void RenameReferencesToLinks(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "ActFileReferences",
            newName: "ActFileLinks");

        migrationBuilder.RenameIndex(
            name: "PK_ActFileReferences",
            newName: "PK_ActFileLinks",
            table: "ActFileLinks");

        foreach (var suffix in new[] { "ActId", "FileAssetId" })
        {
            migrationBuilder.RenameIndex(
                name: $"IX_ActFileReferences_{suffix}",
                newName: $"IX_ActFileLinks_{suffix}",
                table: "ActFileLinks");
        }

        foreach (var suffix in new[] { "Acts_ActId", "FileAssets_FileAssetId" })
        {
            migrationBuilder.Sql(
                $"""ALTER TABLE "ActFileLinks" RENAME CONSTRAINT "FK_ActFileReferences_{suffix}" TO "FK_ActFileLinks_{suffix}";""");
        }
    }

    private static void RestoreTheRoleAndTheOriginatingAct(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Role",
            table: "ActFileLinks",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false);

        migrationBuilder.AddColumn<long>(
            name: "OriginatingActId",
            table: "ActFileLinks",
            type: "bigint",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ActFileLinks_OriginatingActId",
            table: "ActFileLinks",
            column: "OriginatingActId");

        migrationBuilder.AddForeignKey(
            name: "FK_ActFileLinks_Acts_OriginatingActId",
            table: "ActFileLinks",
            column: "OriginatingActId",
            principalTable: "Acts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }
}
