using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class Labels : Migration
{
    private static readonly string[] AddedTables = ["Labels", "LabelAssignments"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateLabels(migrationBuilder);
        CreateLabelAssignments(migrationBuilder);
        CreateIndexes(migrationBuilder);

        foreach (var table in AddedTables)
        {
            StampTimestamps(migrationBuilder, table);
            RecordHistory(migrationBuilder, table);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in AddedTables)
        {
            migrationBuilder.Sql($"""DROP TRIGGER record_history ON "{table}";""");
            migrationBuilder.Sql($"""DROP FUNCTION history."record_{table}"();""");
            migrationBuilder.Sql($"""DROP TABLE history."{table}";""");
        }

        migrationBuilder.DropTable(name: "LabelAssignments");
        migrationBuilder.DropTable(name: "Labels");
    }

    private static void CreateLabels(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Labels",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Color = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_Labels", static x => x.Id);
                table.ForeignKey(
                    name: "FK_Labels_Tenants_TenantId",
                    column: static x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateLabelAssignments(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LabelAssignments",
            columns: static table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                LabelId = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                ActId = table.Column<Guid>(type: "uuid", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: static table =>
            {
                table.PrimaryKey("PK_LabelAssignments", static x => x.Id);
                table.CheckConstraint("CK_LabelAssignments_OnACaseOrAnAct", "(\"CaseId\" IS NULL) <> (\"ActId\" IS NULL)");
                table.ForeignKey(
                    name: "FK_LabelAssignments_Acts_ActId",
                    column: static x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LabelAssignments_Cases_CaseId",
                    column: static x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LabelAssignments_Labels_LabelId",
                    column: static x => x.LabelId,
                    principalTable: "Labels",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LabelAssignments_Tenants_TenantId",
                    column: static x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_LabelAssignments_ActId",
            table: "LabelAssignments",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelAssignments_CaseId",
            table: "LabelAssignments",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelAssignments_LabelId",
            table: "LabelAssignments",
            column: "LabelId");

        migrationBuilder.CreateIndex(
            name: "IX_LabelAssignments_TenantId_LabelId_ActId",
            table: "LabelAssignments",
            columns: ["TenantId", "LabelId", "ActId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LabelAssignments_TenantId_LabelId_CaseId",
            table: "LabelAssignments",
            columns: ["TenantId", "LabelId", "CaseId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Labels_TenantId_Name",
            table: "Labels",
            columns: ["TenantId", "Name"],
            unique: true);
    }

    private static void StampTimestamps(MigrationBuilder migrationBuilder, string table)
    {
        migrationBuilder.Sql(
            $"""CREATE TRIGGER stamp_timestamps BEFORE INSERT OR UPDATE ON "{table}" FOR EACH ROW EXECUTE FUNCTION stamp_timestamps();""");
    }

    private static void RecordHistory(MigrationBuilder migrationBuilder, string table)
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

        migrationBuilder.Sql(
            $"""
            CREATE TRIGGER record_history AFTER INSERT OR UPDATE OR DELETE ON "{table}"
            FOR EACH ROW EXECUTE FUNCTION history."record_{table}"();
            """);

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
