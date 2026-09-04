using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class DatabaseStampedTimestamps : Migration
{
    private static readonly string[] StampedTables =
    [
        "Accounts",
        "Acts",
        "Cases",
        "Comments",
        "Contacts",
        "ExternalActNumbers",
        "ExternalCaseNumbers",
        "FileAssets",
        "RefreshTokens",
        "Tenants",
        "Users",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION stamp_timestamps()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'INSERT' THEN
                    NEW."Created" := clock_timestamp();
                    NEW."Updated" := NULL;
                ELSE
                    NEW."Created" := OLD."Created";
                    NEW."Updated" := clock_timestamp();
                END IF;

                RETURN NEW;
            END;
            $$;
            """);

        foreach (var table in StampedTables)
            migrationBuilder.Sql($"""CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "{table}" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in StampedTables)
            migrationBuilder.Sql($"""DROP TRIGGER stamp_timestamps ON "{table}";""");

        migrationBuilder.Sql("DROP FUNCTION stamp_timestamps();");
    }
}
