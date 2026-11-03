using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsableGobidUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GobidUserId",
                schema: "Organization",
                table: "Responsables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_Responsables_Org_GobidUserId",
                schema: "Organization",
                table: "Responsables",
                columns: new[] { "OrganizationId", "GobidUserId" },
                unique: true,
                filter: "[GobidUserId] IS NOT NULL AND [DeletedAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Responsables_Org_GobidUserId",
                schema: "Organization",
                table: "Responsables");

            migrationBuilder.DropColumn(
                name: "GobidUserId",
                schema: "Organization",
                table: "Responsables");
        }
    }
}
