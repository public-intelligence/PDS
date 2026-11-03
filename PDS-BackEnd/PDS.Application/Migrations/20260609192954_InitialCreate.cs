using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Organization");

            migrationBuilder.CreateTable(
                name: "OccupationalGroups",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OccupationalGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationalStructureAreaTypes",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationalStructureAreaTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationalStructureAreaTypes_OrganizationalStructureAreaTypes_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructureAreaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationalStructures",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AreaTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationalStructures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationalStructures_OrganizationalStructureAreaTypes_AreaTypeId",
                        column: x => x.AreaTypeId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructureAreaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationalStructures_OrganizationalStructures_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                schema: "Organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationalStructureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccupationalGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Competencies = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    YearsOfExperienceRequired = table.Column<int>(type: "int", nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsManager = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_OccupationalGroups_OccupationalGroupId",
                        column: x => x.OccupationalGroupId,
                        principalSchema: "Organization",
                        principalTable: "OccupationalGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Positions_OrganizationalStructures_OrganizationalStructureId",
                        column: x => x.OrganizationalStructureId,
                        principalSchema: "Organization",
                        principalTable: "OrganizationalStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OccupationalGroups_DeletedAt",
                schema: "Organization",
                table: "OccupationalGroups",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OccupationalGroups_Name",
                schema: "Organization",
                table: "OccupationalGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalStructureAreaTypes_DeletedAt",
                schema: "Organization",
                table: "OrganizationalStructureAreaTypes",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalStructureAreaTypes_Name",
                schema: "Organization",
                table: "OrganizationalStructureAreaTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalStructureAreaTypes_ParentId",
                schema: "Organization",
                table: "OrganizationalStructureAreaTypes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalStructures_AreaTypeId",
                schema: "Organization",
                table: "OrganizationalStructures",
                column: "AreaTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalStructures_DeletedAt",
                schema: "Organization",
                table: "OrganizationalStructures",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationalStructures_ParentId",
                schema: "Organization",
                table: "OrganizationalStructures",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_DeletedAt",
                schema: "Organization",
                table: "Positions",
                column: "DeletedAt",
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OccupationalGroupId",
                schema: "Organization",
                table: "Positions",
                column: "OccupationalGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OrganizationalStructureId",
                schema: "Organization",
                table: "Positions",
                column: "OrganizationalStructureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Positions",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "OccupationalGroups",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "OrganizationalStructures",
                schema: "Organization");

            migrationBuilder.DropTable(
                name: "OrganizationalStructureAreaTypes",
                schema: "Organization");
        }
    }
}
