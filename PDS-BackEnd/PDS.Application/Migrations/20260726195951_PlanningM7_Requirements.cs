using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM7_Requirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Requirements",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoaProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoaTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ConsolidatorAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsolidatorAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AppliedRule = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupportCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UnifiedCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnifiedCodeName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedUnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PriceConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    Quarter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequiredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FundingSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MergedIntoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Requirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Requirements_AnnualPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Planning",
                        principalTable: "AnnualPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "Planning",
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_PoaProducts_PoaProductId",
                        column: x => x.PoaProductId,
                        principalSchema: "Planning",
                        principalTable: "PoaProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_PoaTasks_PoaTaskId",
                        column: x => x.PoaTaskId,
                        principalSchema: "Planning",
                        principalTable: "PoaTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_Requirements_MergedIntoId",
                        column: x => x.MergedIntoId,
                        principalSchema: "Planning",
                        principalTable: "Requirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Requirements_SupportCategories_SupportCategoryId",
                        column: x => x.SupportCategoryId,
                        principalSchema: "Planning",
                        principalTable: "SupportCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Requirements_UnifiedCodes_UnifiedCodeId",
                        column: x => x.UnifiedCodeId,
                        principalSchema: "Planning",
                        principalTable: "UnifiedCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrioritizationDecisions",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    RequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequirementCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UnifiedCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DecidedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    DecidingAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrioritizationDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrioritizationDecisions_Requirements_RequirementId",
                        column: x => x.RequirementId,
                        principalSchema: "Planning",
                        principalTable: "Requirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequirementComments",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    RequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementComments_Requirements_RequirementId",
                        column: x => x.RequirementId,
                        principalSchema: "Planning",
                        principalTable: "Requirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrioritizationDecisions_RequirementId",
                schema: "Planning",
                table: "PrioritizationDecisions",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementComments_RequirementId",
                schema: "Planning",
                table: "RequirementComments",
                column: "RequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_ConsolidatorStatus",
                schema: "Planning",
                table: "Requirements",
                columns: new[] { "ConsolidatorAreaId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_CycleId",
                schema: "Planning",
                table: "Requirements",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_MergedIntoId",
                schema: "Planning",
                table: "Requirements",
                column: "MergedIntoId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_OrgCycleStatus",
                schema: "Planning",
                table: "Requirements",
                columns: new[] { "OrganizationId", "CycleId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_PlanId",
                schema: "Planning",
                table: "Requirements",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_PoaProductId",
                schema: "Planning",
                table: "Requirements",
                column: "PoaProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_PoaTaskId",
                schema: "Planning",
                table: "Requirements",
                column: "PoaTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_SupportCategoryId",
                schema: "Planning",
                table: "Requirements",
                column: "SupportCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_UnifiedCodeId",
                schema: "Planning",
                table: "Requirements",
                column: "UnifiedCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrioritizationDecisions",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "RequirementComments",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "Requirements",
                schema: "Planning");
        }
    }
}
