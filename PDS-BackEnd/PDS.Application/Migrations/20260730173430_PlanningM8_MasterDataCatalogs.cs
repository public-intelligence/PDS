using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM8_MasterDataCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Definition",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EnglishName",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludedInMaster",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyExpenseObjectCode",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyExpenseObjectName",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRole",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountingAccounts",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsPostable = table.Column<bool>(type: "bit", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtendedDescription = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourcePdfPage = table.Column<int>(type: "int", nullable: true),
                    SourcePrintedPage = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingAccounts_AccountingAccounts_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Planning",
                        principalTable: "AccountingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidatorAreaProfiles",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    AreaCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OrganicGroup = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NormalizedAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganizationalStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MandateSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConsolidatorKind = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ConsolidatorScope = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Limits = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ValidationState = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CatalogVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Author = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidatorAreaProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsolidatorAreaProfiles_OrganizationalStructures_OrganizationalStructureId",
                        column: x => x.OrganizationalStructureId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidatorRoutingRules",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    RuleKey = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RuleDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupersedesVersion = table.Column<int>(type: "int", nullable: true),
                    Reversible = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KeywordsNormalized = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExclusionKeywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExclusionKeywordsNormalized = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UnspscPrefixes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConsolidatorAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ConsolidatorAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    InstitutionallyValidated = table.Column<bool>(type: "bit", nullable: false),
                    BaseConfidence = table.Column<int>(type: "int", nullable: false),
                    ForceReview = table.Column<bool>(type: "bit", nullable: false),
                    RuleState = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidatorRoutingRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsolidatorRoutingRules_OrganizationalStructures_ConsolidatorAreaId",
                        column: x => x.ConsolidatorAreaId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseObjects",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDetail = table.Column<bool>(type: "bit", nullable: false),
                    SourcePdfPage = table.Column<int>(type: "int", nullable: true),
                    SourcePrintedPage = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseObjects_ExpenseObjects_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Planning",
                        principalTable: "ExpenseObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasterProducts",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OriginalDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnglishName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Definition = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SearchText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UnifiedCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnspscCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UnspscName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ItemSubtype = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AssetTreatment = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MeasureUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferencePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    PriceSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpenseObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExpenseObjectCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExpenseObjectName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AccountingAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AccountingAccountCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AccountingAccountName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ConsolidatorAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsolidatorAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ConsolidatorAreaSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ConsolidatorRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsolidatorRuleKey = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ConsolidatorRuleVersion = table.Column<int>(type: "int", nullable: true),
                    ConsolidatorAreaConfidence = table.Column<int>(type: "int", nullable: true),
                    ExpenseObjectConfidence = table.Column<int>(type: "int", nullable: true),
                    AccountingAccountConfidence = table.Column<int>(type: "int", nullable: true),
                    MatchMethod = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    AreaAmbiguity = table.Column<bool>(type: "bit", nullable: false),
                    AlternativeAreasJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ReviewReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DuplicateOfMasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RequestedAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestJustification = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterProducts_AccountingAccounts_AccountingAccountId",
                        column: x => x.AccountingAccountId,
                        principalSchema: "Planning",
                        principalTable: "AccountingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterProducts_ConsolidatorRoutingRules_ConsolidatorRuleId",
                        column: x => x.ConsolidatorRuleId,
                        principalSchema: "Planning",
                        principalTable: "ConsolidatorRoutingRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MasterProducts_ExpenseObjects_ExpenseObjectId",
                        column: x => x.ExpenseObjectId,
                        principalSchema: "Planning",
                        principalTable: "ExpenseObjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterProducts_MasterProducts_DuplicateOfMasterProductId",
                        column: x => x.DuplicateOfMasterProductId,
                        principalSchema: "Planning",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterProducts_OrganizationalStructures_ConsolidatorAreaId",
                        column: x => x.ConsolidatorAreaId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MasterProducts_UnifiedCodes_UnifiedCodeId",
                        column: x => x.UnifiedCodeId,
                        principalSchema: "Planning",
                        principalTable: "UnifiedCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MasterProductAliases",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    MasterProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    System = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NormalizedText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterProductAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterProductAliases_MasterProducts_MasterProductId",
                        column: x => x.MasterProductId,
                        principalSchema: "Planning",
                        principalTable: "MasterProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingAccounts_OrgParent",
                schema: "Planning",
                table: "AccountingAccounts",
                columns: new[] { "OrganizationId", "ParentId" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingAccounts_ParentId",
                schema: "Planning",
                table: "AccountingAccounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "UX_AccountingAccounts_CodePerOrg",
                schema: "Planning",
                table: "AccountingAccounts",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidatorAreaProfiles_OrganizationalStructureId",
                schema: "Planning",
                table: "ConsolidatorAreaProfiles",
                column: "OrganizationalStructureId");

            migrationBuilder.CreateIndex(
                name: "UX_ConsolidatorAreaProfiles_CodePerOrg",
                schema: "Planning",
                table: "ConsolidatorAreaProfiles",
                columns: new[] { "OrganizationId", "AreaCode" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidatorRoutingRules_ConsolidatorAreaId",
                schema: "Planning",
                table: "ConsolidatorRoutingRules",
                column: "ConsolidatorAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidatorRoutingRules_OrgActivePriority",
                schema: "Planning",
                table: "ConsolidatorRoutingRules",
                columns: new[] { "OrganizationId", "IsActive", "Priority" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ConsolidatorRoutingRules_KeyVersion",
                schema: "Planning",
                table: "ConsolidatorRoutingRules",
                columns: new[] { "OrganizationId", "RuleKey", "Version" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseObjects_OrgParent",
                schema: "Planning",
                table: "ExpenseObjects",
                columns: new[] { "OrganizationId", "ParentId" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseObjects_ParentId",
                schema: "Planning",
                table: "ExpenseObjects",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "UX_ExpenseObjects_CodePerOrg",
                schema: "Planning",
                table: "ExpenseObjects",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProductAliases_OrgNormalized",
                schema: "Planning",
                table: "MasterProductAliases",
                columns: new[] { "OrganizationId", "NormalizedText" });

            migrationBuilder.CreateIndex(
                name: "UX_MasterProductAliases_ProductKindText",
                schema: "Planning",
                table: "MasterProductAliases",
                columns: new[] { "MasterProductId", "Kind", "Text" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_AccountingAccountId",
                schema: "Planning",
                table: "MasterProducts",
                column: "AccountingAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_ConsolidatorAreaId",
                schema: "Planning",
                table: "MasterProducts",
                column: "ConsolidatorAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_ConsolidatorRuleId",
                schema: "Planning",
                table: "MasterProducts",
                column: "ConsolidatorRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_DeletedAt",
                schema: "Planning",
                table: "MasterProducts",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_DuplicateOfMasterProductId",
                schema: "Planning",
                table: "MasterProducts",
                column: "DuplicateOfMasterProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_ExpenseObjectId",
                schema: "Planning",
                table: "MasterProducts",
                column: "ExpenseObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_OrgConsolidator",
                schema: "Planning",
                table: "MasterProducts",
                columns: new[] { "OrganizationId", "ConsolidatorAreaId" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_OrgState",
                schema: "Planning",
                table: "MasterProducts",
                columns: new[] { "OrganizationId", "State" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_OrgUnspsc",
                schema: "Planning",
                table: "MasterProducts",
                columns: new[] { "OrganizationId", "UnspscCode" },
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MasterProducts_UnifiedCodeId",
                schema: "Planning",
                table: "MasterProducts",
                column: "UnifiedCodeId");

            migrationBuilder.CreateIndex(
                name: "UX_MasterProducts_CodePerOrg",
                schema: "Planning",
                table: "MasterProducts",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_MasterProducts_SourceIdPerOrg",
                schema: "Planning",
                table: "MasterProducts",
                columns: new[] { "OrganizationId", "SourceId" },
                unique: true,
                filter: "[DeletedAt] IS NULL AND [SourceId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsolidatorAreaProfiles",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "MasterProductAliases",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "MasterProducts",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "AccountingAccounts",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "ConsolidatorRoutingRules",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "ExpenseObjects",
                schema: "Planning");

            migrationBuilder.DropColumn(
                name: "Definition",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "EnglishName",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "IncludedInMaster",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "LegacyExpenseObjectCode",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "LegacyExpenseObjectName",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "Source",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "SourceRole",
                schema: "Planning",
                table: "UnifiedCodes");
        }
    }
}
