using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// Siembra los catálogos de clasificación de localidades (tipo, naturaleza administrativa y estado).
    /// Datos de referencia compartidos (sin OrganizationId). Idempotente: cada fila se inserta solo si
    /// no existe ya su <c>Codigo</c>, por lo que la migración puede reaplicarse sin duplicar.
    /// </summary>
    public partial class SeedLocalidadCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Tipos de localidad ──
            InsertCatalog(migrationBuilder, "TiposLocalidad", new[]
            {
                ("SEDE_CENTRAL", "Sede central"),
                ("EDIFICIO_ANEXO", "Edificio anexo"),
                ("OFICINA_REGIONAL", "Oficina regional"),
                ("OFICINA_PROVINCIAL", "Oficina provincial"),
                ("OFICINA_MUNICIPAL", "Oficina municipal / local"),
                ("VENTANILLA", "Ventanilla de atención"),
                ("ALMACEN", "Almacén"),
                ("ARCHIVO", "Archivo"),
                ("PUNTO_TEMPORAL", "Punto temporal de servicio"),
                ("OTRO", "Otro"),
            });

            // ── Naturalezas administrativas ──
            InsertCatalog(migrationBuilder, "NaturalezasAdministrativas", new[]
            {
                ("SEDE_ADMINISTRATIVA", "Sede administrativa"),
                ("ORG_DESCONC_TERRITORIAL", "Órgano desconcentrado territorial"),
                ("ORG_DESCONC_FUNCIONAL", "Órgano desconcentrado funcional"),
                ("PUNTO_ATENCION", "Punto de atención ciudadana"),
                ("CENTRO_OPERATIVO", "Centro operativo"),
                ("DEPENDENCIA_LOGISTICA", "Dependencia logística"),
                ("OTRA", "Otra"),
            });

            // ── Estados de localidad (con color para el mapa y bandera terminal) ──
            InsertEstado(migrationBuilder, "ACTIVA", "Activa", "#16a34a", terminal: false);
            InsertEstado(migrationBuilder, "EN_APERTURA", "En apertura", "#2563eb", terminal: false);
            InsertEstado(migrationBuilder, "SUSPENDIDA_TEMP", "Suspendida temporalmente", "#d97706", terminal: false);
            InsertEstado(migrationBuilder, "EN_CIERRE", "En cierre", "#64748b", terminal: true);
            InsertEstado(migrationBuilder, "INACTIVA", "Inactiva", "#dc2626", terminal: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Organization].[EstadosLocalidad] WHERE [Codigo] IN (N'ACTIVA', N'EN_APERTURA', N'SUSPENDIDA_TEMP', N'EN_CIERRE', N'INACTIVA');");
            migrationBuilder.Sql("DELETE FROM [Organization].[NaturalezasAdministrativas] WHERE [Codigo] IN (N'SEDE_ADMINISTRATIVA', N'ORG_DESCONC_TERRITORIAL', N'ORG_DESCONC_FUNCIONAL', N'PUNTO_ATENCION', N'CENTRO_OPERATIVO', N'DEPENDENCIA_LOGISTICA', N'OTRA');");
            migrationBuilder.Sql("DELETE FROM [Organization].[TiposLocalidad] WHERE [Codigo] IN (N'SEDE_CENTRAL', N'EDIFICIO_ANEXO', N'OFICINA_REGIONAL', N'OFICINA_PROVINCIAL', N'OFICINA_MUNICIPAL', N'VENTANILLA', N'ALMACEN', N'ARCHIVO', N'PUNTO_TEMPORAL', N'OTRO');");
        }

        private static void InsertCatalog(MigrationBuilder mb, string table, (string Codigo, string Nombre)[] rows)
        {
            foreach (var (codigo, nombre) in rows)
            {
                mb.Sql(
                    $"IF NOT EXISTS (SELECT 1 FROM [Organization].[{table}] WHERE [Codigo] = N'{codigo}') " +
                    $"INSERT INTO [Organization].[{table}] ([Id], [Codigo], [Nombre]) VALUES (NEWID(), N'{codigo}', N'{nombre.Replace("'", "''")}');");
            }
        }

        private static void InsertEstado(MigrationBuilder mb, string codigo, string nombre, string color, bool terminal)
        {
            mb.Sql(
                "IF NOT EXISTS (SELECT 1 FROM [Organization].[EstadosLocalidad] WHERE [Codigo] = N'" + codigo + "') " +
                "INSERT INTO [Organization].[EstadosLocalidad] ([Id], [Codigo], [Nombre], [Color], [EsTerminal]) " +
                $"VALUES (NEWID(), N'{codigo}', N'{nombre.Replace("'", "''")}', N'{color}', {(terminal ? 1 : 0)});");
        }
    }
}
