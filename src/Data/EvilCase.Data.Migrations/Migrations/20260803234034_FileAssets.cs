using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class FileAssets : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateFileAssets(migrationBuilder);
        CreateActFileLinks(migrationBuilder);
        CreateFileIndexes(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ActFileLinks");

        migrationBuilder.DropTable(
            name: "FileAssets");
    }

    private static void CreateFileAssets(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FileAssets",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OwnerId = table.Column<long>(type: "bigint", nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                MediaType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileAssets", x => x.Id);
                table.ForeignKey(
                    name: "FK_FileAssets_Users_OwnerId",
                    column: x => x.OwnerId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    private static void CreateActFileLinks(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ActFileLinks",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ActId = table.Column<long>(type: "bigint", nullable: false),
                FileAssetId = table.Column<long>(type: "bigint", nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                OriginatingActId = table.Column<long>(type: "bigint", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActFileLinks", x => x.Id);
                table.ForeignKey(
                    name: "FK_ActFileLinks_Acts_ActId",
                    column: x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ActFileLinks_Acts_OriginatingActId",
                    column: x => x.OriginatingActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ActFileLinks_FileAssets_FileAssetId",
                    column: x => x.FileAssetId,
                    principalTable: "FileAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateFileIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ActFileLinks_ActId",
            table: "ActFileLinks",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_ActFileLinks_FileAssetId",
            table: "ActFileLinks",
            column: "FileAssetId");

        migrationBuilder.CreateIndex(
            name: "IX_ActFileLinks_OriginatingActId",
            table: "ActFileLinks",
            column: "OriginatingActId");

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_OwnerId",
            table: "FileAssets",
            column: "OwnerId");

        // Deduplication is within one owner, never across owners.
        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_OwnerId_ContentHash",
            table: "FileAssets",
            columns: ["OwnerId", "ContentHash"],
            unique: true);
    }
}
