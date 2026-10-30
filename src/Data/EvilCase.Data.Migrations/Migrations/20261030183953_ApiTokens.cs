using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

// Outside the history like RefreshTokens: the rows carry the token hash.
public partial class ApiTokens : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ApiTokens",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Scope = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_ApiTokens", static x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiTokens_Users_UserId",
                    column: static x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ApiTokens_TokenHash",
            table: "ApiTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ApiTokens_UserId",
            table: "ApiTokens",
            column: "UserId");

        migrationBuilder.Sql(
            """CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "ApiTokens" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ApiTokens");
    }
}
