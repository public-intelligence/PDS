using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalidadCiudadSubdivision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ciudad",
                schema: "Organization",
                table: "Localidades",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubdivisionAdministrativa",
                schema: "Organization",
                table: "Localidades",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ciudad",
                schema: "Organization",
                table: "Localidades");

            migrationBuilder.DropColumn(
                name: "SubdivisionAdministrativa",
                schema: "Organization",
                table: "Localidades");
        }
    }
}
