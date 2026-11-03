using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM2_TemplatesAndCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanningTemplates",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportCategories",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnifiedCodes",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsolidatorAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsolidatorAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    HasCost = table.Column<bool>(type: "bit", nullable: true),
                    Executor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    GeneratesPurchase = table.Column<bool>(type: "bit", nullable: true),
                    Mergeable = table.Column<bool>(type: "bit", nullable: true),
                    SuggestedUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReferencePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    AnticipationDays = table.Column<int>(type: "int", nullable: true),
                    InstitutionallyEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnifiedCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnifiedCodes_OrganizationalStructures_ConsolidatorAreaId",
                        column: x => x.ConsolidatorAreaId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnifiedCodes_UnifiedCodes_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Planning",
                        principalTable: "UnifiedCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanningTemplateColumns",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisibleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TechnicalName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    Visible = table.Column<bool>(type: "bit", nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningTemplateColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningTemplateColumns_PlanningTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "Planning",
                        principalTable: "PlanningTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupportCategoryCodes",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    SupportCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnifiedCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCategoryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportCategoryCodes_SupportCategories_SupportCategoryId",
                        column: x => x.SupportCategoryId,
                        principalSchema: "Planning",
                        principalTable: "SupportCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupportCategoryCodes_UnifiedCodes_UnifiedCodeId",
                        column: x => x.UnifiedCodeId,
                        principalSchema: "Planning",
                        principalTable: "UnifiedCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnifiedCodeAliases",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    UnifiedCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    System = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnifiedCodeAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnifiedCodeAliases_UnifiedCodes_UnifiedCodeId",
                        column: x => x.UnifiedCodeId,
                        principalSchema: "Planning",
                        principalTable: "UnifiedCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTemplateColumns_DeletedAt",
                schema: "Planning",
                table: "PlanningTemplateColumns",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PlanningTemplateColumns_TechNamePerTemplate",
                schema: "Planning",
                table: "PlanningTemplateColumns",
                columns: new[] { "TemplateId", "TechnicalName" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningTemplates_DeletedAt",
                schema: "Planning",
                table: "PlanningTemplates",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PlanningTemplates_NamePerOrg",
                schema: "Planning",
                table: "PlanningTemplates",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCategories_DeletedAt",
                schema: "Planning",
                table: "SupportCategories",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_SupportCategories_NamePerOrg",
                schema: "Planning",
                table: "SupportCategories",
                columns: new[] { "OrganizationId", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportCategoryCodes_UnifiedCodeId",
                schema: "Planning",
                table: "SupportCategoryCodes",
                column: "UnifiedCodeId");

            migrationBuilder.CreateIndex(
                name: "UX_SupportCategoryCodes_CategoryCode",
                schema: "Planning",
                table: "SupportCategoryCodes",
                columns: new[] { "SupportCategoryId", "UnifiedCodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_UnifiedCodeAliases_SystemCode",
                schema: "Planning",
                table: "UnifiedCodeAliases",
                columns: new[] { "UnifiedCodeId", "System", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes",
                column: "ConsolidatorAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_DeletedAt",
                schema: "Planning",
                table: "UnifiedCodes",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_OrgConsolidator",
                schema: "Planning",
                table: "UnifiedCodes",
                columns: new[] { "OrganizationId", "ConsolidatorAreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_OrgParent",
                schema: "Planning",
                table: "UnifiedCodes",
                columns: new[] { "OrganizationId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_ParentId",
                schema: "Planning",
                table: "UnifiedCodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "UX_UnifiedCodes_CodePerOrg",
                schema: "Planning",
                table: "UnifiedCodes",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanningTemplateColumns",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "SupportCategoryCodes",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "UnifiedCodeAliases",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PlanningTemplates",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "SupportCategories",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "UnifiedCodes",
                schema: "Planning");
        }
    }
}
