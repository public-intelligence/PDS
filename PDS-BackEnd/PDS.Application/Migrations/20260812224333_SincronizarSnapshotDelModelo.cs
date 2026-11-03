using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarSnapshotDelModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MastersoftAccounts",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ControlAccountCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    ControlAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    IsPostable = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MastersoftAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MastersoftAccounts_MastersoftAccounts_ControlAccountId",
                        column: x => x.ControlAccountId,
                        principalSchema: "Planning",
                        principalTable: "MastersoftAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MastersoftDigecobMappings",
                schema: "Planning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    DigecobAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DigecobAccountCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    MastersoftAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MastersoftAccountCode = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MastersoftDigecobMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MastersoftDigecobMappings_AccountingAccounts_DigecobAccountId",
                        column: x => x.DigecobAccountId,
                        principalSchema: "Planning",
                        principalTable: "AccountingAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MastersoftDigecobMappings_MastersoftAccounts_MastersoftAccountId",
                        column: x => x.MastersoftAccountId,
                        principalSchema: "Planning",
                        principalTable: "MastersoftAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MastersoftAccounts_ControlAccountId",
                schema: "Planning",
                table: "MastersoftAccounts",
                column: "ControlAccountId");

            migrationBuilder.CreateIndex(
                name: "UX_MastersoftAccounts_CodePerOrg",
                schema: "Planning",
                table: "MastersoftAccounts",
                columns: new[] { "OrganizationId", "Code" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MastersoftDigecobMappings_DigecobAccountId",
                schema: "Planning",
                table: "MastersoftDigecobMappings",
                column: "DigecobAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MastersoftDigecobMappings_MastersoftAccountId",
                schema: "Planning",
                table: "MastersoftDigecobMappings",
                column: "MastersoftAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MastersoftDigecobMappings_OrgDigecobCode",
                schema: "Planning",
                table: "MastersoftDigecobMappings",
                columns: new[] { "OrganizationId", "DigecobAccountCode" });

            migrationBuilder.CreateIndex(
                name: "UX_MastersoftDigecobMappings_UniquePair",
                schema: "Planning",
                table: "MastersoftDigecobMappings",
                columns: new[] { "OrganizationId", "DigecobAccountId", "MastersoftAccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MastersoftDigecobMappings",
                schema: "Planning");

            migrationBuilder.DropTable(
                name: "MastersoftAccounts",
                schema: "Planning");
        }
    }
}
