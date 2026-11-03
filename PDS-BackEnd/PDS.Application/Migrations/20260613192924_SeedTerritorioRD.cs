using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// Siembra la división territorial de la República Dominicana (país, regiones de planificación,
    /// provincias, municipios y distritos municipales) ejecutando el script SQL embebido
    /// <c>Seed/TerritorioRD.sql</c>. El script es idempotente (INSERT ... WHERE NOT EXISTS por Codigo),
    /// por lo que puede reaplicarse sin duplicar filas. Datos de referencia: la reversión es un no-op.
    /// </summary>
    public partial class SeedTerritorioRD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .Single(n => n.EndsWith("TerritorioRD.sql", System.StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La división territorial es dato de referencia nacional; no se revierte automáticamente.
        }
    }
}
