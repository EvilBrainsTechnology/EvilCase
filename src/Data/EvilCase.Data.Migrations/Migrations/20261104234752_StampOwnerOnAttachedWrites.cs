using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

/// <summary>
/// A write of a comment, a file, a file version or a label assignment stamps the Updated of its case or act.
/// </summary>
public partial class StampOwnerOnAttachedWrites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_owner_of(case_id uuid, act_id uuid)
            RETURNS void
            LANGUAGE sql
            AS $$
                UPDATE "Cases" SET "Updated" = clock_timestamp() WHERE "Id" = case_id;
                UPDATE "Acts" SET "Updated" = clock_timestamp() WHERE "Id" = act_id;
            $$;
            """);

        CreateTriggerFunctions(migrationBuilder);

        migrationBuilder.Sql(
            """
            CREATE TRIGGER stamp_owner AFTER INSERT OR UPDATE OR DELETE ON "Comments" FOR EACH ROW EXECUTE FUNCTION stamp_owner();
            CREATE TRIGGER stamp_owner AFTER INSERT OR UPDATE OR DELETE ON "FileAssets" FOR EACH ROW EXECUTE FUNCTION stamp_owner();
            CREATE TRIGGER stamp_owner AFTER INSERT OR DELETE ON "LabelAssignments" FOR EACH ROW EXECUTE FUNCTION stamp_labelled_owner();
            CREATE TRIGGER stamp_owner AFTER INSERT OR DELETE ON "FileVersions" FOR EACH ROW EXECUTE FUNCTION stamp_file_owner();
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TRIGGER stamp_owner ON "Comments";
            DROP TRIGGER stamp_owner ON "FileAssets";
            DROP TRIGGER stamp_owner ON "LabelAssignments";
            DROP TRIGGER stamp_owner ON "FileVersions";

            DROP FUNCTION stamp_owner();
            DROP FUNCTION stamp_labelled_owner();
            DROP FUNCTION stamp_file_owner();
            DROP FUNCTION stamp_owner_of(uuid, uuid);
            """);
    }

    private static void CreateTriggerFunctions(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_owner()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    PERFORM stamp_owner_of(OLD."CaseId", OLD."ActId");
                ELSE
                    PERFORM stamp_owner_of(NEW."CaseId", NEW."ActId");
                END IF;

                RETURN NULL;
            END;
            $$;
            """);

        // A label deleted in settings takes its assignments along and stamps nothing.
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_labelled_owner()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'INSERT' THEN
                    PERFORM stamp_owner_of(NEW."CaseId", NEW."ActId");
                ELSIF EXISTS (SELECT FROM "Labels" WHERE "Id" = OLD."LabelId") THEN
                    PERFORM stamp_owner_of(OLD."CaseId", OLD."ActId");
                END IF;

                RETURN NULL;
            END;
            $$;
            """);

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
    }
}
