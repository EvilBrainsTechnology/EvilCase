using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

/// <summary>
/// The full-text indexes over the titles and descriptions of cases and acts: no query reads them, the search matches with
/// ILIKE.
/// </summary>
public partial class DropUnusedSearchIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP INDEX "IX_Cases_Title_Search";
            DROP INDEX "IX_Cases_Description_Search";
            DROP INDEX "IX_Acts_Title_Search";
            DROP INDEX "IX_Acts_Description_Search";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""CREATE INDEX "IX_Cases_Title_Search" ON "Cases" USING GIN (to_tsvector('simple', immutable_unaccent("Title")));""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Cases_Description_Search" ON "Cases" USING GIN (to_tsvector('simple', immutable_unaccent(COALESCE("Description", ''))));""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Acts_Title_Search" ON "Acts" USING GIN (to_tsvector('simple', immutable_unaccent("Title")));""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Acts_Description_Search" ON "Acts" USING GIN (to_tsvector('simple', immutable_unaccent(COALESCE("Description", ''))));""");
    }
}
