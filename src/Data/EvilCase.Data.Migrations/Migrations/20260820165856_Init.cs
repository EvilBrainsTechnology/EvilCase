using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvilBrains.EvilCase.Data.Migrations.Migrations;

public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
            .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

        CreateAccounts(migrationBuilder);
        CreateTenants(migrationBuilder);
        CreateActs(migrationBuilder);
        CreateCases(migrationBuilder);
        CreateComments(migrationBuilder);
        CreateContacts(migrationBuilder);
        CreateUsers(migrationBuilder);
        CreateExternalActNumbers(migrationBuilder);
        CreateExternalCaseNumbers(migrationBuilder);
        CreateFileAssets(migrationBuilder);
        CreateRefreshTokens(migrationBuilder);

        CreateActIndexes(migrationBuilder);
        CreateCaseIndexes(migrationBuilder);
        CreateCommentIndexes(migrationBuilder);
        CreateContactIndexes(migrationBuilder);
        CreateExternalActNumberIndexes(migrationBuilder);
        CreateExternalCaseNumberIndexes(migrationBuilder);
        CreateFileAssetIndexes(migrationBuilder);
        CreateRefreshTokenIndexes(migrationBuilder);
        CreateTenantIndexes(migrationBuilder);
        CreateUserIndexes(migrationBuilder);

        AddDeferredForeignKeys(migrationBuilder);

        CreateSearchIndexes(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Users_Contacts_DefaultContactId",
            table: "Users");

        migrationBuilder.DropTable(
            name: "Comments");

        migrationBuilder.DropTable(
            name: "ExternalActNumbers");

        migrationBuilder.DropTable(
            name: "ExternalCaseNumbers");

        migrationBuilder.DropTable(
            name: "FileAssets");

        migrationBuilder.DropTable(
            name: "RefreshTokens");

        migrationBuilder.DropTable(
            name: "Acts");

        migrationBuilder.DropTable(
            name: "Cases");

        migrationBuilder.DropTable(
            name: "Contacts");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropTable(
            name: "Tenants");

        migrationBuilder.DropTable(
            name: "Accounts");

        migrationBuilder.Sql("DROP FUNCTION IF EXISTS immutable_unaccent(text);");
    }

    private static void CreateAccounts(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Accounts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Accounts", x => x.Id);
            });
    }

    private static void CreateTenants(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
                table.ForeignKey(
                    name: "FK_Tenants_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    private static void CreateActs(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Acts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                ActNumber = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Direction = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                SenderContactId = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientContactId = table.Column<Guid>(type: "uuid", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Acts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Acts_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateCases(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Cases",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ParentCaseId = table.Column<Guid>(type: "uuid", nullable: true),
                CaseNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Cases", x => x.Id);
                table.ForeignKey(
                    name: "FK_Cases_Cases_ParentCaseId",
                    column: x => x.ParentCaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_Cases_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateComments(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                ActId = table.Column<Guid>(type: "uuid", nullable: true),
                Body = table.Column<string>(type: "text", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.Id);
                table.CheckConstraint("CK_Comments_OnACaseOrAnAct", "(\"CaseId\" IS NULL) <> (\"ActId\" IS NULL)");
                table.ForeignKey(
                    name: "FK_Comments_Acts_ActId",
                    column: x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Comments_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Comments_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateContacts(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Contacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Address = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                DataBoxId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Contacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Contacts_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateUsers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                DefaultContactId = table.Column<Guid>(type: "uuid", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey(
                    name: "FK_Users_Contacts_DefaultContactId",
                    column: x => x.DefaultContactId,
                    principalTable: "Contacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Users_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateExternalActNumbers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExternalActNumbers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                ActId = table.Column<Guid>(type: "uuid", nullable: false),
                Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AssignedByContactId = table.Column<Guid>(type: "uuid", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExternalActNumbers", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Acts_ActId",
                    column: x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Contacts_AssignedByContactId",
                    column: x => x.AssignedByContactId,
                    principalTable: "Contacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalActNumbers_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateExternalCaseNumbers(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ExternalCaseNumbers",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                Value = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                AssignedByContactId = table.Column<Guid>(type: "uuid", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExternalCaseNumbers", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Contacts_AssignedByContactId",
                    column: x => x.AssignedByContactId,
                    principalTable: "Contacts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ExternalCaseNumbers_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateFileAssets(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FileAssets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                ActId = table.Column<Guid>(type: "uuid", nullable: true),
                FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                MediaType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FileAssets", x => x.Id);
                table.CheckConstraint("CK_FileAssets_OnACaseOrAnAct", "(\"CaseId\" IS NULL) <> (\"ActId\" IS NULL)");
                table.ForeignKey(
                    name: "FK_FileAssets_Acts_ActId",
                    column: x => x.ActId,
                    principalTable: "Acts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FileAssets_Cases_CaseId",
                    column: x => x.CaseId,
                    principalTable: "Cases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_FileAssets_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_FileAssets_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
    }

    private static void CreateRefreshTokens(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                SessionExpires = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                UserAgent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    private static void CreateActIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Acts_CaseId_Date",
            table: "Acts",
            columns: ["CaseId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_Acts_RecipientContactId",
            table: "Acts",
            column: "RecipientContactId");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_SenderContactId",
            table: "Acts",
            column: "SenderContactId");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_TenantId",
            table: "Acts",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Acts_TenantId_ActNumber",
            table: "Acts",
            columns: ["TenantId", "ActNumber"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Acts_UserId",
            table: "Acts",
            column: "UserId");
    }

    private static void CreateCaseIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Cases_ParentCaseId",
            table: "Cases",
            column: "ParentCaseId");

        migrationBuilder.CreateIndex(
            name: "IX_Cases_TenantId",
            table: "Cases",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Cases_TenantId_CaseNumber",
            table: "Cases",
            columns: ["TenantId", "CaseNumber"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Cases_TenantId_Date",
            table: "Cases",
            columns: ["TenantId", "Date"]);

        migrationBuilder.CreateIndex(
            name: "IX_Cases_UserId",
            table: "Cases",
            column: "UserId");
    }

    private static void CreateCommentIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Comments_ActId",
            table: "Comments",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_CaseId",
            table: "Comments",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_TenantId",
            table: "Comments",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Comments_UserId",
            table: "Comments",
            column: "UserId");
    }

    private static void CreateContactIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Contacts_TenantId",
            table: "Contacts",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Contacts_TenantId_DataBoxId",
            table: "Contacts",
            columns: ["TenantId", "DataBoxId"]);

        migrationBuilder.CreateIndex(
            name: "IX_Contacts_UserId",
            table: "Contacts",
            column: "UserId");
    }

    private static void CreateExternalActNumberIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_ActId",
            table: "ExternalActNumbers",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_AssignedByContactId",
            table: "ExternalActNumbers",
            column: "AssignedByContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_TenantId",
            table: "ExternalActNumbers",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_TenantId_ActId_Value",
            table: "ExternalActNumbers",
            columns: ["TenantId", "ActId", "Value"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExternalActNumbers_UserId",
            table: "ExternalActNumbers",
            column: "UserId");
    }

    private static void CreateExternalCaseNumberIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_AssignedByContactId",
            table: "ExternalCaseNumbers",
            column: "AssignedByContactId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_CaseId",
            table: "ExternalCaseNumbers",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_TenantId",
            table: "ExternalCaseNumbers",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_TenantId_CaseId_Value",
            table: "ExternalCaseNumbers",
            columns: ["TenantId", "CaseId", "Value"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExternalCaseNumbers_UserId",
            table: "ExternalCaseNumbers",
            column: "UserId");
    }

    private static void CreateFileAssetIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_ActId",
            table: "FileAssets",
            column: "ActId");

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_CaseId",
            table: "FileAssets",
            column: "CaseId");

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_TenantId",
            table: "FileAssets",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_FileAssets_UserId",
            table: "FileAssets",
            column: "UserId");
    }

    private static void CreateRefreshTokenIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_AuthSessionId",
            table: "RefreshTokens",
            column: "AuthSessionId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_TokenHash",
            table: "RefreshTokens",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");
    }

    private static void CreateTenantIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Tenants_AccountId",
            table: "Tenants",
            column: "AccountId");
    }

    private static void CreateUserIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Users_DefaultContactId",
            table: "Users",
            column: "DefaultContactId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_TenantId",
            table: "Users",
            column: "TenantId");
    }

    private static void AddDeferredForeignKeys(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Cases_CaseId",
            table: "Acts",
            column: "CaseId",
            principalTable: "Cases",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Contacts_RecipientContactId",
            table: "Acts",
            column: "RecipientContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Contacts_SenderContactId",
            table: "Acts",
            column: "SenderContactId",
            principalTable: "Contacts",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Acts_Users_UserId",
            table: "Acts",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Cases_Users_UserId",
            table: "Cases",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Comments_Users_UserId",
            table: "Comments",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.AddForeignKey(
            name: "FK_Contacts_Users_UserId",
            table: "Contacts",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    private static void CreateSearchIndexes(MigrationBuilder migrationBuilder)
    {
        // unaccent is not IMMUTABLE, so an expression index cannot call it; this wrapper pins the
        // dictionary and is what every index below and every search query call.
        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION immutable_unaccent(text)
            RETURNS text
            LANGUAGE sql
            IMMUTABLE
            STRICT
            PARALLEL SAFE
            AS $$ SELECT public.unaccent('public.unaccent'::regdictionary, $1) $$;
            """);

        migrationBuilder.Sql("""CREATE INDEX "IX_Cases_Title_Search" ON "Cases" USING GIN (to_tsvector('simple', immutable_unaccent("Title")));""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Cases_Description_Search" ON "Cases" USING GIN (to_tsvector('simple', immutable_unaccent(COALESCE("Description", ''))));""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Acts_Title_Search" ON "Acts" USING GIN (to_tsvector('simple', immutable_unaccent("Title")));""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Acts_Description_Search" ON "Acts" USING GIN (to_tsvector('simple', immutable_unaccent(COALESCE("Description", ''))));""");

        migrationBuilder.Sql("""CREATE INDEX "IX_Cases_CaseNumber_Trigram" ON "Cases" USING GIN ("CaseNumber" gin_trgm_ops);""");
        migrationBuilder.Sql("""CREATE INDEX "IX_Acts_ActNumber_Trigram" ON "Acts" USING GIN ("ActNumber" gin_trgm_ops);""");
        migrationBuilder.Sql("""CREATE INDEX "IX_ExternalCaseNumbers_Value_Trigram" ON "ExternalCaseNumbers" USING GIN ("Value" gin_trgm_ops);""");
        migrationBuilder.Sql("""CREATE INDEX "IX_ExternalActNumbers_Value_Trigram" ON "ExternalActNumbers" USING GIN ("Value" gin_trgm_ops);""");
    }
}
