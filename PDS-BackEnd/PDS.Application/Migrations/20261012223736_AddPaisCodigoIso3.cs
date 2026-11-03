using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// B01: agrega <c>Paises.CodigoIso3</c> (índice único filtrado) y siembra los 249 países ISO 3166-1 con el
    /// script embebido <c>Seed/PaisesIso3166.sql</c> (idempotente; completa el alfa-3 de DO sin renombrarlo).
    /// </summary>
    public partial class AddPaisCodigoIso3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoIso3",
                schema: "Organization",
                table: "Paises",
                type: "varchar(3)",
                unicode: false,
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paises_CodigoIso3",
                schema: "Organization",
                table: "Paises",
                column: "CodigoIso3",
                unique: true,
                filter: "[CodigoIso3] IS NOT NULL");

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .Single(n => n.EndsWith("PaisesIso3166.sql", System.StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Los países sembrados son datos de referencia; solo se revierte el esquema (los países nuevos se
            // conservan por si ya hay localidades que los referencian).
            migrationBuilder.DropIndex(
                name: "IX_Paises_CodigoIso3",
                schema: "Organization",
                table: "Paises");

            migrationBuilder.DropColumn(
                name: "CodigoIso3",
                schema: "Organization",
                table: "Paises");
        }
    }
}
