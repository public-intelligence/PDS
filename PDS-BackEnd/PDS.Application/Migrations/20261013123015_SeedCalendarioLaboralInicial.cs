using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// Carga inicial del calendario laboral (SPEC-LOC-CAL-01 §13.1, pasos 2.2 a 2.6 y 2.8): calendario de feriados
    /// <c>DO-NAC</c> por organización, jornada inicial lunes a viernes 08:00-16:00 para cada localidad, suscripción de
    /// las localidades dominicanas, localidad predeterminada (única SEDE_CENTRAL activa), excepciones de sistema por
    /// suspensión o cierre vigentes y una fila inicial en el feed de cambios. Ejecuta el script embebido
    /// <c>Seed/CalendarioLaboralInicial.sql</c>, idempotente (solo inserta lo que falta). Son datos de partida
    /// que los administradores mantienen luego: la reversión es un no-op.
    /// </summary>
    public partial class SeedCalendarioLaboralInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly
                .GetManifestResourceNames()
                .Single(n => n.EndsWith("CalendarioLaboralInicial.sql", System.StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            var sql = reader.ReadToEnd();

            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Datos de partida editables por los administradores; no se revierten automáticamente.
        }
    }
}
