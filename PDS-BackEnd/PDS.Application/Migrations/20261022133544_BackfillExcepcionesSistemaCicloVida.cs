using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <summary>
    /// RF-LO-32 (migración, paso 6): crea las excepciones de sistema de las localidades que ya estaban suspendidas o cerradas, y
    /// publica un cambio <c>Migracion</c> por localidad afectada (versión +1 y fila en <c>CalendarioCambios</c>) para que los consumidores
    /// del feed (WF, SDS) recalculen. Idempotente: no duplica excepciones de sistema existentes ni toca localidades eliminadas.
    /// </summary>
    public partial class BackfillExcepcionesSistemaCicloVida : Migration
    {
        private const string MarcaMigracion = "Migración RF-LO-32";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DECLARE @Creadas TABLE (LocalidadId uniqueidentifier NOT NULL, FechaInicio date NOT NULL, FechaFin date NULL);

                INSERT INTO [Organization].[LocalidadExcepcionesCalendario]
                    ([Id], [LocalidadId], [FechaInicio], [FechaFin], [Tipo], [Categoria], [Origen], [Motivo], [InstrumentoLegalId],
                     [EsSistema], [CreatedAt], [UpdatedAt], [CreadoPorId], [CreadoPor])
                OUTPUT inserted.[LocalidadId], inserted.[FechaInicio], inserted.[FechaFin] INTO @Creadas
                SELECT NEWID(), l.[Id], l.[SuspensionFechaInicio], l.[SuspensionFechaFin], N'Cierre', N'Suspension', N'Institucional',
                       ISNULL(NULLIF(LTRIM(RTRIM(l.[SuspensionMotivo])), N''), N'Suspensión de la localidad'), l.[SuspensionInstrumentoLegalId],
                       1, SYSUTCDATETIME(), SYSUTCDATETIME(), '00000000-0000-0000-0000-000000000000', N'{MarcaMigracion}'
                FROM [Organization].[Localidades] l
                WHERE l.[DeletedAt] IS NULL
                  AND l.[SuspensionFechaInicio] IS NOT NULL
                  AND (l.[SuspensionFechaFin] IS NULL OR l.[SuspensionFechaFin] >= l.[SuspensionFechaInicio])
                  AND NOT EXISTS (SELECT 1 FROM [Organization].[LocalidadExcepcionesCalendario] e
                                  WHERE e.[LocalidadId] = l.[Id] AND e.[EsSistema] = 1 AND e.[Categoria] = N'Suspension' AND e.[DeletedAt] IS NULL);

                INSERT INTO [Organization].[LocalidadExcepcionesCalendario]
                    ([Id], [LocalidadId], [FechaInicio], [FechaFin], [Tipo], [Categoria], [Origen], [Motivo], [InstrumentoLegalId],
                     [EsSistema], [CreatedAt], [UpdatedAt], [CreadoPorId], [CreadoPor])
                OUTPUT inserted.[LocalidadId], inserted.[FechaInicio], inserted.[FechaFin] INTO @Creadas
                SELECT NEWID(), l.[Id], l.[CierreFecha], NULL, N'Cierre', N'CierreDefinitivo', N'Institucional',
                       ISNULL(NULLIF(LTRIM(RTRIM(l.[CierreMotivo])), N''), N'Cierre definitivo de la localidad'), l.[CierreInstrumentoLegalId],
                       1, SYSUTCDATETIME(), SYSUTCDATETIME(), '00000000-0000-0000-0000-000000000000', N'{MarcaMigracion}'
                FROM [Organization].[Localidades] l
                WHERE l.[DeletedAt] IS NULL
                  AND l.[CierreFecha] IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [Organization].[LocalidadExcepcionesCalendario] e
                                  WHERE e.[LocalidadId] = l.[Id] AND e.[EsSistema] = 1 AND e.[Categoria] = N'CierreDefinitivo' AND e.[DeletedAt] IS NULL);

                DECLARE @Afectadas TABLE (LocalidadId uniqueidentifier NOT NULL PRIMARY KEY, Desde date NULL, Hasta date NULL);
                INSERT INTO @Afectadas (LocalidadId, Desde, Hasta)
                SELECT c.[LocalidadId], MIN(c.[FechaInicio]),
                       CASE WHEN COUNT(*) = COUNT(c.[FechaFin]) THEN MAX(c.[FechaFin]) ELSE NULL END
                FROM @Creadas c GROUP BY c.[LocalidadId];

                UPDATE l SET l.[CalendarioVersion] = l.[CalendarioVersion] + 1
                FROM [Organization].[Localidades] l INNER JOIN @Afectadas a ON a.[LocalidadId] = l.[Id];

                INSERT INTO [Organization].[CalendarioCambios] ([OrganizationId], [LocalidadId], [CalendarioVersion], [Motivo], [FechaDesde], [FechaHasta])
                SELECT l.[OrganizationId], l.[Id], l.[CalendarioVersion], N'Migracion', a.[Desde], a.[Hasta]
                FROM [Organization].[Localidades] l INNER JOIN @Afectadas a ON a.[LocalidadId] = l.[Id];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Solo retira las excepciones que creó esta migración; las versiones y filas del feed son append-only y se conservan.
            migrationBuilder.Sql($"""
                DELETE FROM [Organization].[LocalidadExcepcionesCalendario]
                WHERE [EsSistema] = 1 AND [CreadoPor] = N'{MarcaMigracion}';
                """);
        }
    }
}
