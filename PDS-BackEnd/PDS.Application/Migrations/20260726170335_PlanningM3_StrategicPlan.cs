using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM3_StrategicPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StrategicPlans",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    PeriodStartYear = table.Column<int>(type: "int", nullable: false),
                    PeriodEndYear = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PreviousVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategicFocuses",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EndAlignment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicFocuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicFocuses_StrategicPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Planning",
                        principalTable: "StrategicPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicPlanChanges",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ChangeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Level = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicPlanChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicPlanChanges_StrategicPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Planning",
                        principalTable: "StrategicPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicObjectives",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    FocusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicObjectives_StrategicFocuses_FocusId",
                        column: x => x.FocusId,
                        principalSchema: "Planning",
                        principalTable: "StrategicFocuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicProducts",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DiscontinuedFromYear = table.Column<int>(type: "int", nullable: true),
                    DiscontinuationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicProducts_StrategicObjectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalSchema: "Planning",
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicMeasureUnits",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LineageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ResponsibleAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DiscontinuedFromYear = table.Column<int>(type: "int", nullable: true),
                    DiscontinuationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicMeasureUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicMeasureUnits_StrategicProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "StrategicProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicProductInvolvedAreas",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicProductInvolvedAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicProductInvolvedAreas_StrategicProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "StrategicProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicAnnualTargets",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    MeasureUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicAnnualTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicAnnualTargets_StrategicMeasureUnits_MeasureUnitId",
                        column: x => x.MeasureUnitId,
                        principalSchema: "Planning",
                        principalTable: "StrategicMeasureUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_StrategicAnnualTargets_UnitYear",
                schema: "Planning",
                table: "StrategicAnnualTargets",
                columns: new[] { "MeasureUnitId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicFocuses_Lineage",
                schema: "Planning",
                table: "StrategicFocuses",
                column: "LineageId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicFocuses_PlanOrder",
                schema: "Planning",
                table: "StrategicFocuses",
                columns: new[] { "PlanId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicMeasureUnits_Lineage",
                schema: "Planning",
                table: "StrategicMeasureUnits",
                column: "LineageId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicMeasureUnits_ProductOrder",
                schema: "Planning",
                table: "StrategicMeasureUnits",
                columns: new[] { "ProductId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_FocusOrder",
                schema: "Planning",
                table: "StrategicObjectives",
                columns: new[] { "FocusId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_Lineage",
                schema: "Planning",
                table: "StrategicObjectives",
                column: "LineageId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicPlanChanges_PlanDate",
                schema: "Planning",
                table: "StrategicPlanChanges",
                columns: new[] { "PlanId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_StrategicPlans_OrgVersion",
                schema: "Planning",
                table: "StrategicPlans",
                columns: new[] { "OrganizationId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "UX_StrategicPlans_OneCurrentPerOrg",
                schema: "Planning",
                table: "StrategicPlans",
                column: "OrganizationId",
                unique: true,
                filter: "[Status] = 'Current'");

            migrationBuilder.CreateIndex(
                name: "UX_StrategicProductInvolvedAreas_ProductArea",
                schema: "Planning",
                table: "StrategicProductInvolvedAreas",
                columns: new[] { "ProductId", "AreaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicProducts_Lineage",
                schema: "Planning",
                table: "StrategicProducts",
                column: "LineageId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicProducts_ObjectiveOrder",
                schema: "Planning",
                table: "StrategicProducts",
                columns: new[] { "ObjectiveId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrategicAnnualTargets",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicPlanChanges",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicProductInvolvedAreas",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicMeasureUnits",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicProducts",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicObjectives",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicFocuses",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "StrategicPlans",
                schema: "Planning");
        }
    }
}
