using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class DatabaseRecordedHistory : Migration
{
    // RefreshTokens rotate on every token refresh and carry the token hash, so they stay out.
    private static readonly string[] HistorizedTables =
    [
        "Accounts",
        "Tenants",
        "Users",
        "Contacts",
        "Cases",
        "Acts",
        "FileAssets",
        "Comments",
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE SCHEMA history;");

        CreateOperationUserIdFunction(migrationBuilder);
        CreateRejectWriteFunction(migrationBuilder);

        foreach (var table in HistorizedTables)
        {
            CreateMirrorTable(migrationBuilder, table);
            CreateRecordFunction(migrationBuilder, table);
            CreateRecordTrigger(migrationBuilder, table);
            CreateAppendOnlyTriggers(migrationBuilder, table);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in HistorizedTables)
            migrationBuilder.Sql($"""DROP TRIGGER record_history ON "{table}";""");

        migrationBuilder.Sql("DROP SCHEMA history CASCADE;");
    }

    private static void CreateOperationUserIdFunction(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION history.operation_user_id()
            RETURNS uuid
            LANGUAGE sql
            STABLE
            AS $$ SELECT NULLIF(current_setting('evilcase.user_id', true), '')::uuid $$;
            """);
    }

    private static void CreateRejectWriteFunction(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE FUNCTION history.reject_write()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                RAISE EXCEPTION 'history.% is append only', TG_TABLE_NAME;
            END;
            $$;
            """);
    }

    private static void CreateMirrorTable(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql(
            $"""
            CREATE TABLE history."{table}" (
                "HistoryId" uuid NOT NULL PRIMARY KEY,
                "Operation" character varying(6) NOT NULL,
                "OperationAt" timestamp with time zone NOT NULL,
                "OperationUserId" uuid,
                LIKE public."{table}"
            );
            """);

        migrationBuilder.Sql($"""CREATE INDEX "IX_{table}_Id" ON history."{table}" ("Id");""");
    }

    private static void CreateRecordFunction(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql(
            $"""
            CREATE FUNCTION history."record_{table}"()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                IF TG_OP = 'DELETE' THEN
                    INSERT INTO history."{table}"
                    SELECT uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), (OLD).*;
                ELSE
                    INSERT INTO history."{table}"
                    SELECT uuidv7(), TG_OP, clock_timestamp(), history.operation_user_id(), (NEW).*;
                END IF;

                RETURN NULL;
            END;
            $$;
            """);
    }

    private static void CreateRecordTrigger(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql(
            $"""
            CREATE TRIGGER record_history AFTER INSERT OR UPDATE OR DELETE ON "{table}"
            FOR EACH ROW EXECUTE FUNCTION history."record_{table}"();
            """);
    }

    private static void CreateAppendOnlyTriggers(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql(
            $"""
            CREATE TRIGGER reject_write BEFORE UPDATE OR DELETE ON history."{table}"
            FOR EACH ROW EXECUTE FUNCTION history.reject_write();
            """);

        migrationBuilder.Sql(
            $"""
            CREATE TRIGGER reject_truncate BEFORE TRUNCATE ON history."{table}"
            FOR EACH STATEMENT EXECUTE FUNCTION history.reject_write();
            """);
    }
}
