using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Data;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.Enums;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Excel;

namespace PDS.Service.Services.Planning;

public class MasterDataImportService : IMasterDataImportService
{
    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly IConsolidatorRoutingService _routingService;
    private readonly IPlanningActivityService _activityService;
    private readonly IHostEnvironment _env;

    public MasterDataImportService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        IConsolidatorRoutingService routingService,
        IPlanningActivityService activityService,
        IHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _routingService = routingService;
        _activityService = activityService;
        _env = env;
    }

    public async Task<IHttpResult<MasterDataImportResultDto>> ImportFromRepositoryAsync(int organizationId, bool dryRun, Guid userId, string? userName)
    {
        if (!_env.IsDevelopment())
        {
            return HttpResult<MasterDataImportResultDto>.Failure(
                "Acceso no permitido",
                new Dictionary<string, IEnumerable<string>> { { "import", new[] { "Este endpoint solo está disponible en entorno de desarrollo." } } },
                HttpStatusCode.Forbidden);
        }

        var repositoryFilePath = Path.Combine(_env.ContentRootPath, "..", "docs", "Maestra_Institucional_de_Articulos.xlsx");
        if (!File.Exists(repositoryFilePath))
        {
            repositoryFilePath = Path.Combine(_env.ContentRootPath, "docs", "Maestra_Institucional_de_Articulos.xlsx");
        }

        if (!File.Exists(repositoryFilePath))
        {
            return HttpResult<MasterDataImportResultDto>.Failure(
                "Archivo no encontrado",
                new Dictionary<string, IEnumerable<string>> { { "file", new[] { $"No se encontró el archivo institucional en: {repositoryFilePath}" } } },
                HttpStatusCode.NotFound);
        }

        using var stream = File.OpenRead(repositoryFilePath);
        return await ImportAsync(organizationId, stream, dryRun, userId, userName);
    }

    public async Task<IHttpResult<MasterDataImportResultDto>> ImportAsync(int organizationId, Stream stream, bool dryRun, Guid userId, string? userName)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        using var workbook = new XLWorkbook(stream);

        // Con EnableRetryOnFailure, una transacción manual solo se permite dentro de la estrategia de
        // ejecución, que reintenta el bloque completo (begin/commit incluidos) ante fallos transitorios.
        var strategy = _unitOfWork.Context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync<IHttpResult<MasterDataImportResultDto>>(async () =>
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.Context.ChangeTracker.AutoDetectChangesEnabled = false;

                // 1. Catálogo de Áreas -> ConsolidatorAreaProfiles
                var areasSummary = await ImportAreasAsync(organizationId, workbook, warnings);

                // 2. Catálogo UNSPSC -> UnifiedCodes
                var unspscSummary = await ImportUnspscAsync(organizationId, workbook);

                // 3. Catálogo Objetos del Gasto -> ExpenseObjects
                var expenseSummary = await ImportExpenseObjectsAsync(organizationId, workbook);

                // 4. Plan de Cuentas -> AccountingAccounts
                var accountingSummary = await ImportAccountingAccountsAsync(organizationId, workbook);

                // 5. Reglas Área Consolidadora -> ConsolidatorRoutingRules
                var rulesSummary = await ImportRoutingRulesAsync(organizationId, workbook);

                // 6. Maestra de Artículos -> MasterProducts + Aliases
                var productsSummary = await ImportMasterProductsAsync(organizationId, workbook);

                // 7. Motor de reglas re-evalúa artículos no ratificados
                if (!dryRun)
                {
                    await _routingService.ReevaluateAsync(organizationId, null, onlyUnratified: true, userId, userName);
                }

                var result = new MasterDataImportResultDto
                {
                    Applied = !dryRun,
                    AreasSummary = areasSummary,
                    UnspscSummary = unspscSummary,
                    ExpenseObjectsSummary = expenseSummary,
                    AccountingAccountsSummary = accountingSummary,
                    RoutingRulesSummary = rulesSummary,
                    MasterProductsSummary = productsSummary,
                    Warnings = warnings,
                    Errors = errors
                };

                if (dryRun)
                {
                    await _unitOfWork.RollbackAsync();
                }
                else
                {
                    await _unitOfWork.Context.SaveChangesAsync();
                    await _unitOfWork.CommitAsync();

                    await _activityService.LogAsync(
                        organizationId,
                        userId,
                        userName,
                        "Catálogos",
                        "Importó Maestra Institucional",
                        $"Importó la Maestra de Artículos ({productsSummary.Added} creados, {productsSummary.Updated} actualizados)",
                        "MasterProduct",
                        Guid.Empty);
                }

                return HttpResult<MasterDataImportResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();

                // EF envuelve el error real ("see inner exception"); sin la cadena completa
                // el mensaje que llega al usuario no dice nada accionable.
                var detalle = ex.Message;
                for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
                    detalle += $" → {inner.Message}";

                errors.Add($"Error crítico durante la importación: {detalle}");

                return HttpResult<MasterDataImportResultDto>.Failure(
                    "Error en la importación",
                    new Dictionary<string, IEnumerable<string>> { { "import", errors } },
                    HttpStatusCode.InternalServerError);
            }
            finally
            {
                _unitOfWork.Context.ChangeTracker.AutoDetectChangesEnabled = true;
            }
        });
    }

    private async Task<SheetImportSummaryDto> ImportAreasAsync(int orgId, IXLWorkbook workbook, List<string> warnings)
    {
        var sheetName = "Catálogo de Áreas";
        var table = ExcelTableReader.Read(workbook, sheetName, maxRows: 1000, maxColumns: 50);

        var existingStructures = await _unitOfWork.Context.OrganizationalStructures
            .AsNoTracking()
            .Where(s => s.OrganizationId == orgId && s.DeletedAt == null)
            .ToListAsync();

        var existingProfiles = await _unitOfWork.Context.ConsolidatorAreaProfiles
            .Where(p => p.OrganizationId == orgId && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.AreaCode, StringComparer.OrdinalIgnoreCase);

        int added = 0, updated = 0, unchanged = 0;

        foreach (var row in table.Rows)
        {
            var code = row.Get("codigo_area");
            var name = row.Get("nombre_area");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            var normName = ExcelTableReader.Normalize(name);
            var matchedStruct = existingStructures.FirstOrDefault(s => ExcelTableReader.Normalize(s.Name) == normName);

            if (matchedStruct is null)
            {
                warnings.Add($"Catálogo de Áreas: El área '{name}' ({code}) no resolvió una Estructura Organizativa activa.");
            }

            if (existingProfiles.TryGetValue(code, out var profile))
            {
                var isChanged = profile.AreaName != name ||
                                profile.OrganicGroup != row.Get("grupo_organico") ||
                                profile.OrganizationalStructureId != matchedStruct?.Id;

                if (isChanged)
                {
                    profile.AreaName = name;
                    profile.NormalizedAreaName = normName;
                    profile.OrganicGroup = row.Get("grupo_organico");
                    profile.OrganizationalStructureId = matchedStruct?.Id;
                    profile.MandateSummary = row.Get("mandato_resumido");
                    profile.ConsolidatorScope = row.Get("alcance_consolidador");
                    profile.Limits = row.Get("limites");
                    profile.CatalogVersion = row.Get("version_catalogo");
                    profile.UpdatedAt = DateTime.UtcNow;
                    updated++;
                }
                else unchanged++;
            }
            else
            {
                var newProfile = new ConsolidatorAreaProfile
                {
                    OrganizationId = orgId,
                    AreaCode = code,
                    OrganicGroup = row.Get("grupo_organico"),
                    AreaName = name,
                    NormalizedAreaName = normName,
                    OrganizationalStructureId = matchedStruct?.Id,
                    MandateSummary = row.Get("mandato_resumido"),
                    ConsolidatorKind = "AreaConsolidadora",
                    ConsolidatorScope = row.Get("alcance_consolidador"),
                    Limits = row.Get("limites"),
                    CatalogVersion = row.Get("version_catalogo"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Context.ConsolidatorAreaProfiles.AddAsync(newProfile);
                added++;
            }
        }

        await _unitOfWork.Context.SaveChangesAsync();
        return new SheetImportSummaryDto { SheetName = sheetName, TotalRows = table.Rows.Count, Added = added, Updated = updated, Unchanged = unchanged, Disabled = 0 };
    }

    /// <summary>
    /// <summary>
    /// Hoja "Catálogo UNSPSC" (~70k filas). Una fila por registro de origen, con las cuatro
    /// columnas jerárquicas repetidas; `nivel_fuente` (S/F/C/M) dice a qué nivel pertenece.
    ///
    /// El change tracker se vacía tras cada lote: con 20 mil entidades vivas cada
    /// SaveChanges se vuelve cuadrático y el import no termina. Por eso los diccionarios
    /// guardan <c>Guid</c>, no referencias a entidades.
    /// </summary>
    private async Task<SheetImportSummaryDto> ImportUnspscAsync(int orgId, IXLWorkbook workbook)
    {
        var sheetName = "Catálogo UNSPSC";
        var table = ExcelTableReader.Read(workbook, sheetName, maxRows: 100000, maxColumns: 50);

        var existentes = await _unitOfWork.Context.UnifiedCodes
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId && u.DeletedAt == null)
            .Select(u => new { u.Id, u.Code, u.Name, u.EnglishName, u.Level })
            .ToDictionaryAsync(u => u.Code, StringComparer.OrdinalIgnoreCase);

        int added = 0, updated = 0, unchanged = 0;
        var nuevos = new List<UnifiedCode>();
        var cambiados = new List<(Guid Id, string Name, string Level, string? English, string? Definition)>();
        var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in table.Rows)
        {
            var (level, code) = row.Get("nivel_fuente").Trim().ToUpperInvariant() switch
            {
                "S" => ("Segment", row.Get("segmento")),
                "F" => ("Family", row.Get("familia")),
                "C" => ("Class", row.Get("clase")),
                "M" => ("Material", row.Get("material")),
                _ => (string.Empty, string.Empty)
            };

            var name = row.Get("descripcion_es");
            if (string.IsNullOrWhiteSpace(level) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                continue;

            // La hoja repite los ancestros en cada fila: el primero gana.
            if (!vistos.Add(code)) continue;

            var englishName = Recortar(row.Get("descripcion_en"), 500);
            var definition = Recortar(row.Get("definicion"), 2000);

            if (existentes.TryGetValue(code, out var actual))
            {
                if (actual.Name != name || actual.EnglishName != englishName || actual.Level != level)
                {
                    cambiados.Add((actual.Id, name, level, englishName, definition));
                    updated++;
                }
                else unchanged++;
            }
            else
            {
                nuevos.Add(new UnifiedCode
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    Code = code,
                    Name = Recortar(name, 500)!,
                    Level = level,
                    EnglishName = englishName,
                    Definition = definition,
                    SourceRole = Recortar(row.Get("rol_fuente"), 30),
                    IncludedInMaster = EsAfirmativo(row.Get("incluido_maestra")),
                    LegacyExpenseObjectCode = Recortar(row.Get("objeto_legado_1"), 20),
                    LegacyExpenseObjectName = Recortar(row.Get("descripcion_objeto_legado_1"), 2000),
                    Source = Recortar(row.Get("fuente"), 200),
                    InstitutionallyEnabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                added++;
            }
        }

        await BulkInsertUnifiedCodesAsync(nuevos);

        foreach (var lote in cambiados.Chunk(LoteEscritura))
        {
            var ids = lote.Select(c => c.Id).ToList();
            var entidades = await _unitOfWork.Context.UnifiedCodes.Where(u => ids.Contains(u.Id)).ToListAsync();
            foreach (var e in entidades)
            {
                var datos = lote.First(c => c.Id == e.Id);
                e.Name = datos.Name;
                e.Level = datos.Level;
                e.EnglishName = datos.English;
                e.Definition = datos.Definition;
                e.UpdatedAt = DateTime.UtcNow;
            }
            await _unitOfWork.Context.SaveChangesAsync();
            _unitOfWork.Context.ChangeTracker.Clear();
        }

        // Corregir y normalizar el campo Level según la estructura oficial de 8 dígitos de UNSPSC
        await _unitOfWork.Context.Database.ExecuteSqlRawAsync(@"
UPDATE Planning.UnifiedCodes
SET Level = CASE 
    WHEN Code LIKE '%000000' THEN 'Segment'
    WHEN Code LIKE '%0000'   THEN 'Family'
    WHEN Code LIKE '%00'     THEN 'Class'
    ELSE 'Material'
END
WHERE OrganizationId = {0} AND DeletedAt IS NULL AND LEN(Code) = 8;

-- 1. Material -> Class (6 dígitos + 00)
UPDATE h SET h.ParentId = p.Id
FROM Planning.UnifiedCodes h
JOIN Planning.UnifiedCodes p ON p.OrganizationId = h.OrganizationId AND p.DeletedAt IS NULL AND p.Code = LEFT(h.Code, 6) + '00'
WHERE h.OrganizationId = {0} AND h.DeletedAt IS NULL AND h.Level = 'Material' AND p.Id <> h.Id;

-- 2. Class -> Family (4 dígitos + 0000)
UPDATE h SET h.ParentId = p.Id
FROM Planning.UnifiedCodes h
JOIN Planning.UnifiedCodes p ON p.OrganizationId = h.OrganizationId AND p.DeletedAt IS NULL AND p.Code = LEFT(h.Code, 4) + '0000'
WHERE h.OrganizationId = {0} AND h.DeletedAt IS NULL AND h.Level = 'Class' AND p.Id <> h.Id;

-- 3. Family -> Segment (2 dígitos + 000000)
UPDATE h SET h.ParentId = p.Id
FROM Planning.UnifiedCodes h
JOIN Planning.UnifiedCodes p ON p.OrganizationId = h.OrganizationId AND p.DeletedAt IS NULL AND p.Code = LEFT(h.Code, 2) + '000000'
WHERE h.OrganizationId = {0} AND h.DeletedAt IS NULL AND h.Level = 'Family' AND p.Id <> h.Id;
", orgId);

        return new SheetImportSummaryDto { SheetName = sheetName, TotalRows = table.Rows.Count, Added = added, Updated = updated, Unchanged = unchanged, Disabled = 0 };
    }

    /// <summary>Tamaño de lote de escritura. Vaciar el tracker es lo que mantiene el import lineal.</summary>
    private const int LoteEscritura = 2000;

    private async Task BulkInsertUnifiedCodesAsync(List<UnifiedCode> entidades)
    {
        if (entidades.Count == 0) return;

        var dbConnection = _unitOfWork.Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        var sqlConnection = (SqlConnection)dbConnection;
        var currentTx = _unitOfWork.Context.Database.CurrentTransaction;
        var sqlTransaction = currentTx != null ? (SqlTransaction)currentTx.GetDbTransaction() : null;

        using var bulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction)
        {
            DestinationTableName = "Planning.UnifiedCodes",
            BatchSize = 10000,
            BulkCopyTimeout = 600
        };

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("OrganizationId", typeof(int));
        table.Columns.Add("Code", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Level", typeof(string));
        table.Columns.Add("EnglishName", typeof(string));
        table.Columns.Add("Definition", typeof(string));
        table.Columns.Add("SourceRole", typeof(string));
        table.Columns.Add("IncludedInMaster", typeof(bool));
        table.Columns.Add("LegacyExpenseObjectCode", typeof(string));
        table.Columns.Add("LegacyExpenseObjectName", typeof(string));
        table.Columns.Add("Source", typeof(string));
        table.Columns.Add("InstitutionallyEnabled", typeof(bool));
        table.Columns.Add("CreatedAt", typeof(DateTime));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        foreach (DataColumn col in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        foreach (var u in entidades)
        {
            table.Rows.Add(
                u.Id == Guid.Empty ? Guid.NewGuid() : u.Id,
                u.OrganizationId,
                u.Code,
                u.Name,
                u.Level,
                (object?)u.EnglishName ?? DBNull.Value,
                (object?)u.Definition ?? DBNull.Value,
                (object?)u.SourceRole ?? DBNull.Value,
                u.IncludedInMaster,
                (object?)u.LegacyExpenseObjectCode ?? DBNull.Value,
                (object?)u.LegacyExpenseObjectName ?? DBNull.Value,
                (object?)u.Source ?? DBNull.Value,
                u.InstitutionallyEnabled,
                u.CreatedAt,
                u.UpdatedAt
            );
        }

        await bulkCopy.WriteToServerAsync(table);
    }

    private static string? Recortar(string? valor, int max)
    {
        if (string.IsNullOrEmpty(valor)) return string.IsNullOrWhiteSpace(valor) ? null : valor;
        return valor.Length > max ? valor[..max] : valor;
    }

    /// <summary>Interpreta las marcas booleanas del archivo ("1", "Sí", "S", "SI").</summary>
    private static bool EsAfirmativo(string? valor)
    {
        var v = ExcelTableReader.Normalize(valor ?? string.Empty);
        return v == "1" || v == "si" || v == "s";
    }

    private async Task<SheetImportSummaryDto> ImportExpenseObjectsAsync(int orgId, IXLWorkbook workbook)
    {
        var sheetName = "Catálogo Objetos del Gasto";
        var table = ExcelTableReader.Read(workbook, sheetName, maxRows: 10000, maxColumns: 50);

        var existing = await _unitOfWork.Context.ExpenseObjects
            .Where(e => e.OrganizationId == orgId && e.DeletedAt == null)
            .ToDictionaryAsync(e => e.Code, StringComparer.OrdinalIgnoreCase);

        int added = 0, updated = 0, unchanged = 0;

        foreach (var row in table.Rows)
        {
            var code = row.Get("codigo");
            var name = row.Get("denominacion");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            int.TryParse(row.Get("nivel"), out var level);
            var esDetalle = ExcelTableReader.Normalize(row.Get("es_detalle"));
            var isDetail = esDetalle == "1" || esDetalle == "si";

            if (existing.TryGetValue(code, out var item))
            {
                item.Name = name;
                item.Description = row.Get("descripcion");
                item.Level = level;
                item.IsDetail = isDetail;
                item.ParentCode = row.Get("codigo_padre");
                item.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            else
            {
                var newItem = new ExpenseObject
                {
                    OrganizationId = orgId,
                    Code = code,
                    Name = name,
                    Description = row.Get("descripcion"),
                    Level = level,
                    ParentCode = row.Get("codigo_padre"),
                    IsDetail = isDetail,
                    Source = row.Get("fuente"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Context.ExpenseObjects.AddAsync(newItem);
                existing[code] = newItem;
                added++;
            }
        }

        await _unitOfWork.Context.SaveChangesAsync();

        // Resolver ParentIds
        var unresolved = await _unitOfWork.Context.ExpenseObjects
            .Where(e => e.OrganizationId == orgId && e.ParentId == null && e.ParentCode != null)
            .ToListAsync();

        foreach (var e in unresolved)
        {
            if (existing.TryGetValue(e.ParentCode!, out var parent))
            {
                e.ParentId = parent.Id;
            }
        }

        await _unitOfWork.Context.SaveChangesAsync();
        return new SheetImportSummaryDto { SheetName = sheetName, TotalRows = table.Rows.Count, Added = added, Updated = updated, Unchanged = unchanged, Disabled = 0 };
    }

    private async Task<SheetImportSummaryDto> ImportAccountingAccountsAsync(int orgId, IXLWorkbook workbook)
    {
        var sheetName = "Plan de Cuentas";
        var table = ExcelTableReader.Read(workbook, sheetName, maxRows: 10000, maxColumns: 50);

        var existing = await _unitOfWork.Context.AccountingAccounts
            .Where(a => a.OrganizationId == orgId && a.DeletedAt == null)
            .ToDictionaryAsync(a => a.Code, StringComparer.OrdinalIgnoreCase);

        int added = 0, updated = 0, unchanged = 0;

        foreach (var row in table.Rows)
        {
            var code = row.Get("cuenta");
            var name = row.Get("nombre");
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;

            int.TryParse(row.Get("nivel"), out var level);
            var isPostable = row.Get("imputable") == "S" || row.Get("imputable").Equals("SI", StringComparison.OrdinalIgnoreCase);

            if (existing.TryGetValue(code, out var item))
            {
                item.Name = name;
                item.IsPostable = isPostable;
                item.Level = level;
                item.ParentCode = row.Get("cuenta_padre");
                item.ExtendedDescription = row.Get("descripcion_ampliada");
                item.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            else
            {
                var newItem = new AccountingAccount
                {
                    OrganizationId = orgId,
                    Code = code,
                    Name = name,
                    IsPostable = isPostable,
                    Level = level,
                    ParentCode = row.Get("cuenta_padre"),
                    ExtendedDescription = row.Get("descripcion_ampliada"),
                    Source = row.Get("fuente"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Context.AccountingAccounts.AddAsync(newItem);
                existing[code] = newItem;
                added++;
            }
        }

        await _unitOfWork.Context.SaveChangesAsync();

        // Resolver ParentIds
        var unresolved = await _unitOfWork.Context.AccountingAccounts
            .Where(a => a.OrganizationId == orgId && a.ParentId == null && a.ParentCode != null)
            .ToListAsync();

        foreach (var a in unresolved)
        {
            if (existing.TryGetValue(a.ParentCode!, out var parent))
            {
                a.ParentId = parent.Id;
            }
        }

        await _unitOfWork.Context.SaveChangesAsync();
        return new SheetImportSummaryDto { SheetName = sheetName, TotalRows = table.Rows.Count, Added = added, Updated = updated, Unchanged = unchanged, Disabled = 0 };
    }

    private async Task<SheetImportSummaryDto> ImportRoutingRulesAsync(int orgId, IXLWorkbook workbook)
    {
        var sheetName = "Reglas Área Consolidadora";
        var table = ExcelTableReader.Read(workbook, sheetName, maxRows: 1000, maxColumns: 50);

        var existingRules = await _unitOfWork.Context.ConsolidatorRoutingRules
            .Where(r => r.OrganizationId == orgId && r.DeletedAt == null)
            .ToListAsync();

        var existingProfiles = await _unitOfWork.Context.ConsolidatorAreaProfiles
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.AreaName, StringComparer.OrdinalIgnoreCase);

        int added = 0, updated = 0, unchanged = 0;

        foreach (var row in table.Rows)
        {
            var key = row.Get("id_regla");
            int.TryParse(row.Get("version_regla"), out var version);
            if (version == 0) version = 1;
            if (string.IsNullOrWhiteSpace(key)) continue;

            int.TryParse(row.Get("prioridad"), out var priority);
            if (priority == 0) priority = 100;

            int.TryParse(row.Get("confianza_base"), out var confidence);
            if (confidence == 0) confidence = 80;

            var areaName = row.Get("area_consolidadora");
            Guid? areaId = null;
            if (existingProfiles.TryGetValue(areaName, out var profile))
            {
                areaId = profile.OrganizationalStructureId;
            }

            var rule = existingRules.FirstOrDefault(r => r.RuleKey.Equals(key, StringComparison.OrdinalIgnoreCase) && r.Version == version);

            if (rule is not null)
            {
                rule.Priority = priority;
                rule.Keywords = row.Get("palabra_clave");
                rule.KeywordsNormalized = NormalizeSeparated(row.Get("palabra_clave"));
                rule.ExclusionKeywords = row.Get("palabra_exclusion");
                rule.ExclusionKeywordsNormalized = NormalizeSeparated(row.Get("palabra_exclusion"));
                rule.UnspscPrefixes = row.Get("categoria_unspsc");
                rule.ItemType = row.Get("tipo_articulo");
                rule.ConsolidatorAreaName = areaName;
                rule.ConsolidatorAreaId = areaId;
                rule.Justification = row.Get("justificacion");
                rule.IsActive = row.Get("activo") == "1";
                rule.InstitutionallyValidated = row.Get("validada_institucionalmente") == "1";
                rule.BaseConfidence = confidence;
                rule.ForceReview = row.Get("forzar_revision") == "1";
                rule.RuleState = row.Get("estado_regla");
                rule.UpdatedAt = DateTime.UtcNow;
                updated++;
            }
            else
            {
                var newRule = new ConsolidatorRoutingRule
                {
                    OrganizationId = orgId,
                    RuleKey = key,
                    Version = version,
                    Author = row.Get("autor_regla"),
                    Priority = priority,
                    Keywords = row.Get("palabra_clave"),
                    KeywordsNormalized = NormalizeSeparated(row.Get("palabra_clave")),
                    ExclusionKeywords = row.Get("palabra_exclusion"),
                    ExclusionKeywordsNormalized = NormalizeSeparated(row.Get("palabra_exclusion")),
                    UnspscPrefixes = row.Get("categoria_unspsc"),
                    ItemType = row.Get("tipo_articulo"),
                    ConsolidatorAreaName = areaName,
                    ConsolidatorAreaId = areaId,
                    Justification = row.Get("justificacion"),
                    IsActive = row.Get("activo") == "1",
                    InstitutionallyValidated = row.Get("validada_institucionalmente") == "1",
                    BaseConfidence = confidence,
                    ForceReview = row.Get("forzar_revision") == "1",
                    RuleState = row.Get("estado_regla"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Context.ConsolidatorRoutingRules.AddAsync(newRule);
                added++;
            }
        }

        await _unitOfWork.Context.SaveChangesAsync();
        return new SheetImportSummaryDto { SheetName = sheetName, TotalRows = table.Rows.Count, Added = added, Updated = updated, Unchanged = unchanged, Disabled = 0 };
    }

    /// <summary>
    /// Hoja "Maestra de Artículos" (~18k filas + sus sinónimos). Upsert por `id_articulo`
    /// (UUIDv5 de la fuente). Como en UNSPSC, los altas se insertan por lotes vaciando el
    /// change tracker; solo se materializan las filas que de verdad cambian, para que un
    /// re-import del mismo archivo no genere escrituras.
    /// </summary>
    private async Task<SheetImportSummaryDto> ImportMasterProductsAsync(int orgId, IXLWorkbook workbook)
    {
        var sheetName = "Maestra de Artículos";
        var table = ExcelTableReader.Read(workbook, sheetName, maxRows: 100000, maxColumns: 100);

        // Proyección sin tracking: solo lo necesario para decidir alta / cambio / sin cambio.
        var existentes = await _unitOfWork.Context.MasterProducts
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.DeletedAt == null && p.SourceId != null)
            .Select(p => new
            {
                p.Id,
                SourceId = p.SourceId!.Value,
                p.Name,
                p.UnspscCode,
                p.ExpenseObjectCode,
                p.AccountingAccountCode,
                p.ConsolidatorAreaId,
                p.ConsolidatorAreaSource,
                p.State
            })
            .ToDictionaryAsync(p => p.SourceId);

        var unspscLookup = await _unitOfWork.Context.UnifiedCodes
            .AsNoTracking()
            .Where(u => u.OrganizationId == orgId && u.DeletedAt == null)
            .Select(u => new { u.Id, u.Code, u.Name })
            .ToDictionaryAsync(u => u.Code, StringComparer.OrdinalIgnoreCase);

        var expenseLookup = await _unitOfWork.Context.ExpenseObjects
            .AsNoTracking()
            .Where(e => e.OrganizationId == orgId && e.DeletedAt == null)
            .Select(e => new { e.Id, e.Code, e.Name })
            .ToDictionaryAsync(e => e.Code, StringComparer.OrdinalIgnoreCase);

        var accountLookup = await _unitOfWork.Context.AccountingAccounts
            .AsNoTracking()
            .Where(a => a.OrganizationId == orgId && a.DeletedAt == null)
            .Select(a => new { a.Id, a.Code, a.Name })
            .ToDictionaryAsync(a => a.Code, StringComparer.OrdinalIgnoreCase);

        var areaProfileLookup = await _unitOfWork.Context.ConsolidatorAreaProfiles
            .AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.DeletedAt == null)
            .ToDictionaryAsync(p => p.AreaName, StringComparer.OrdinalIgnoreCase);

        int added = 0, updated = 0, unchanged = 0;
        var nuevos = new List<MasterProduct>();
        var nuevosAliases = new List<MasterProductAlias>();
        var idsACambiar = new List<Guid>();
        var datosPorId = new Dictionary<Guid, MasterProduct>();
        var fileSourceIds = new HashSet<Guid>();

        foreach (var row in table.Rows)
        {
            var rawSourceId = row.Get("id_articulo");
            var name = Recortar(row.Get("descripcion_estandar"), 300);
            if (string.IsNullOrWhiteSpace(rawSourceId) || string.IsNullOrWhiteSpace(name)) continue;
            if (!Guid.TryParse(rawSourceId, out var sourceId)) continue;
            if (!fileSourceIds.Add(sourceId)) continue;

            decimal.TryParse(row.Get("precio_referencia"), out var refPrice);
            var synonyms = row.Get("sinonimos_fuente");
            var searchText = ExcelTableReader.Normalize($"{name} {row.Get("descripcion_original")} {synonyms} {row.Get("definicion_fuente")}");
            if (searchText.Length > 4000) searchText = searchText[..4000];

            var unspscCodeStr = row.Get("codigo_unspsc");
            unspscLookup.TryGetValue(unspscCodeStr, out var unspscObj);

            var expCodeStr = row.Get("objeto_gasto");
            expenseLookup.TryGetValue(expCodeStr, out var expObj);

            var accCodeStr = row.Get("cuenta_contable_pcc");
            accountLookup.TryGetValue(accCodeStr, out var accObj);

            var areaName = row.Get("area_consolidadora_bien");
            var isPendingArea = string.IsNullOrWhiteSpace(areaName)
                || areaName.Contains("PENDIENTE DE DEFINICIÓN INSTITUCIONAL", StringComparison.OrdinalIgnoreCase);

            Guid? areaId = null;
            if (!isPendingArea && areaProfileLookup.TryGetValue(areaName, out var areaProfile))
                areaId = areaProfile.OrganizationalStructureId;

            // Un artículo solo es utilizable si su área resolvió contra el organigrama:
            // un nombre de área que no resuelve deja el artículo pendiente, no activo.
            var areaSource = areaId.HasValue ? ConsolidatorAreaSources.Imported : ConsolidatorAreaSources.None;
            var state = areaId.HasValue ? MasterProductStates.Active : MasterProductStates.Pending;

            var datos = new MasterProduct
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                SourceId = sourceId,
                Code = $"ART-{sourceId.ToString("N")[..20].ToUpper()}",
                Name = name!,
                OriginalDescription = Recortar(row.Get("descripcion_original"), 500),
                EnglishName = Recortar(row.Get("descripcion_ingles"), 300),
                Definition = Recortar(row.Get("definicion_fuente"), 2000),
                SearchText = searchText,
                UnifiedCodeId = unspscObj?.Id,
                UnspscCode = Recortar(unspscCodeStr, 20),
                UnspscName = unspscObj?.Name,
                ItemType = Recortar(row.Get("tipo_articulo"), 20),
                ItemSubtype = Recortar(row.Get("tipo_item"), 30),
                AssetTreatment = Recortar(row.Get("tratamiento_bien"), 30),
                MeasureUnit = Recortar(row.Get("unidad_medida"), 50),
                ReferencePrice = refPrice > 0 ? refPrice : null,
                Currency = "DOP",
                ExpenseObjectId = expObj?.Id,
                ExpenseObjectCode = Recortar(expCodeStr, 20),
                ExpenseObjectName = Recortar(expObj?.Name ?? row.Get("descripcion_objeto_gasto"), 300),
                AccountingAccountId = accObj?.Id,
                AccountingAccountCode = Recortar(accCodeStr, 30),
                AccountingAccountName = Recortar(accObj?.Name ?? row.Get("descripcion_cuenta_contable"), 300),
                ConsolidatorAreaId = areaId,
                ConsolidatorAreaName = areaId.HasValue ? Recortar(areaName, 250) : null,
                ConsolidatorAreaSource = areaSource,
                State = state,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existentes.TryGetValue(sourceId, out var actual))
            {
                var cambio = actual.Name != datos.Name
                    || actual.UnspscCode != datos.UnspscCode
                    || actual.ExpenseObjectCode != datos.ExpenseObjectCode
                    || actual.AccountingAccountCode != datos.AccountingAccountCode
                    || (actual.ConsolidatorAreaSource != ConsolidatorAreaSources.Manual
                        && (actual.ConsolidatorAreaId != datos.ConsolidatorAreaId || actual.State != datos.State));

                if (cambio)
                {
                    idsACambiar.Add(actual.Id);
                    datosPorId[actual.Id] = datos;
                    updated++;
                }
                else unchanged++;
                continue;
            }

            // Los sinónimos vienen separados por coma y repiten variantes; se deduplican
            // por texto, que es la clave del índice único de alias.
            if (!string.IsNullOrWhiteSpace(synonyms))
            {
                var yaAgregados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var syn in synonyms.Split([',', '|', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var texto = syn.Length > 300 ? syn[..300] : syn;
                    if (!yaAgregados.Add(texto)) continue;

                    nuevosAliases.Add(new MasterProductAlias
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = orgId,
                        MasterProductId = datos.Id,
                        Kind = "Synonym",
                        Text = texto,
                        NormalizedText = ExcelTableReader.Normalize(texto)
                    });
                }
            }

            nuevos.Add(datos);
            added++;
        }

        await BulkInsertMasterProductsAsync(nuevos);
        await BulkInsertMasterProductAliasesAsync(nuevosAliases);

        // Solo se materializan las filas que cambiaron. La ratificación manual nunca se pisa.
        foreach (var lote in idsACambiar.Chunk(LoteEscritura))
        {
            var entidades = await _unitOfWork.Context.MasterProducts.Where(p => lote.Contains(p.Id)).ToListAsync();
            foreach (var p in entidades)
            {
                var d = datosPorId[p.Id];
                p.Name = d.Name;
                p.OriginalDescription = d.OriginalDescription;
                p.EnglishName = d.EnglishName;
                p.Definition = d.Definition;
                p.SearchText = d.SearchText;
                p.UnifiedCodeId = d.UnifiedCodeId;
                p.UnspscCode = d.UnspscCode;
                p.UnspscName = d.UnspscName;
                p.ItemType = d.ItemType;
                p.ItemSubtype = d.ItemSubtype;
                p.AssetTreatment = d.AssetTreatment;
                p.MeasureUnit = d.MeasureUnit;
                p.ReferencePrice = d.ReferencePrice;
                p.ExpenseObjectId = d.ExpenseObjectId;
                p.ExpenseObjectCode = d.ExpenseObjectCode;
                p.ExpenseObjectName = d.ExpenseObjectName;
                p.AccountingAccountId = d.AccountingAccountId;
                p.AccountingAccountCode = d.AccountingAccountCode;
                p.AccountingAccountName = d.AccountingAccountName;

                if (p.ConsolidatorAreaSource != ConsolidatorAreaSources.Manual)
                {
                    p.ConsolidatorAreaId = d.ConsolidatorAreaId;
                    p.ConsolidatorAreaName = d.ConsolidatorAreaName;
                    p.ConsolidatorAreaSource = d.ConsolidatorAreaSource;
                    p.State = d.State;
                }

                p.UpdatedAt = DateTime.UtcNow;
            }
            await _unitOfWork.Context.SaveChangesAsync();
            _unitOfWork.Context.ChangeTracker.Clear();
        }

        // Lo que desaparece del archivo se deshabilita, nunca se borra: puede estar
        // referenciado por un requerimiento vivo (criterio de aceptación de M8).
        var idsAusentes = existentes.Values
            .Where(p => !fileSourceIds.Contains(p.SourceId) && p.State != MasterProductStates.Disabled)
            .Select(p => p.Id)
            .ToList();

        var disabledCount = 0;
        foreach (var lote in idsAusentes.Chunk(LoteEscritura))
        {
            var entidades = await _unitOfWork.Context.MasterProducts.Where(p => lote.Contains(p.Id)).ToListAsync();
            foreach (var p in entidades)
            {
                p.State = MasterProductStates.Disabled;
                p.UpdatedAt = DateTime.UtcNow;
                disabledCount++;
            }
            await _unitOfWork.Context.SaveChangesAsync();
            _unitOfWork.Context.ChangeTracker.Clear();
        }

        return new SheetImportSummaryDto { SheetName = sheetName, TotalRows = table.Rows.Count, Added = added, Updated = updated, Unchanged = unchanged, Disabled = disabledCount };
    }

    private async Task BulkInsertMasterProductsAsync(List<MasterProduct> entidades)
    {
        if (entidades.Count == 0) return;

        var dbConnection = _unitOfWork.Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        var sqlConnection = (SqlConnection)dbConnection;
        var currentTx = _unitOfWork.Context.Database.CurrentTransaction;
        var sqlTransaction = currentTx != null ? (SqlTransaction)currentTx.GetDbTransaction() : null;

        using var bulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction)
        {
            DestinationTableName = "Planning.MasterProducts",
            BatchSize = 10000,
            BulkCopyTimeout = 600
        };

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("OrganizationId", typeof(int));
        table.Columns.Add("SourceId", typeof(Guid));
        table.Columns.Add("Code", typeof(string));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("OriginalDescription", typeof(string));
        table.Columns.Add("EnglishName", typeof(string));
        table.Columns.Add("Definition", typeof(string));
        table.Columns.Add("SearchText", typeof(string));
        table.Columns.Add("UnifiedCodeId", typeof(Guid));
        table.Columns.Add("UnspscCode", typeof(string));
        table.Columns.Add("UnspscName", typeof(string));
        table.Columns.Add("ItemType", typeof(string));
        table.Columns.Add("ItemSubtype", typeof(string));
        table.Columns.Add("AssetTreatment", typeof(string));
        table.Columns.Add("MeasureUnit", typeof(string));
        table.Columns.Add("ReferencePrice", typeof(decimal));
        table.Columns.Add("Currency", typeof(string));
        table.Columns.Add("ExpenseObjectId", typeof(Guid));
        table.Columns.Add("ExpenseObjectCode", typeof(string));
        table.Columns.Add("ExpenseObjectName", typeof(string));
        table.Columns.Add("AccountingAccountId", typeof(Guid));
        table.Columns.Add("AccountingAccountCode", typeof(string));
        table.Columns.Add("AccountingAccountName", typeof(string));
        table.Columns.Add("ConsolidatorAreaId", typeof(Guid));
        table.Columns.Add("ConsolidatorAreaName", typeof(string));
        table.Columns.Add("ConsolidatorAreaSource", typeof(string));
        table.Columns.Add("AreaAmbiguity", typeof(bool));
        table.Columns.Add("RequiresReview", typeof(bool));
        table.Columns.Add("State", typeof(string));
        table.Columns.Add("Version", typeof(int));
        table.Columns.Add("CreatedAt", typeof(DateTime));
        table.Columns.Add("UpdatedAt", typeof(DateTime));

        foreach (DataColumn col in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        foreach (var p in entidades)
        {
            table.Rows.Add(
                p.Id == Guid.Empty ? Guid.NewGuid() : p.Id,
                p.OrganizationId,
                (object?)p.SourceId ?? DBNull.Value,
                p.Code,
                p.Name,
                (object?)p.OriginalDescription ?? DBNull.Value,
                (object?)p.EnglishName ?? DBNull.Value,
                (object?)p.Definition ?? DBNull.Value,
                p.SearchText,
                (object?)p.UnifiedCodeId ?? DBNull.Value,
                (object?)p.UnspscCode ?? DBNull.Value,
                (object?)p.UnspscName ?? DBNull.Value,
                (object?)p.ItemType ?? DBNull.Value,
                (object?)p.ItemSubtype ?? DBNull.Value,
                (object?)p.AssetTreatment ?? DBNull.Value,
                (object?)p.MeasureUnit ?? DBNull.Value,
                (object?)p.ReferencePrice ?? DBNull.Value,
                p.Currency,
                (object?)p.ExpenseObjectId ?? DBNull.Value,
                (object?)p.ExpenseObjectCode ?? DBNull.Value,
                (object?)p.ExpenseObjectName ?? DBNull.Value,
                (object?)p.AccountingAccountId ?? DBNull.Value,
                (object?)p.AccountingAccountCode ?? DBNull.Value,
                (object?)p.AccountingAccountName ?? DBNull.Value,
                (object?)p.ConsolidatorAreaId ?? DBNull.Value,
                (object?)p.ConsolidatorAreaName ?? DBNull.Value,
                p.ConsolidatorAreaSource,
                p.AreaAmbiguity,
                p.RequiresReview,
                p.State,
                p.Version,
                p.CreatedAt,
                p.UpdatedAt
            );
        }

        await bulkCopy.WriteToServerAsync(table);
    }

    private async Task BulkInsertMasterProductAliasesAsync(List<MasterProductAlias> entidades)
    {
        if (entidades.Count == 0) return;

        var dbConnection = _unitOfWork.Context.Database.GetDbConnection();
        if (dbConnection.State != ConnectionState.Open)
        {
            await dbConnection.OpenAsync();
        }

        var sqlConnection = (SqlConnection)dbConnection;
        var currentTx = _unitOfWork.Context.Database.CurrentTransaction;
        var sqlTransaction = currentTx != null ? (SqlTransaction)currentTx.GetDbTransaction() : null;

        using var bulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, sqlTransaction)
        {
            DestinationTableName = "Planning.MasterProductAliases",
            BatchSize = 10000,
            BulkCopyTimeout = 600
        };

        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("OrganizationId", typeof(int));
        table.Columns.Add("MasterProductId", typeof(Guid));
        table.Columns.Add("Kind", typeof(string));
        table.Columns.Add("Text", typeof(string));
        table.Columns.Add("NormalizedText", typeof(string));

        foreach (DataColumn col in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        }

        foreach (var a in entidades)
        {
            table.Rows.Add(
                a.Id == Guid.Empty ? Guid.NewGuid() : a.Id,
                a.OrganizationId,
                a.MasterProductId,
                a.Kind,
                a.Text,
                a.NormalizedText
            );
        }

        await bulkCopy.WriteToServerAsync(table);
    }

    private static string? NormalizeSeparated(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var parts = text.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var normalizedParts = parts.Select(p => ExcelTableReader.Normalize(p)).Where(p => p.Length > 0);
        return string.Join("|", normalizedParts);
    }
}
