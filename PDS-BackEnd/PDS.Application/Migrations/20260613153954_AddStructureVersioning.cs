using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddStructureVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalInstruments",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Number = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalInstruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StructureVersions",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalInstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PublishedByName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructureVersions_LegalInstruments_LegalInstrumentId",
                        column: x => x.LegalInstrumentId,
                        principalSchema: "Organization",
                        principalTable: "LegalInstruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StructureVersionAuditLogs",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StructureVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureVersionAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructureVersionAuditLogs_StructureVersions_StructureVersionId",
                        column: x => x.StructureVersionId,
                        principalSchema: "Organization",
                        principalTable: "StructureVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StructureVersionPositions",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StructureVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOrganizationalStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AuthorizedPosts = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    IsSupervisor = table.Column<bool>(type: "bit", nullable: false),
                    OccupationalGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccupationalGroupName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureVersionPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructureVersionPositions_StructureVersions_StructureVersionId",
                        column: x => x.StructureVersionId,
                        principalSchema: "Organization",
                        principalTable: "StructureVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StructureVersionUnits",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    StructureVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOrganizationalStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AreaTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaTypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ParentSourceUnitId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StructureVersionUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StructureVersionUnits_StructureVersions_StructureVersionId",
                        column: x => x.StructureVersionId,
                        principalSchema: "Organization",
                        principalTable: "StructureVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalInstruments_DeletedAt",
                schema: "Organization",
                table: "LegalInstruments",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StructureVersionAuditLogs_StructureVersionId",
                schema: "Organization",
                table: "StructureVersionAuditLogs",
                column: "StructureVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_StructureVersionPositions_StructureVersionId_SourceOrganizationalStructureId",
                schema: "Organization",
                table: "StructureVersionPositions",
                columns: new[] { "StructureVersionId", "SourceOrganizationalStructureId" });

            migrationBuilder.CreateIndex(
                name: "IX_StructureVersions_LegalInstrumentId",
                schema: "Organization",
                table: "StructureVersions",
                column: "LegalInstrumentId");

            migrationBuilder.CreateIndex(
                name: "UX_StructureVersions_NumberPerOrg",
                schema: "Organization",
                table: "StructureVersions",
                columns: new[] { "OrganizationId", "Number" },
                unique: true,
                filter: "[Number] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_StructureVersions_OneDraftPerOrg",
                schema: "Organization",
                table: "StructureVersions",
                column: "OrganizationId",
                unique: true,
                filter: "[Status] = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "UX_StructureVersions_OneOpenPublishedPerOrg",
                schema: "Organization",
                table: "StructureVersions",
                column: "OrganizationId",
                unique: true,
                filter: "[Status] = 'Published' AND [EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StructureVersionUnits_StructureVersionId_ParentSourceUnitId",
                schema: "Organization",
                table: "StructureVersionUnits",
                columns: new[] { "StructureVersionId", "ParentSourceUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_StructureVersionUnits_StructureVersionId_SourceOrganizationalStructureId",
                schema: "Organization",
                table: "StructureVersionUnits",
                columns: new[] { "StructureVersionId", "SourceOrganizationalStructureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StructureVersionAuditLogs",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "StructureVersionPositions",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "StructureVersionUnits",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "StructureVersions",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "LegalInstruments",
                schema: "Organization");
        }
    }
}
