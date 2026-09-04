using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class ActAndCaseContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddContactColumns(migrationBuilder);
        BackfillActContacts(migrationBuilder);
        DropIssuedAndAddressedContacts(migrationBuilder);
        AddContactConstraints(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        DropDirectionPairing(migrationBuilder);
        AddIssuedAndAddressedColumns(migrationBuilder);
        RestoreIssuedAndAddressedContacts(migrationBuilder);
        DropContactColumns(migrationBuilder);
        RequireDirection(migrationBuilder);
        AddIssuedAndAddressedConstraints(migrationBuilder);
    }

    private static void DropIssuedAndAddressedContacts(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Acts_Contacts_AddressedToContactId",
            table: "Acts");

        migrationBuilder.DropForeignKey(
            name: "FK_Acts_Contacts_IssuedByContactId",
            table: "Acts");

        migrationBuilder.DropForeignKey(
            name: "FK_Users_Contacts_DefaultContactId",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Acts_AddressedToContactId",
            table: "Acts");

        migrationBuilder.DropIndex(
            name: "IX_Acts_IssuedByContactId",
            table: "Acts");

        migrationBuilder.DropIndex(
            name: "IX_Users_DefaultContactId",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "AddressedToContactId",
            table: "Acts");

        migrationBuilder.DropColumn(
            name: "IssuedByContactId",
            table: "Acts");

        migrationBuilder.DropColumn(
            name: "DefaultContactId",
            table: "Users");
    }

    private static void AddContactColumns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Direction",
            table: "Acts",
            type: "character varying(8)",
            maxLength: 8,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(8)",
            oldMaxLength: 8);

        migrationBuilder.AddColumn<Guid>(
            name: "ContactId",
            table: "Acts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ContactId",
            table: "Cases",
            type: "uuid",
            nullable: true);
    }

    private static void AddContactConstraints(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddCheckConstraint(
            name: "CK_Acts_DirectionWithContact",
            table: "Acts",
            sql: "(\"Direction\" IS NULL) = (\"ContactId\" IS NULL)");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_ContactId",
            table: "Acts",
            column: "ContactId");

        migrationBuilder.CreateIndex(
            name: "IX_Cases_ContactId",
            table: "Cases",
            column: "ContactId");

        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Contacts_ContactId",
            table: "Acts",
            column: "ContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Contacts_ContactId",
            table: "Cases",
            column: "ContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    private static void DropDirectionPairing(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Acts_DirectionWithContact",
            table: "Acts");
    }

    private static void DropContactColumns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Acts_Contacts_ContactId",
            table: "Acts");

        migrationBuilder.DropForeignKey(
            name: "FK_Cases_Contacts_ContactId",
            table: "Cases");

        migrationBuilder.DropIndex(
            name: "IX_Acts_ContactId",
            table: "Acts");

        migrationBuilder.DropIndex(
            name: "IX_Cases_ContactId",
            table: "Cases");

        migrationBuilder.DropColumn(
            name: "ContactId",
            table: "Acts");

        migrationBuilder.DropColumn(
            name: "ContactId",
            table: "Cases");
    }

    private static void AddIssuedAndAddressedColumns(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "AddressedToContactId",
            table: "Acts",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "IssuedByContactId",
            table: "Acts",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);

        migrationBuilder.AddColumn<Guid>(
            name: "DefaultContactId",
            table: "Users",
            type: "uuid",
            nullable: false,
            defaultValue: Guid.Empty);
    }

    private static void RequireDirection(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Direction",
            table: "Acts",
            type: "character varying(8)",
            maxLength: 8,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(8)",
            oldMaxLength: 8,
            oldNullable: true);
    }

    private static void AddIssuedAndAddressedConstraints(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Acts_AddressedToContactId",
            table: "Acts",
            column: "AddressedToContactId");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_IssuedByContactId",
            table: "Acts",
            column: "IssuedByContactId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_DefaultContactId",
            table: "Users",
            column: "DefaultContactId");

        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Contacts_AddressedToContactId",
            table: "Acts",
            column: "AddressedToContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Contacts_IssuedByContactId",
            table: "Acts",
            column: "IssuedByContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Users_Contacts_DefaultContactId",
            table: "Users",
            column: "DefaultContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    // The direction is nulled with the contact so the check constraint holds.
    // The stamp trigger is off: a migration is not a change the user made.
    private static void BackfillActContacts(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Acts" DISABLE TRIGGER stamp_timestamps;

            UPDATE "Acts"
            SET "ContactId" = CASE WHEN "Direction" = 'Incoming' THEN "IssuedByContactId" ELSE "AddressedToContactId" END;

            UPDATE "Acts" SET "Direction" = NULL WHERE "ContactId" IS NULL;

            ALTER TABLE "Acts" ENABLE TRIGGER stamp_timestamps;
            """);
    }

    // IssuedBy and DefaultContact are NOT NULL: the oldest contact stands in.
    private static void RestoreIssuedAndAddressedContacts(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "Acts" DISABLE TRIGGER stamp_timestamps;
            ALTER TABLE "Users" DISABLE TRIGGER stamp_timestamps;

            UPDATE "Acts" AS a
            SET "IssuedByContactId" = COALESCE(
                    CASE WHEN a."Direction" = 'Outgoing' THEN NULL ELSE a."ContactId" END,
                    (SELECT c."Id" FROM "Contacts" AS c WHERE c."TenantId" = a."TenantId" ORDER BY c."Created", c."Id" LIMIT 1)),
                "AddressedToContactId" = CASE WHEN a."Direction" = 'Outgoing' THEN a."ContactId" END,
                "Direction" = COALESCE(a."Direction", 'Incoming');

            UPDATE "Users" AS u
            SET "DefaultContactId" = (
                    SELECT c."Id" FROM "Contacts" AS c WHERE c."TenantId" = u."TenantId" ORDER BY c."Created", c."Id" LIMIT 1);

            ALTER TABLE "Acts" ENABLE TRIGGER stamp_timestamps;
            ALTER TABLE "Users" ENABLE TRIGGER stamp_timestamps;
            """);
    }
}
