using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

/// <summary>
/// A file hangs on an act alone. The files of cases go with their versions and their upload links, without stamping
/// their case; the history records the deletes and the blobs stay on disk. Down restores the schema, not the files.
/// </summary>
public partial class FilesOnActsOnly : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER stamp_owner ON "FileAssets";
            DROP TRIGGER stamp_owner ON "FileVersions";
            """);

        migrationBuilder.Sql("""DELETE FROM "FileLinks" WHERE "Kind" = 'CaseUpload';""");
        migrationBuilder.Sql("""DELETE FROM "FileAssets" WHERE "CaseId" IS NOT NULL;""");

        RecordFileAssets(migrationBuilder, withCase: false);

        migrationBuilder.DropCheckConstraint(
            name: "CK_FileAssets_OnACaseOrAnAct",
            table: "FileAssets");

        migrationBuilder.DropIndex(
            name: "IX_FileAssets_CaseId",
            table: "FileAssets");

        migrationBuilder.DropIndex(
            name: "IX_FileAssets_TenantId_CaseId_IsMain",
            table: "FileAssets");

        migrationBuilder.Sql("""DROP INDEX "IX_FileAssets_TenantId_CaseId_FileName";""");

        migrationBuilder.DropForeignKey(
            name: "FK_FileAssets_Cases_CaseId",
            table: "FileAssets");

        migrationBuilder.DropColumn(
            name: "CaseId",
            table: "FileAssets");

        migrationBuilder.AlterColumn<Guid>(
            name: "ActId",
            table: "FileAssets",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        StampActs(migrationBuilder);

        migrationBuilder.Sql("DROP FUNCTION stamp_file_owner();");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER stamp_act ON "FileAssets";
            DROP TRIGGER stamp_act ON "FileVersions";

            DROP FUNCTION stamp_file_act();
            DROP FUNCTION stamp_file_version_act();
            """);

        migrationBuilder.AlterColumn<Guid>(
            name: "ActId",
            table: "FileAssets",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        migrationBuilder.AddColumn<Guid>(
            name: "CaseId",
            table: "FileAssets",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_CaseId",
            table: "FileAssets",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_TenantId_CaseId_IsMain",
            table: "FileAssets",
            columns: ["TenantId", "CaseId"],
            unique: true,
            filter: "\"IsMain\"");

        migrationBuilder.Sql(
            """CREATE UNIQUE INDEX "IX_FileAssets_TenantId_CaseId_FileName" ON "FileAssets" ("TenantId", "CaseId", file_name_key("FileName"));""");

        migrationBuilder.AddCheckConstraint(
            name: "CK_FileAssets_OnACaseOrAnAct",
            table: "FileAssets",
            sql: "(\"CaseId\" IS NULL) <> (\"ActId\" IS NULL)");

        migrationBuilder.AddForeignKey(
            name: "FK_FileAssets_Cases_CaseId",
            table: "FileAssets",
            column: "CaseId",
            principalTable: "Cases",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        RecordFileAssets(migrationBuilder, withCase: true);
        StampOwners(migrationBuilder);
    }

    private static void RecordFileAssets(MigrationBuilder migrationBuilder, bool withCase)
    {
        var column = withCase ? @", ""CaseId""" : "";
        var oldValue = withCase ? @", OLD.""CaseId""" : "";
        var newValue = withCase ? @", NEW.""CaseId""" : "";

        migrationBuilder.Sql(
            $"""
            CREATE OR REPLACE FUNCTION history."record_FileAssets"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId"{column}, "ActId", "FileName", "IsMain", "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), OLD."Id", OLD."TenantId", OLD."UserId"{oldValue}, OLD."ActId", OLD."FileName", OLD."IsMain", OLD."Created", OLD."Updated");
                ELSE
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId"{column}, "ActId", "FileName", "IsMain", "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), NEW."Id", NEW."TenantId", NEW."UserId"{newValue}, NEW."ActId", NEW."FileName", NEW."IsMain", NEW."Created", NEW."Updated");
                END IF;

                RETURN NULL;
            END;
            $$;
            """);
    }

    private static void StampActs(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_file_act()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                UPDATE "Acts" SET "Updated" = clock_timestamp()
                WHERE "Id" = CASE WHEN TG_OP = 'DELETE' THEN OLD."ActId" ELSE NEW."ActId" END;

                RETURN NULL;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_file_version_act()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                UPDATE "Acts" SET "Updated" = clock_timestamp()
                WHERE "Id" = (
                    SELECT "ActId" FROM "FileAssets"
                    WHERE "Id" = CASE WHEN TG_OP = 'DELETE' THEN OLD."FileAssetId" ELSE NEW."FileAssetId" END);

                RETURN NULL;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER stamp_act AFTER INSERT OR UPDATE OR DELETE ON "FileAssets" FOR EACH ROW EXECUTE FUNCTION stamp_file_act();
            CREATE TRIGGER stamp_act AFTER INSERT OR DELETE ON "FileVersions" FOR EACH ROW EXECUTE FUNCTION stamp_file_version_act();
            """);
    }

    private static void StampOwners(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_file_owner()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                case_id uuid;
                act_id uuid;
            BEGIN
                SELECT "CaseId", "ActId" INTO case_id, act_id
                FROM "FileAssets"
                WHERE "Id" = CASE WHEN TG_OP = 'DELETE' THEN OLD."FileAssetId" ELSE NEW."FileAssetId" END;

                PERFORM stamp_owner_of(case_id, act_id);

                RETURN NULL;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER stamp_owner AFTER INSERT OR UPDATE OR DELETE ON "FileAssets" FOR EACH ROW EXECUTE FUNCTION stamp_owner();
            CREATE TRIGGER stamp_owner AFTER INSERT OR DELETE ON "FileVersions" FOR EACH ROW EXECUTE FUNCTION stamp_file_owner();
            """);
    }
}
