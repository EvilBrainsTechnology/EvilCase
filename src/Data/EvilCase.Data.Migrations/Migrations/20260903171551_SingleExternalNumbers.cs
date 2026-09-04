using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class SingleExternalNumbers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ExternalCaseNumber",
            table: "Cases",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ExternalActNumber",
            table: "Acts",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        BackfillCaseNumbers(migrationBuilder);
        BackfillActNumbers(migrationBuilder);

        migrationBuilder.DropTable(name: "ExternalCaseNumbers");
        migrationBuilder.DropTable(name: "ExternalActNumbers");
    }

    // Rows are not restored: the contact that assigned each value is recorded nowhere after Up.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        CreateExternalCaseNumbers(migrationBuilder);
        CreateExternalCaseNumberIndexes(migrationBuilder);
        CreateExternalActNumbers(migrationBuilder);
        CreateExternalActNumberIndexes(migrationBuilder);
        CreateSearchIndexesAndTriggers(migrationBuilder);

        migrationBuilder.DropColumn(
            name: "ExternalCaseNumber",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "ExternalActNumber",
            table: "Acts");
    }

    // The backfill keeps the mark each owner accrued first — the one its detail listed at the top. The
    // stamp trigger is off for it: a migration is not a change the user made.
    private static void BackfillCaseNumbers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Cases" DISABLE TRIGGER stamp_timestamps;

            UPDATE "Cases" AS c
            SET "ExternalCaseNumber" = n."Value"
            FROM (
                SELECT DISTINCT ON ("CaseId") "CaseId", "Value"
                FROM "ExternalCaseNumbers"
                ORDER BY "CaseId", "Created", "Id"
            ) AS n
            WHERE n."CaseId" = c."Id";

            ALTER TABLE "Cases" ENABLE TRIGGER stamp_timestamps;
            """);
    }

    private static void BackfillActNumbers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Acts" DISABLE TRIGGER stamp_timestamps;

            UPDATE "Acts" AS a
            SET "ExternalActNumber" = n."Value"
            FROM (
                SELECT DISTINCT ON ("ActId") "ActId", "Value"
                FROM "ExternalActNumbers"
                ORDER BY "ActId", "Created", "Id"
            ) AS n
            WHERE n."ActId" = a."Id";

            ALTER TABLE "Acts" ENABLE TRIGGER stamp_timestamps;
            """);
    }

    private static void CreateExternalCaseNumbers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExternalCaseNumbers",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AssignedByContactId = table.Column<Guid>(type: "uuid", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_ExternalCaseNumbers", static x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Cases_CaseId",
                    column: static x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Contacts_AssignedByContactId",
                    column: static x => x.AssignedByContactId,
                    principalTable: "Contacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Tenants_TenantId",
                    column: static x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Users_UserId",
                    column: static x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateExternalCaseNumberIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_AssignedByContactId",
            table: "ExternalCaseNumbers",
            column: "AssignedByContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_CaseId",
            table: "ExternalCaseNumbers",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_TenantId_CaseId_Value",
            table: "ExternalCaseNumbers",
            columns: ["TenantId", "CaseId", "Value"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_UserId",
            table: "ExternalCaseNumbers",
            column: "UserId");
    }

    private static void CreateExternalActNumbers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExternalActNumbers",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ActId = table.Column<Guid>(type: "uuid", nullable: false),
                Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AssignedByContactId = table.Column<Guid>(type: "uuid", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_ExternalActNumbers", static x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Acts_ActId",
                    column: static x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Contacts_AssignedByContactId",
                    column: static x => x.AssignedByContactId,
                    principalTable: "Contacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Tenants_TenantId",
                    column: static x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Users_UserId",
                    column: static x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateExternalActNumberIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_ActId",
            table: "ExternalActNumbers",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_AssignedByContactId",
            table: "ExternalActNumbers",
            column: "AssignedByContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_TenantId_ActId_Value",
            table: "ExternalActNumbers",
            columns: ["TenantId", "ActId", "Value"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_UserId",
            table: "ExternalActNumbers",
            column: "UserId");
    }

    private static void CreateSearchIndexesAndTriggers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""CREATE INDEX "IX_ExternalCaseNumbers_Value_Trigram" ON "ExternalCaseNumbers" USING GIN ("Value" gin_trgm_ops);""");
        migrationBuilder.Sql("""CREATE INDEX "IX_ExternalActNumbers_Value_Trigram" ON "ExternalActNumbers" USING GIN ("Value" gin_trgm_ops);""");
        migrationBuilder.Sql("""CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "ExternalCaseNumbers" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
        migrationBuilder.Sql("""CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "ExternalActNumbers" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
    }
}
