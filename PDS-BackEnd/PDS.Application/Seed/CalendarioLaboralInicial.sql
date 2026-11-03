-- SPEC-LOC-CAL-01 §13.1 (pasos 2.2 a 2.6 y 2.8): carga inicial del calendario laboral.
-- Idempotente: cada bloque inserta solo lo que falta (IF NOT EXISTS / WHERE NOT EXISTS), por lo que puede
-- ejecutarse varias veces sin duplicar filas. Un único lote (sin GO) para poder correr desde migrationBuilder.Sql.
SET NOCOUNT ON;

DECLARE @doId uniqueidentifier = (SELECT TOP 1 [Id] FROM [Organization].[Paises] WHERE [Codigo] = N'DO');
DECLARE @sistema uniqueidentifier = '00000000-0000-0000-0000-000000000000';
-- Forma exacta que escribe EF Core (owned types con ToJson): lunes a viernes 08:00-16:00 (480-960), sábado y domingo cerrados.
DECLARE @semana nvarchar(max) = N'[{"dia":1,"Intervalos":[{"finMinuto":960,"inicioMinuto":480}]},{"dia":2,"Intervalos":[{"finMinuto":960,"inicioMinuto":480}]},{"dia":3,"Intervalos":[{"finMinuto":960,"inicioMinuto":480}]},{"dia":4,"Intervalos":[{"finMinuto":960,"inicioMinuto":480}]},{"dia":5,"Intervalos":[{"finMinuto":960,"inicioMinuto":480}]},{"dia":6,"Intervalos":[]},{"dia":7,"Intervalos":[]}]';

-- (a) Calendario de feriados DO-NAC por organización con localidades (sin entradas: las carga un administrador).
INSERT INTO [Organization].[CalendariosFeriados] ([OrganizationId], [Codigo], [Nombre], [Descripcion], [PaisId], [Ambito], [Activo])
SELECT DISTINCT l.[OrganizationId], N'DO-NAC', N'Feriados nacionales de República Dominicana',
       N'Calendario base creado por la migración inicial. Los feriados se cargan desde la publicación oficial.',
       @doId, N'Nacional', 1
FROM [Organization].[Localidades] l
WHERE l.[DeletedAt] IS NULL
  AND @doId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [Organization].[CalendariosFeriados] c
                  WHERE c.[OrganizationId] = l.[OrganizationId] AND c.[Codigo] = N'DO-NAC' AND c.[DeletedAt] IS NULL);

-- (b) Versión inicial de la jornada semanal (2000-01-01) para cada localidad que no tiene ninguna.
INSERT INTO [Organization].[LocalidadJornadas] ([LocalidadId], [VigenteDesde], [Motivo], [CreadoPorId], [CreadoPor], [Semana])
SELECT l.[Id], '2000-01-01', N'Migración inicial (lunes a viernes 08:00-16:00)', @sistema, N'Sistema', @semana
FROM [Organization].[Localidades] l
WHERE l.[DeletedAt] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [Organization].[LocalidadJornadas] j WHERE j.[LocalidadId] = l.[Id]);

-- (c) Las localidades dominicanas se suscriben a DO-NAC (sin duplicar el par localidad-calendario).
INSERT INTO [Organization].[LocalidadCalendariosFeriados] ([LocalidadId], [CalendarioFeriadosId], [VigenteDesde], [VigenteHasta], [CreadoPorId], [CreadoPor])
SELECT l.[Id], c.[Id], NULL, NULL, @sistema, N'Sistema'
FROM [Organization].[Localidades] l
JOIN [Organization].[CalendariosFeriados] c
  ON c.[OrganizationId] = l.[OrganizationId] AND c.[Codigo] = N'DO-NAC' AND c.[DeletedAt] IS NULL
WHERE l.[DeletedAt] IS NULL
  AND l.[PaisId] = @doId
  AND NOT EXISTS (SELECT 1 FROM [Organization].[LocalidadCalendariosFeriados] s
                  WHERE s.[LocalidadId] = l.[Id] AND s.[CalendarioFeriadosId] = c.[Id]);

-- (d) Localidad predeterminada del calendario: la única SEDE_CENTRAL activa de la organización, si aún no hay ninguna marcada.
UPDATE l SET l.[EsLocalidadCalendarioPredeterminada] = 1
FROM [Organization].[Localidades] l
JOIN [Organization].[TiposLocalidad] t ON t.[Id] = l.[TipoLocalidadId] AND t.[Codigo] = N'SEDE_CENTRAL'
JOIN [Organization].[EstadosLocalidad] e ON e.[Id] = l.[EstadoLocalidadId] AND e.[Codigo] = N'ACTIVA'
WHERE l.[DeletedAt] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [Organization].[Localidades] x
                  WHERE x.[OrganizationId] = l.[OrganizationId] AND x.[DeletedAt] IS NULL AND x.[EsLocalidadCalendarioPredeterminada] = 1)
  AND (SELECT COUNT(*)
       FROM [Organization].[Localidades] y
       JOIN [Organization].[TiposLocalidad] ty ON ty.[Id] = y.[TipoLocalidadId] AND ty.[Codigo] = N'SEDE_CENTRAL'
       JOIN [Organization].[EstadosLocalidad] ey ON ey.[Id] = y.[EstadoLocalidadId] AND ey.[Codigo] = N'ACTIVA'
       WHERE y.[OrganizationId] = l.[OrganizationId] AND y.[DeletedAt] IS NULL) = 1;

-- (e) Excepciones de sistema por el ciclo de vida actual (RF-LO-32): suspensión vigente y cierre definitivo.
INSERT INTO [Organization].[LocalidadExcepcionesCalendario]
    ([LocalidadId], [FechaInicio], [FechaFin], [Tipo], [Categoria], [Origen], [Motivo], [EsSistema], [CreadoPorId], [CreadoPor])
SELECT l.[Id],
       COALESCE(l.[SuspensionFechaInicio], CONVERT(date, l.[UpdatedAt])),
       l.[SuspensionFechaFin],
       N'Cierre', N'Suspension', N'Institucional',
       LEFT(CASE WHEN NULLIF(LTRIM(RTRIM(l.[SuspensionMotivo])), N'') IS NULL
                 THEN N'Suspensión temporal de la localidad'
                 ELSE N'Suspensión temporal de la localidad: ' + LTRIM(RTRIM(l.[SuspensionMotivo])) END, 1000),
       1, @sistema, N'Sistema'
FROM [Organization].[Localidades] l
JOIN [Organization].[EstadosLocalidad] e ON e.[Id] = l.[EstadoLocalidadId] AND e.[Codigo] = N'SUSPENDIDA_TEMP'
WHERE l.[DeletedAt] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [Organization].[LocalidadExcepcionesCalendario] x
                  WHERE x.[LocalidadId] = l.[Id] AND x.[EsSistema] = 1 AND x.[Categoria] = N'Suspension'
                    AND x.[DeletedAt] IS NULL
                    AND x.[FechaInicio] = COALESCE(l.[SuspensionFechaInicio], CONVERT(date, l.[UpdatedAt])));

INSERT INTO [Organization].[LocalidadExcepcionesCalendario]
    ([LocalidadId], [FechaInicio], [FechaFin], [Tipo], [Categoria], [Origen], [Motivo], [EsSistema], [CreadoPorId], [CreadoPor])
SELECT l.[Id], l.[CierreFecha], NULL, N'Cierre', N'CierreDefinitivo', N'Institucional',
       N'Cierre definitivo de la localidad', 1, @sistema, N'Sistema'
FROM [Organization].[Localidades] l
WHERE l.[DeletedAt] IS NULL
  AND l.[CierreFecha] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [Organization].[LocalidadExcepcionesCalendario] x
                  WHERE x.[LocalidadId] = l.[Id] AND x.[EsSistema] = 1 AND x.[Categoria] = N'CierreDefinitivo'
                    AND x.[DeletedAt] IS NULL);

-- (f) Una fila inicial en el feed de cambios por localidad (CalendarioVersion permanece en 1); los consumidores sincronizan completo.
INSERT INTO [Organization].[CalendarioCambios] ([OrganizationId], [LocalidadId], [CalendarioVersion], [Motivo], [FechaDesde], [FechaHasta])
SELECT l.[OrganizationId], l.[Id], l.[CalendarioVersion], N'Migracion', NULL, NULL
FROM [Organization].[Localidades] l
WHERE l.[DeletedAt] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [Organization].[CalendarioCambios] c WHERE c.[LocalidadId] = l.[Id]);
