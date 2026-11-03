using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM11_RetireSupportCategoriesAndUnifiedCodeAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnifiedCodes_OrganizationalStructures_ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropTable(
                name: "SupportCategoryCodes",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "SupportCategories",
                schema: "Planning");

            migrationBuilder.DropIndex(
                name: "IX_UnifiedCodes_ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropIndex(
                name: "IX_UnifiedCodes_OrgConsolidator",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "AnticipationDays",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "ConsolidatorAreaName",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "Executor",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "GeneratesPurchase",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "HasCost",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "Mergeable",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "ReferencePrice",
                schema: "Planning",
                table: "UnifiedCodes");

            migrationBuilder.DropColumn(
                name: "SuggestedUnit",
                schema: "Planning",
                table: "UnifiedCodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnticipationDays",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsolidatorAreaName",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Executor",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GeneratesPurchase",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasCost",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Mergeable",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferencePrice",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedUnit",
                schema: "Planning",
                table: "UnifiedCodes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SupportCategories",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCategories", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes",
                column: "ConsolidatorAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_UnifiedCodes_OrgConsolidator",
                schema: "Planning",
                table: "UnifiedCodes",
                columns: new[] { "OrganizationId", "ConsolidatorAreaId" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_UnifiedCodes_OrganizationalStructures_ConsolidatorAreaId",
                schema: "Planning",
                table: "UnifiedCodes",
                column: "ConsolidatorAreaId",
                principalSchema: "Organization",
                principalTable: "OrganizationalStructures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
