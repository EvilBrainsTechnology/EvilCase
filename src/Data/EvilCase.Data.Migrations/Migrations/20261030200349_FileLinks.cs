using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

// Outside the history like ApiTokens: the rows carry the secret hash and die on use.
public partial class FileLinks : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateFileLinks(migrationBuilder);
        CreateFileLinkIndexes(migrationBuilder);

        migrationBuilder.Sql(
            """CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "FileLinks" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FileLinks");
    }

    private static void CreateFileLinks(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FileLinks",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SecretHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ApiTokenId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_FileLinks", static x => x.Id);
                table.ForeignKey(
                    name: "FK_FileLinks_ApiTokens_ApiTokenId",
                    column: static x => x.ApiTokenId,
                    principalTable: "ApiTokens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FileLinks_Tenants_TenantId",
                    column: static x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FileLinks_Users_UserId",
                    column: static x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    private static void CreateFileLinkIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_FileLinks_ApiTokenId",
            table: "FileLinks",
            column: "ApiTokenId");

        migrationBuilder.CreateIndex(
            name: "IX_FileLinks_Expires",
            table: "FileLinks",
            column: "Expires");

        migrationBuilder.CreateIndex(
            name: "IX_FileLinks_SecretHash",
            table: "FileLinks",
            column: "SecretHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileLinks_TenantId",
            table: "FileLinks",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_FileLinks_UserId",
            table: "FileLinks",
            column: "UserId");
    }
}
