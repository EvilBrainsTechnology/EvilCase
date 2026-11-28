using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseTree : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The walk stays in the tenant of the case asked for; UNION drops a case already reached, so a cycle ends it.
        migrationBuilder.Sql(
            """
            CREATE FUNCTION case_tree(case_id uuid)
            RETURNS SETOF "Cases"
            LANGUAGE sql
            STABLE
            AS $$
                WITH RECURSIVE tree ("Id", "TenantId") AS (
                    SELECT c."Id", c."TenantId"
                    FROM "Cases" c
                    WHERE c."Id" = case_id
                    UNION
                    SELECT c."Id", c."TenantId"
                    FROM tree t
                    JOIN "Cases" c ON c."ParentCaseId" = t."Id" AND c."TenantId" = t."TenantId"
                )
                SELECT c.*
                FROM tree t
                JOIN "Cases" c ON c."Id" = t."Id";
            $$;
            """);

        // The cases of the list's page that hold a file in their tree, walked once for all of them.
        migrationBuilder.Sql(
            """
            CREATE FUNCTION cases_with_files(case_ids uuid[])
            RETURNS TABLE (case_id uuid)
            LANGUAGE sql
            STABLE
            AS $$
                WITH RECURSIVE tree (root_id, "Id", "TenantId") AS (
                    SELECT c."Id", c."Id", c."TenantId"
                    FROM "Cases" c
                    WHERE c."Id" = ANY (case_ids)
                    UNION
                    SELECT t.root_id, c."Id", c."TenantId"
                    FROM tree t
                    JOIN "Cases" c ON c."ParentCaseId" = t."Id" AND c."TenantId" = t."TenantId"
                )
                SELECT DISTINCT t.root_id
                FROM tree t
                WHERE EXISTS (
                    SELECT 1
                    FROM "Acts" a
                    JOIN "FileAssets" f ON f."ActId" = a."Id"
                    WHERE a."CaseId" = t."Id");
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION cases_with_files(uuid[]);");
        migrationBuilder.Sql("DROP FUNCTION case_tree(uuid);");
    }
}
