using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class Comments : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateComments(migrationBuilder);
        CreateCommentIndexes(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Comments");
    }

    private static void CreateComments(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CaseId = table.Column<long>(type: "bigint", nullable: true),
                ActId = table.Column<long>(type: "bigint", nullable: true),
                Body = table.Column<string>(type: "text", nullable: false),
                AuthorUserId = table.Column<long>(type: "bigint", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.Id);
                table.CheckConstraint("CK_Comments_OnACaseOrAnAct", "(\"CaseId\" IS NULL) <> (\"ActId\" IS NULL)");
                table.ForeignKey(
                    name: "FK_Comments_Acts_ActId",
                    column: x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Comments_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Comments_Users_AuthorUserId",
                    column: x => x.AuthorUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    private static void CreateCommentIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Comments_ActId",
            table: "Comments",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_AuthorUserId",
            table: "Comments",
            column: "AuthorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_CaseId",
            table: "Comments",
            column: "CaseId");
    }
}
