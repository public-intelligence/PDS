using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class RenameCalendarioPredeterminadaIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Localidades_CalendarioPredeterminada",
                schema: "Organization",
                table: "Localidades",
                newName: "UX_Localidades_Org_CalendarioPredeterminada");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_Localidades_Org_CalendarioPredeterminada",
                schema: "Organization",
                table: "Localidades",
                newName: "IX_Localidades_CalendarioPredeterminada");
        }
    }
}
