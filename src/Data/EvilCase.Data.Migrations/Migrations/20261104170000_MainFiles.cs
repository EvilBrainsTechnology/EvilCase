using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class MainFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsMain",
            table: "FileAssets",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""ALTER TABLE history."FileAssets" ADD COLUMN "IsMain" boolean;""");

        RecordFileAssets(migrationBuilder, withMain: true);
        MarkOldestFilesMain(migrationBuilder);

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_TenantId_ActId_IsMain",
            table: "FileAssets",
            columns: ["TenantId", "ActId"],
            unique: true,
            filter: "\"IsMain\"");

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_TenantId_CaseId_IsMain",
            table: "FileAssets",
            columns: ["TenantId", "CaseId"],
            unique: true,
            filter: "\"IsMain\"");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_FileAssets_TenantId_ActId_IsMain",
            table: "FileAssets");

        migrationBuilder.DropIndex(
            name: "IX_FileAssets_TenantId_CaseId_IsMain",
            table: "FileAssets");

        RecordFileAssets(migrationBuilder, withMain: false);

        migrationBuilder.DropColumn(
            name: "IsMain",
            table: "FileAssets");

        migrationBuilder.Sql("""ALTER TABLE history."FileAssets" DROP COLUMN "IsMain";""");
    }

    private static void RecordFileAssets(MigrationBuilder migrationBuilder, bool withMain)
    {
        var column = withMain ? @", ""IsMain""" : "";
        var oldValue = withMain ? @", OLD.""IsMain""" : "";
        var newValue = withMain ? @", NEW.""IsMain""" : "";

        migrationBuilder.Sql(
            $"""
            CREATE OR REPLACE FUNCTION history."record_FileAssets"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId", "CaseId", "ActId", "FileName"{column}, "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), OLD."Id", OLD."TenantId", OLD."UserId", OLD."CaseId", OLD."ActId", OLD."FileName"{oldValue}, OLD."Created", OLD."Updated");
                ELSE
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId", "CaseId", "ActId", "FileName"{column}, "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), NEW."Id", NEW."TenantId", NEW."UserId", NEW."CaseId", NEW."ActId", NEW."FileName"{newValue}, NEW."Created", NEW."Updated");
                END IF;

                RETURN NULL;
            END;
            $$;
            """);
    }

    // The oldest file of every case and act, in the order of the files list, becomes its main file.
    private static void MarkOldestFilesMain(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "FileAssets" DISABLE TRIGGER stamp_timestamps;

            UPDATE "FileAssets" SET "IsMain" = true
            WHERE "Id" IN (
                SELECT DISTINCT ON ("TenantId", "CaseId", "ActId") "Id"
                FROM "FileAssets"
                ORDER BY "TenantId", "CaseId", "ActId", "Created", "Id");

            ALTER TABLE "FileAssets" ENABLE TRIGGER stamp_timestamps;
            """);
    }
}
