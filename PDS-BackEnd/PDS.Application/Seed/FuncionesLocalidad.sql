-- Funciones de localidad del servicio exterior (B30 / RF-LO-45, SPEC-LOC-CAL-01 Apéndice C).
-- Idempotente: solo inserta las funciones cuyo Codigo no existe entre las no eliminadas; nunca pisa datos editados.
SET NOCOUNT ON;

DECLARE @funciones TABLE ([Codigo] nvarchar(50) NOT NULL, [Nombre] nvarchar(250) NOT NULL, [Categoria] nvarchar(30) NOT NULL);
INSERT INTO @funciones ([Codigo], [Nombre], [Categoria]) VALUES
    (N'EMBAJADA', N'Embajada', N'Diplomatica'),
    (N'MISION_PERMANENTE', N'Misión permanente ante organismo internacional', N'Multilateral'),
    (N'CONSULADO_GENERAL', N'Consulado general', N'Consular'),
    (N'SECCION_CONSULAR', N'Sección consular', N'Consular');

INSERT INTO [Organization].[FuncionesLocalidad] ([Id], [Codigo], [Nombre], [Categoria], [Descripcion], [CreatedAt], [UpdatedAt])
SELECT NEWID(), f.[Codigo], f.[Nombre], f.[Categoria], NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
FROM @funciones f
WHERE NOT EXISTS (
    SELECT 1 FROM [Organization].[FuncionesLocalidad] x
    WHERE x.[Codigo] = f.[Codigo] AND x.[DeletedAt] IS NULL);
