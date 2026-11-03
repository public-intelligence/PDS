using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalidadZonaHoraria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZonaHoraria",
                schema: "Organization",
                table: "Localidades",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "America/Santo_Domingo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZonaHoraria",
                schema: "Organization",
                table: "Localidades");
        }
    }
}
