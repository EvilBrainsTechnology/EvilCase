using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseAncestors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The walk stays in the tenant of the case asked for, and CYCLE stops it at a case already
        // on its path; the case asked for never counts as its own ancestor.
        migrationBuilder.Sql(
            """
            CREATE FUNCTION case_ancestors(case_id uuid)
            RETURNS TABLE ("CaseId" uuid, "Title" character varying(256), "Depth" integer)
            LANGUAGE sql
            STABLE
            AS $$
                WITH RECURSIVE ancestors ("CaseId", "TenantId", "Depth") AS (
                    SELECT c."ParentCaseId", c."TenantId", 1
                    FROM "Cases" c
                    WHERE c."Id" = case_id AND c."ParentCaseId" IS NOT NULL
                    UNION ALL
                    SELECT c."ParentCaseId", a."TenantId", a."Depth" + 1
                    FROM ancestors a
                    JOIN "Cases" c ON c."Id" = a."CaseId" AND c."TenantId" = a."TenantId"
                    WHERE c."ParentCaseId" IS NOT NULL AND a."CaseId" <> case_id
                ) CYCLE "CaseId" SET "IsCycle" USING "Path"
                SELECT a."CaseId", c."Title", a."Depth"
                FROM ancestors a
                JOIN "Cases" c ON c."Id" = a."CaseId" AND c."TenantId" = a."TenantId"
                WHERE NOT a."IsCycle" AND a."CaseId" <> case_id;
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION case_ancestors(uuid);");
    }
}
