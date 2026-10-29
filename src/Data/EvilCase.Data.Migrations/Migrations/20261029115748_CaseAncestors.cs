using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class CaseAncestors : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // CYCLE stops the walk at a case already on its path; the case asked for never counts as its own ancestor.
        migrationBuilder.Sql(
            """
            CREATE FUNCTION case_ancestors(case_id uuid)
            RETURNS TABLE ("CaseId" uuid, "Depth" integer)
            LANGUAGE sql
            STABLE
            AS $$
                WITH RECURSIVE ancestors ("CaseId", "Depth") AS (
                    SELECT c."ParentCaseId", 1
                    FROM "Cases" c
                    WHERE c."Id" = case_id AND c."ParentCaseId" IS NOT NULL
                    UNION ALL
                    SELECT c."ParentCaseId", a."Depth" + 1
                    FROM ancestors a
                    JOIN "Cases" c ON c."Id" = a."CaseId"
                    WHERE c."ParentCaseId" IS NOT NULL AND a."CaseId" <> case_id
                ) CYCLE "CaseId" SET "IsCycle" USING "Path"
                SELECT a."CaseId", a."Depth"
                FROM ancestors a
                WHERE NOT a."IsCycle" AND a."CaseId" <> case_id;
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP FUNCTION case_ancestors(uuid);");
    }
}
