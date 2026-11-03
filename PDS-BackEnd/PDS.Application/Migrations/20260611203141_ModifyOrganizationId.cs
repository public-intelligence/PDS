using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class ModifyOrganizationId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "Organization",
                table: "OrganizationalStructures");

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                schema: "Organization",
                table: "OrganizationalStructures",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "Organization",
                table: "OrganizationalStructures");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "Organization",
                table: "OrganizationalStructures",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");
        }
    }
}
