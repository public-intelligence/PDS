using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM4_Cycles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanningCycles",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    EndYear = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StrategicPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningCycles_PlanningTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "Planning",
                        principalTable: "PlanningTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanningCycles_StrategicPlans_StrategicPlanId",
                        column: x => x.StrategicPlanId,
                        principalSchema: "Planning",
                        principalTable: "StrategicPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanningSequences",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NextValue = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnnualPlans",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GeneralObservation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnnualPlans_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "Planning",
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanningCycleAreas",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningCycleAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningCycleAreas_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "Planning",
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanningCycleMilestones",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanningCycleMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanningCycleMilestones_PlanningCycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "Planning",
                        principalTable: "PlanningCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoaProducts",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Origin = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StrategicFocusId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StrategicObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StrategicProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OperationalJustification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoaProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoaProducts_AnnualPlans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "Planning",
                        principalTable: "AnnualPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoaMeasureUnits",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerificationMeans = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResponsibleAreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibleAreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Q1 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Q2 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Q3 = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Q4 = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoaMeasureUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoaMeasureUnits_PoaProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "PoaProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoaProductCustomValues",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoaProductCustomValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoaProductCustomValues_PlanningTemplateColumns_TemplateColumnId",
                        column: x => x.TemplateColumnId,
                        principalSchema: "Planning",
                        principalTable: "PlanningTemplateColumns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PoaProductCustomValues_PoaProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "PoaProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoaProductInvolvedAreas",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoaProductInvolvedAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoaProductInvolvedAreas_PoaProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "PoaProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoaProductMunicipalities",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MunicipioName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoaProductMunicipalities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoaProductMunicipalities_PoaProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "PoaProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoaTasks",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsContinuous = table.Column<bool>(type: "bit", nullable: false),
                    MonthsMask = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoaTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoaTasks_PoaProducts_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Planning",
                        principalTable: "PoaProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_AnnualPlans_CycleArea",
                schema: "Planning",
                table: "AnnualPlans",
                columns: new[] { "CycleId", "AreaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PlanningCycleAreas_CycleArea",
                schema: "Planning",
                table: "PlanningCycleAreas",
                columns: new[] { "CycleId", "AreaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCycleMilestones_CycleId",
                schema: "Planning",
                table: "PlanningCycleMilestones",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCycles_DeletedAt",
                schema: "Planning",
                table: "PlanningCycles",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCycles_StrategicPlanId",
                schema: "Planning",
                table: "PlanningCycles",
                column: "StrategicPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanningCycles_TemplateId",
                schema: "Planning",
                table: "PlanningCycles",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "UX_PlanningCycles_YearPerOrg",
                schema: "Planning",
                table: "PlanningCycles",
                columns: new[] { "OrganizationId", "Year" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_PlanningSequences_OrgScopeKey",
                schema: "Planning",
                table: "PlanningSequences",
                columns: new[] { "OrganizationId", "Scope", "ScopeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoaMeasureUnits_ProductId",
                schema: "Planning",
                table: "PoaMeasureUnits",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PoaProductCustomValues_TemplateColumnId",
                schema: "Planning",
                table: "PoaProductCustomValues",
                column: "TemplateColumnId");

            migrationBuilder.CreateIndex(
                name: "UX_PoaProductCustomValues_ProductColumn",
                schema: "Planning",
                table: "PoaProductCustomValues",
                columns: new[] { "ProductId", "TemplateColumnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PoaProductInvolvedAreas_ProductArea",
                schema: "Planning",
                table: "PoaProductInvolvedAreas",
                columns: new[] { "ProductId", "AreaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PoaProductMunicipalities_ProductMunicipio",
                schema: "Planning",
                table: "PoaProductMunicipalities",
                columns: new[] { "ProductId", "MunicipioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PoaProducts_PlanCode",
                schema: "Planning",
                table: "PoaProducts",
                columns: new[] { "PlanId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoaTasks_ProductId",
                schema: "Planning",
                table: "PoaTasks",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanningCycleAreas",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PlanningCycleMilestones",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PlanningSequences",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PoaMeasureUnits",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PoaProductCustomValues",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PoaProductInvolvedAreas",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PoaProductMunicipalities",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PoaTasks",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PoaProducts",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "AnnualPlans",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "PlanningCycles",
                schema: "Planning");
        }
    }
}
