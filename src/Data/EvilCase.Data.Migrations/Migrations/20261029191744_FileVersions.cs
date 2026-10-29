using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class FileVersions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateFileVersions(migrationBuilder);
        CopyFirstVersions(migrationBuilder);
        CreateFileVersionIndexes(migrationBuilder);
        CreateFileNameIndexes(migrationBuilder);
        DropBlobColumns(migrationBuilder);
        RecordFileAssetsByName(migrationBuilder);
        StampTimestamps(migrationBuilder);
        RecordHistory(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""DROP TRIGGER record_history ON "FileVersions";""");
        migrationBuilder.Sql("""DROP FUNCTION history."record_FileVersions"();""");
        migrationBuilder.Sql("""DROP TABLE history."FileVersions";""");

        AddBlobColumns(migrationBuilder);

        RecordFileAssetsWithBlobColumns(migrationBuilder);
        CopyCurrentVersionsBack(migrationBuilder);

        migrationBuilder.Sql("""DROP INDEX "IX_FileAssets_TenantId_CaseId_FileName";""");
        migrationBuilder.Sql("""DROP INDEX "IX_FileAssets_TenantId_ActId_FileName";""");
        migrationBuilder.Sql("DROP FUNCTION file_name_key(text);");

        migrationBuilder.DropTable(name: "FileVersions");
    }

    private static void AddBlobColumns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ContentHash",
            table: "FileAssets",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<long>(
            name: "SizeBytes",
            table: "FileAssets",
            type: "bigint",
            nullable: false,
            defaultValue: 0L);

        migrationBuilder.AddColumn<string>(
            name: "MediaType",
            table: "FileAssets",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StoragePath",
            table: "FileAssets",
            type: "character varying(256)",
            maxLength: 256,
            nullable: false,
            defaultValue: "");
    }

    private static void RecordFileAssetsWithBlobColumns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION history."record_FileAssets"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId", "CaseId", "ActId", "FileName", "ContentHash", "SizeBytes", "MediaType", "StoragePath", "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), OLD."Id", OLD."TenantId", OLD."UserId", OLD."CaseId", OLD."ActId", OLD."FileName", OLD."ContentHash", OLD."SizeBytes", OLD."MediaType", OLD."StoragePath", OLD."Created", OLD."Updated");
                ELSE
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId", "CaseId", "ActId", "FileName", "ContentHash", "SizeBytes", "MediaType", "StoragePath", "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), NEW."Id", NEW."TenantId", NEW."UserId", NEW."CaseId", NEW."ActId", NEW."FileName", NEW."ContentHash", NEW."SizeBytes", NEW."MediaType", NEW."StoragePath", NEW."Created", NEW."Updated");
                END IF;

                RETURN NULL;
            END;
            $$;
            """);
    }

    private static void CopyCurrentVersionsBack(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "FileAssets" f SET "ContentHash" = v."ContentHash", "SizeBytes" = v."SizeBytes", "MediaType" = v."MediaType", "StoragePath" = v."StoragePath"
            FROM "FileVersions" v
            WHERE v."FileAssetId" = f."Id" AND v."Number" = (SELECT max(c."Number") FROM "FileVersions" c WHERE c."FileAssetId" = f."Id");
            """);
    }

    private static void CreateFileVersions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FileVersions",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                FileAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                MediaType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                StoragePath = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_FileVersions", static x => x.Id);
                table.ForeignKey(
                    name: "FK_FileVersions_FileAssets_FileAssetId",
                    column: static x => x.FileAssetId,
                    principalTable: "FileAssets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FileVersions_Tenants_TenantId",
                    column: static x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_FileVersions_Users_UserId",
                    column: static x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    // Version 1 takes the file's id, so its blob keeps the name it was written under; Created keeps the upload moment because the stamp trigger arrives after the copy.
    private static void CopyFirstVersions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO "FileVersions" ("Id", "TenantId", "UserId", "FileAssetId", "Number", "ContentHash", "SizeBytes", "MediaType", "StoragePath", "Created")
            SELECT "Id", "TenantId", "UserId", "Id", 1, "ContentHash", "SizeBytes", "MediaType", "StoragePath", "Created"
            FROM "FileAssets";
            """);
    }

    private static void CreateFileVersionIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_FileVersions_FileAssetId",
            table: "FileVersions",
            column: "FileAssetId");

        migrationBuilder.CreateIndex(
            name: "IX_FileVersions_TenantId_FileAssetId_Number",
            table: "FileVersions",
            columns: ["TenantId", "FileAssetId", "Number"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_FileVersions_UserId",
            table: "FileVersions",
            column: "UserId");
    }

    private static void CreateFileNameIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """CREATE FUNCTION file_name_key(text) RETURNS text LANGUAGE sql IMMUTABLE STRICT PARALLEL SAFE AS $$ SELECT lower($1 COLLATE "cs-CZ-x-icu") $$;""");

        migrationBuilder.Sql(
            """CREATE UNIQUE INDEX "IX_FileAssets_TenantId_CaseId_FileName" ON "FileAssets" ("TenantId", "CaseId", file_name_key("FileName"));""");

        migrationBuilder.Sql(
            """CREATE UNIQUE INDEX "IX_FileAssets_TenantId_ActId_FileName" ON "FileAssets" ("TenantId", "ActId", file_name_key("FileName"));""");
    }

    private static void DropBlobColumns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ContentHash",
            table: "FileAssets");

        migrationBuilder.DropColumn(
            name: "MediaType",
            table: "FileAssets");

        migrationBuilder.DropColumn(
            name: "SizeBytes",
            table: "FileAssets");

        migrationBuilder.DropColumn(
            name: "StoragePath",
            table: "FileAssets");
    }

    // The dropped columns stay in the mirror, empty from now on, so the record function names its columns.
    private static void RecordFileAssetsByName(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """ALTER TABLE history."FileAssets" ALTER COLUMN "ContentHash" DROP NOT NULL, ALTER COLUMN "SizeBytes" DROP NOT NULL, ALTER COLUMN "StoragePath" DROP NOT NULL;""");

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION history."record_FileAssets"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId", "CaseId", "ActId", "FileName", "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), OLD."Id", OLD."TenantId", OLD."UserId", OLD."CaseId", OLD."ActId", OLD."FileName", OLD."Created", OLD."Updated");
                ELSE
                    INSERT INTO history."FileAssets" ("HistoryId", "Operation", "OperationAt", "OperationUserId", "Id", "TenantId", "UserId", "CaseId", "ActId", "FileName", "Created", "Updated")
                    VALUES (uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), NEW."Id", NEW."TenantId", NEW."UserId", NEW."CaseId", NEW."ActId", NEW."FileName", NEW."Created", NEW."Updated");
                END IF;

                RETURN NULL;
            END;
            $$;
            """);
    }

    private static void StampTimestamps(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "FileVersions" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
    }

    private static void RecordHistory(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE history."FileVersions" (
                "HistoryId" uuid NOT NULL PRIMARY KEY,
                "Operation" character varying(6) NOT NULL,
                "OperationAt" timestamp with time zone NOT NULL,
                "OperationUserId" uuid,
                LIKE public."FileVersions"
            );
            """);

        migrationBuilder.Sql("""CREATE INDEX "IX_FileVersions_Id" ON history."FileVersions" ("Id");""");

        migrationBuilder.Sql(
            """
            CREATE FUNCTION history."record_FileVersions"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    INSERT INTO history."FileVersions"
                    SELECT uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), (OLD).*;
                ELSE
                    INSERT INTO history."FileVersions"
                    SELECT uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), (NEW).*;
                END IF;

                RETURN NULL;
            END;
            $$;
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER record_history AFTER INSERT OR UPDATE OR DELETE ON "FileVersions"
            FOR EACH ROW EXECUTE FUNCTION history."record_FileVersions"();
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER reject_write BEFORE UPDATE OR DELETE ON history."FileVersions"
            FOR EACH ROW EXECUTE FUNCTION history.reject_write();
            """);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER reject_truncate BEFORE TRUNCATE ON history."FileVersions"
            FOR EACH STATEMENT EXECUTE FUNCTION history.reject_write();
            """);
    }
}
