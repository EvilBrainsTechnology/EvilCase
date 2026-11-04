using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

/// <summary>
/// A write of a comment, a file, a file version or a label assignment stamps the Updated of its case or act: the
/// update sets nothing itself, and stamp_timestamps sets the clock time on it.
/// </summary>
public partial class StampOwnerOnAttachedWrites : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateStampOwnerFunction(migrationBuilder);
        CreateStampLabelledOwnerFunction(migrationBuilder);
        CreateStampFileOwnerFunction(migrationBuilder);

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
            """);
    }

    private static void CreateStampOwnerFunction(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_owner()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                written record;
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    written := OLD;
                ELSE
                    written := NEW;
                END IF;

                UPDATE "Cases" SET "Updated" = NULL WHERE "Id" = written."CaseId";
                UPDATE "Acts" SET "Updated" = NULL WHERE "Id" = written."ActId";

                RETURN NULL;
            END;
            $$;
            """);
    }

    // A label deleted in settings takes its assignments along and stamps nothing.
    private static void CreateStampLabelledOwnerFunction(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_labelled_owner()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                written record;
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    IF NOT EXISTS (SELECT FROM "Labels" WHERE "Id" = OLD."LabelId") THEN
                        RETURN NULL;
                    END IF;

                    written := OLD;
                ELSE
                    written := NEW;
                END IF;

                UPDATE "Cases" SET "Updated" = NULL WHERE "Id" = written."CaseId";
                UPDATE "Acts" SET "Updated" = NULL WHERE "Id" = written."ActId";

                RETURN NULL;
            END;
            $$;
            """);
    }

    private static void CreateStampFileOwnerFunction(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION stamp_file_owner()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            DECLARE
                written record;
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    written := OLD;
                ELSE
                    written := NEW;
                END IF;

                UPDATE "Cases" SET "Updated" = NULL WHERE "Id" = (SELECT "CaseId" FROM "FileAssets" WHERE "Id" = written."FileAssetId");
                UPDATE "Acts" SET "Updated" = NULL WHERE "Id" = (SELECT "ActId" FROM "FileAssets" WHERE "Id" = written."FileAssetId");

                RETURN NULL;
            END;
            $$;
            """);
    }
}
