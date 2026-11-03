using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Excel;
using System.Globalization;
using System.Net;

namespace PDS.Service.Services.Planning;

public sealed class StrategicPlanExcelService : IStrategicPlanExcelService
{
    private const string SheetName = "PEI";
    private const string CatalogSheetName = "Catálogos";
    private const string ModuleName = "PEI";

    // Encabezados normalizados (ExcelTableReader.Normalize) de las columnas fijas.
    private const string ColFocus = "foco estrategico";
    private const string ColEndAlignment = "alineacion end";
    private const string ColObjectiveCode = "codigo objetivo";
    private const string ColObjective = "objetivo estrategico";
    private const string ColProduct = "producto estrategico";
    private const string ColInvolvedAreas = "areas involucradas";
    private const string ColUnit = "unidad de medida";
    private const string ColResponsibleArea = "area responsable";

    private readonly IUnitOfWork<PDSDbContext> _uow;
    private readonly IRepository<OrganizationalStructure> _structureRepository;
    private readonly IPlanningActivityService _activityService;
    private readonly IPlanningConfigService _configService;
    private readonly ILogger<StrategicPlanExcelService> _logger;

    public StrategicPlanExcelService(
        IUnitOfWork<PDSDbContext> uow,
        IRepository<OrganizationalStructure> structureRepository,
        IPlanningActivityService activityService,
        IPlanningConfigService configService,
        ILogger<StrategicPlanExcelService> logger)
    {
        _uow = uow;
        _structureRepository = structureRepository;
        _activityService = activityService;
        _configService = configService;
        _logger = logger;
    }

    private static int CurrentYear => DateTime.Now.Year;

    // ───────────────────────── Plantilla y exportación ─────────────────────────

    public async Task<IHttpResult<ExcelFileDto>> BuildTemplateAsync(int organizationId, Guid planId)
    {
        var plan = await LoadGraphAsync(organizationId, planId);
        if (plan is null)
            return Fail<ExcelFileDto>("La versión del PEI no existe", HttpStatusCode.NotFound);

        var areas = await GetAreaNamesAsync(organizationId);
        var bytes = BuildWorkbook(plan, areas, includeContent: false);

        return HttpResult<ExcelFileDto>.Success(new ExcelFileDto
        {
            Content = bytes,
            FileName = $"Plantilla PEI {plan.PeriodStartYear}-{plan.PeriodEndYear}.xlsx"
        });
    }

    public async Task<IHttpResult<ExcelFileDto>> ExportAsync(int organizationId, Guid planId)
    {
        var plan = await LoadGraphAsync(organizationId, planId);
        if (plan is null)
            return Fail<ExcelFileDto>("La versión del PEI no existe", HttpStatusCode.NotFound);

        var areas = await GetAreaNamesAsync(organizationId);
        var bytes = BuildWorkbook(plan, areas, includeContent: true);

        return HttpResult<ExcelFileDto>.Success(new ExcelFileDto
        {
            Content = bytes,
            FileName = $"PEI {plan.PeriodStartYear}-{plan.PeriodEndYear} v{plan.VersionNumber}.0.xlsx"
        });
    }

    private static byte[] BuildWorkbook(StrategicPlan plan, IReadOnlyList<string> areaNames, bool includeContent)
    {
        var years = PlanYears(plan);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SheetName);

        // Encabezados.
        var headers = new List<string>
        {
            "Foco estratégico", "Alineación END", "Código objetivo", "Objetivo estratégico",
            "Producto estratégico", "Unidad de medida", "Área responsable"
        };
        headers.AddRange(years.Select(y => $"Meta {y}"));
        headers.Add("Meta total");
        headers.AddRange([
            "Área involucrada 1 (Opcional)",
            "Área involucrada 2 (Opcional)",
            "Área involucrada 3 (Opcional)",
            "Área involucrada 4 (Opcional)",
            "Área involucrada 5 (Opcional)"
        ]);

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1F4E79"));
            cell.Style.Font.SetFontColor(XLColor.White);
        }

        // Hoja de catálogos (áreas del organigrama) con validación de datos.
        var catalog = workbook.Worksheets.Add(CatalogSheetName);
        catalog.Cell(1, 1).Value = "Áreas del organigrama";
        catalog.Cell(1, 1).Style.Font.SetBold();
        for (var i = 0; i < areaNames.Count; i++)
            catalog.Cell(i + 2, 1).Value = areaNames[i];
        catalog.Column(1).AdjustToContents();

        if (areaNames.Count > 0)
        {
            var catalogRange = catalog.Range(2, 1, areaNames.Count + 1, 1);
            var responsibleColumn = 7;
            ws.Range(2, responsibleColumn, 500, responsibleColumn)
                .CreateDataValidation()
                .List(catalogRange, true);

            var involvedStartCol = 7 + years.Count + 2; // Después de Meta total
            for (var i = 0; i < 5; i++)
            {
                var invCol = involvedStartCol + i;
                ws.Range(2, invCol, 500, invCol)
                    .CreateDataValidation()
                    .List(catalogRange, true);
            }
        }

        var row = 2;
        if (includeContent)
        {
            foreach (var focus in plan.Focuses.OrderBy(f => f.Order))
            {
                var focusFirstRow = true;
                foreach (var objective in focus.Objectives.OrderBy(o => o.Order))
                {
                    var objectiveFirstRow = true;
                    foreach (var product in objective.Products.OrderBy(p => p.Order))
                    {
                        var productFirstRow = true;
                        var units = product.MeasureUnits.OrderBy(u => u.Order).ToList();

                        // Producto sin unidades: igual se exporta (fila con unidad vacía).
                        if (units.Count == 0)
                        {
                            WriteRow(ws, row++, years, focus, objective, product, unit: null,
                                focusFirstRow, objectiveFirstRow, productFirstRow);
                            focusFirstRow = objectiveFirstRow = false;
                            continue;
                        }

                        foreach (var unit in units)
                        {
                            WriteRow(ws, row++, years, focus, objective, product, unit,
                                focusFirstRow, objectiveFirstRow, productFirstRow);
                            focusFirstRow = objectiveFirstRow = productFirstRow = false;
                        }
                    }
                }
            }
        }
        else
        {
            // Fila de ejemplo para guiar el llenado.
            var example = new List<object>
            {
                "1 - Ejemplo de foco estratégico", "END 2030 — Eje 1", "OE 1.1",
                "1.1 - Ejemplo de objetivo estratégico", "Ejemplo de producto estratégico",
                "% de implementación del producto", areaNames.FirstOrDefault() ?? "Área responsable"
            };
            for (var c = 0; c < example.Count; c++)
                ws.Cell(2, c + 1).Value = example[c].ToString();
            for (var y = 0; y < years.Count; y++)
                ws.Cell(2, 8 + y).Value = 0;

            ws.Cell(2, 8 + years.Count).Value = 0; // Meta total

            var firstArea = areaNames.FirstOrDefault() ?? "Área involucrada";
            for (var i = 0; i < 5; i++)
                ws.Cell(2, 9 + years.Count + i).Value = i == 0 ? firstArea : string.Empty;
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, headers.Count).AdjustToContents();
        foreach (var column in ws.Columns(1, headers.Count).Where(c => c.Width > 60))
            column.Width = 60;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteRow(
        IXLWorksheet ws, int row, IReadOnlyList<int> years,
        StrategicFocus focus, StrategicObjective objective, StrategicProduct product, StrategicMeasureUnit? unit,
        bool focusFirstRow, bool objectiveFirstRow, bool productFirstRow)
    {
        if (focusFirstRow)
        {
            ws.Cell(row, 1).Value = focus.Name;
            ws.Cell(row, 2).Value = focus.EndAlignment ?? string.Empty;
        }

        if (objectiveFirstRow)
        {
            ws.Cell(row, 3).Value = objective.Code;
            ws.Cell(row, 4).Value = objective.Name;
        }

        if (productFirstRow)
        {
            ws.Cell(row, 5).Value = product.Name;

            var involvedList = product.InvolvedAreas.Select(a => a.AreaName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            var invColStart = 8 + years.Count + 1; // Después de Meta total
            for (var i = 0; i < 5; i++)
            {
                ws.Cell(row, invColStart + i).Value = i < involvedList.Count ? involvedList[i] : string.Empty;
            }
        }

        if (unit is null)
            return;

        ws.Cell(row, 6).Value = unit.Name;
        ws.Cell(row, 7).Value = unit.ResponsibleAreaName ?? string.Empty;

        decimal total = 0;
        for (var y = 0; y < years.Count; y++)
        {
            var target = unit.AnnualTargets.FirstOrDefault(t => t.Year == years[y]);
            if (target is not null)
            {
                ws.Cell(row, 8 + y).Value = target.Value;
                total += target.Value;
            }
        }

        ws.Cell(row, 8 + years.Count).Value = total;
    }

    // ───────────────────────── Importación ─────────────────────────

    public async Task<IHttpResult<ImportResultDto>> ImportAsync(
        int organizationId, Guid planId, Stream file, string mode, bool dryRun, Guid userId, string? userName)
    {
        var replaceMode = string.Equals(mode, "reemplazar", StringComparison.OrdinalIgnoreCase);
        if (!replaceMode && !string.Equals(mode, "actualizar", StringComparison.OrdinalIgnoreCase))
            return Fail<ImportResultDto>("El modo de importación debe ser 'actualizar' o 'reemplazar'");

        var plan = await LoadGraphAsync(organizationId, planId, tracked: true);
        if (plan is null)
            return Fail<ImportResultDto>("La versión del PEI no existe", HttpStatusCode.NotFound);
        if (plan.Status != "Draft")
            return Fail<ImportResultDto>(
                "Solo la enmienda en elaboración admite importación", HttpStatusCode.Conflict);

        // Áreas del organigrama para resolver nombres → ids.
        var areasResult = await _structureRepository.FilterAsync(s => s.OrganizationId == organizationId);
        if (!areasResult.IsSuccess)
            return Fail<ImportResultDto>("No se pudo recuperar el organigrama");
        var areasByName = new Dictionary<string, OrganizationalStructure>();
        foreach (var area in areasResult.Value)
            areasByName.TryAdd(ExcelTableReader.Normalize(area.Name), area);

        // Fase 1: parsear y validar TODO. Cualquier error → no se aplica nada.
        ExcelTable table;
        try
        {
            table = ExcelTableReader.Read(file, SheetName);
        }
        catch (InvalidOperationException)
        {
            // Sin hoja "PEI": se intenta la primera hoja del archivo.
            file.Position = 0;
            try
            {
                table = ExcelTableReader.Read(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Archivo Excel ilegible al importar el PEI {PlanId}.", planId);
                return Fail<ImportResultDto>("El archivo no es un Excel válido (.xlsx)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Archivo Excel ilegible al importar el PEI {PlanId}.", planId);
            return Fail<ImportResultDto>("El archivo no es un Excel válido (.xlsx)");
        }

        var years = PlanYears(plan);
        var errors = new List<ImportErrorDto>();
        var warnings = new List<string>();

        // Columnas de años presentes: "meta {yyyy}".
        var yearColumns = new Dictionary<int, string>();
        foreach (var header in table.Headers)
        {
            if (header.StartsWith("meta ", StringComparison.Ordinal)
                && int.TryParse(header[5..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year)
                && years.Contains(year))
            {
                yearColumns.TryAdd(year, header);
            }
        }

        foreach (var required in new[] { ColFocus, ColObjective, ColProduct, ColUnit })
        {
            if (!table.Headers.Contains(required))
                errors.Add(new ImportErrorDto { Row = 1, Column = required, Message = $"Falta la columna «{required}» de la plantilla" });
        }

        if (yearColumns.Count == 0)
            errors.Add(new ImportErrorDto { Row = 1, Message = $"El archivo no tiene columnas de metas del período {plan.PeriodStartYear}-{plan.PeriodEndYear} (ej. «Meta {plan.PeriodStartYear}»)" });

        if (table.Rows.Count == 0)
            errors.Add(new ImportErrorDto { Row = 2, Message = "El archivo no tiene filas de datos" });

        if (errors.Count > 0)
            return HttpResult<ImportResultDto>.Success(new ImportResultDto { Applied = false, Errors = errors, Warnings = warnings });

        var parsed = ParseRows(table, yearColumns, areasByName, errors);

        // Fase de validación contra lo guardado: metas de años transcurridos de unidades
        // existentes deben coincidir (inmutabilidad).
        ValidateLockedTargets(plan, parsed, errors);

        if (errors.Count > 0 || dryRun)
        {
            var previewSummary = errors.Count > 0 ? null : ComputeSummary(plan, parsed, replaceMode, warnings);
            return HttpResult<ImportResultDto>.Success(new ImportResultDto
            {
                Applied = false,
                Errors = errors,
                Warnings = warnings,
                Summary = previewSummary
            });
        }

        // Fase 2: aplicar. Todos los cambios se escriben en un único SaveChanges, que ya es atómico (EF lo
        // ejecuta en su propia transacción) y la estrategia de reintentos lo reintenta completo; una
        // transacción manual fallaría con EnableRetryOnFailure.
        ImportSummaryDto summary;
        try
        {
            summary = Apply(plan, parsed, replaceMode, warnings);

            _uow.Context.StrategicPlanChanges.Add(new StrategicPlanChange
            {
                PlanId = plan.Id,
                UserId = userId,
                UserName = userName,
                ChangeType = "Edited",
                Level = "Product",
                Description = $"Importación Excel ({mode.ToLowerInvariant()}): {summary.Added} agregado(s), {summary.Updated} actualizado(s), {summary.Removed} eliminado(s)."
            });

            await _uow.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al aplicar la importación del PEI {PlanId}.", planId);
            return Fail<ImportResultDto>("No se pudo aplicar la importación");
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Importó el PEI desde Excel",
            $"Modo {mode.ToLowerInvariant()}: {summary.Added} agregado(s), {summary.Updated} actualizado(s), {summary.Removed} eliminado(s).",
            nameof(StrategicPlan), plan.Id);

        return HttpResult<ImportResultDto>.Success(new ImportResultDto
        {
            Applied = true,
            Errors = [],
            Warnings = warnings,
            Summary = summary
        });
    }

    // ───────────────────────── Parseo ─────────────────────────

    private sealed class ParsedUnit
    {
        public required int Row { get; init; }
        public required string Name { get; init; }
        public OrganizationalStructure? ResponsibleArea { get; init; }
        public required Dictionary<int, decimal> Targets { get; init; }
    }

    private sealed class ParsedProduct
    {
        public required int Row { get; init; }
        public required string Name { get; init; }
        public List<OrganizationalStructure> InvolvedAreas { get; } = [];
        public List<ParsedUnit> Units { get; } = [];
    }

    private sealed class ParsedObjective
    {
        public required int Row { get; init; }
        public required string Name { get; init; }
        public string? Code { get; init; }
        public List<ParsedProduct> Products { get; } = [];
    }

    private sealed class ParsedFocus
    {
        public required int Row { get; init; }
        public required string Name { get; init; }
        public string? EndAlignment { get; set; }
        public List<ParsedObjective> Objectives { get; } = [];
    }

    private List<ParsedFocus> ParseRows(
        ExcelTable table,
        Dictionary<int, string> yearColumns,
        Dictionary<string, OrganizationalStructure> areasByName,
        List<ImportErrorDto> errors)
    {
        var focuses = new List<ParsedFocus>();
        ParsedFocus? currentFocus = null;
        ParsedObjective? currentObjective = null;
        ParsedProduct? currentProduct = null;

        foreach (var row in table.Rows)
        {
            // Arrastre del último valor no vacío (el archivo institucional deja celdas en blanco).
            var focusName = row.Get(ColFocus).Trim();
            var objectiveName = row.Get(ColObjective).Trim();
            var productName = row.Get(ColProduct).Trim();
            var unitName = row.Get(ColUnit).Trim();

            if (focusName.Length > 0 && !SameNormalized(currentFocus?.Name, focusName))
            {
                currentFocus = focuses.FirstOrDefault(f => SameNormalized(f.Name, focusName));
                if (currentFocus is null)
                {
                    currentFocus = new ParsedFocus { Row = row.RowNumber, Name = focusName };
                    focuses.Add(currentFocus);
                }
                currentObjective = null;
                currentProduct = null;
            }

            if (currentFocus is null)
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Foco estratégico", Message = "La fila no tiene foco estratégico (ni se puede heredar de una fila anterior)" });
                continue;
            }

            var endAlignment = row.Get(ColEndAlignment).Trim();
            if (endAlignment.Length > 0)
                currentFocus.EndAlignment ??= endAlignment;

            if (objectiveName.Length > 0 && !SameNormalized(currentObjective?.Name, objectiveName))
            {
                currentObjective = currentFocus.Objectives.FirstOrDefault(o => SameNormalized(o.Name, objectiveName));
                if (currentObjective is null)
                {
                    currentObjective = new ParsedObjective
                    {
                        Row = row.RowNumber,
                        Name = objectiveName,
                        Code = row.Get(ColObjectiveCode).Trim() is { Length: > 0 } code ? code : null
                    };
                    currentFocus.Objectives.Add(currentObjective);
                }
                currentProduct = null;
            }

            if (currentObjective is null)
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Objetivo estratégico", Message = "La fila no tiene objetivo estratégico (ni se puede heredar de una fila anterior)" });
                continue;
            }

            if (productName.Length > 0 && !SameNormalized(currentProduct?.Name, productName))
            {
                currentProduct = currentObjective.Products.FirstOrDefault(p => SameNormalized(p.Name, productName));
                if (currentProduct is null)
                {
                    currentProduct = new ParsedProduct { Row = row.RowNumber, Name = productName };
                    currentObjective.Products.Add(currentProduct);

                    var involvedRawList = new List<string>();
                    for (var i = 1; i <= 5; i++)
                    {
                        var val = row.Get($"area involucrada {i}").Trim();
                        if (val.Length > 0)
                            involvedRawList.Add(val);
                    }
                    var legacyInvolved = row.Get(ColInvolvedAreas).Trim();
                    if (legacyInvolved.Length > 0)
                        involvedRawList.AddRange(SplitList(legacyInvolved));

                    foreach (var areaName in involvedRawList.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (areasByName.TryGetValue(ExcelTableReader.Normalize(areaName), out var area))
                        {
                            if (!currentProduct.InvolvedAreas.Any(a => a.Id == area.Id))
                                currentProduct.InvolvedAreas.Add(area);
                        }
                        else
                        {
                            errors.Add(new ImportErrorDto
                            {
                                Row = row.RowNumber,
                                Column = "Áreas involucradas",
                                Message = $"El área involucrada «{areaName}» no existe en el organigrama (o no está habilitada por el guardrail)"
                            });
                        }
                    }
                }
            }

            if (currentProduct is null)
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Producto estratégico", Message = "La fila no tiene producto estratégico (ni se puede heredar de una fila anterior)" });
                continue;
            }

            // Fila sin unidad: válida solo si define el producto (fila de encabezado de grupo).
            if (unitName.Length == 0)
                continue;

            if (currentProduct.Units.Any(u => SameNormalized(u.Name, unitName)))
            {
                errors.Add(new ImportErrorDto
                {
                    Row = row.RowNumber,
                    Column = "Unidad de medida",
                    Message = $"La unidad «{unitName}» está repetida para el producto «{currentProduct.Name}»"
                });
                continue;
            }

            OrganizationalStructure? responsible = null;
            var responsibleName = row.Get(ColResponsibleArea).Trim();
            if (responsibleName.Length > 0)
            {
                if (!areasByName.TryGetValue(ExcelTableReader.Normalize(responsibleName), out responsible))
                {
                    errors.Add(new ImportErrorDto
                    {
                        Row = row.RowNumber,
                        Column = "Área responsable",
                        Message = $"El área responsable «{responsibleName}» no existe en el organigrama"
                    });
                    continue;
                }
            }

            var targets = new Dictionary<int, decimal>();
            var rowValid = true;
            foreach (var (year, column) in yearColumns)
            {
                var raw = row.Get(column).Trim();
                if (raw.Length == 0)
                    continue;

                if (!TryParseDecimal(raw, out var value) || value < 0)
                {
                    errors.Add(new ImportErrorDto
                    {
                        Row = row.RowNumber,
                        Column = $"Meta {year}",
                        Message = $"El valor «{raw}» no es un número válido"
                    });
                    rowValid = false;
                    continue;
                }

                if (value > 0)
                    targets[year] = value;
            }

            if (!rowValid)
                continue;

            currentProduct.Units.Add(new ParsedUnit
            {
                Row = row.RowNumber,
                Name = unitName,
                ResponsibleArea = responsible,
                Targets = targets
            });
        }

        return focuses;
    }

    /// <summary>
    /// Inmutabilidad: para unidades EXISTENTES (match por nombres normalizados en la ruta
    /// foco→objetivo→producto→unidad), las metas de años ≤ año actual deben coincidir con lo
    /// guardado. Las unidades nuevas sí pueden traer valores de años pasados (carga inicial).
    /// </summary>
    private void ValidateLockedTargets(StrategicPlan plan, List<ParsedFocus> parsed, List<ImportErrorDto> errors)
    {
        foreach (var parsedFocus in parsed)
        {
            var focus = plan.Focuses.FirstOrDefault(f => SameNormalized(f.Name, parsedFocus.Name));
            if (focus is null) continue;

            foreach (var parsedObjective in parsedFocus.Objectives)
            {
                var objective = focus.Objectives.FirstOrDefault(o => SameNormalized(o.Name, parsedObjective.Name));
                if (objective is null) continue;

                foreach (var parsedProduct in parsedObjective.Products)
                {
                    var product = objective.Products.FirstOrDefault(p => SameNormalized(p.Name, parsedProduct.Name));
                    if (product is null) continue;

                    foreach (var parsedUnit in parsedProduct.Units)
                    {
                        var unit = product.MeasureUnits.FirstOrDefault(u => SameNormalized(u.Name, parsedUnit.Name));
                        if (unit is null) continue;

                        foreach (var target in unit.AnnualTargets.Where(t => t.Year <= CurrentYear))
                        {
                            parsedUnit.Targets.TryGetValue(target.Year, out var incoming);
                            if (incoming != target.Value)
                            {
                                errors.Add(new ImportErrorDto
                                {
                                    Row = parsedUnit.Row,
                                    Column = $"Meta {target.Year}",
                                    Message = $"La meta {target.Year} de «{unit.Name}» es historia inmutable (guardada: {target.Value}); no puede cambiarse por importación"
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    // ───────────────────────── Aplicación ─────────────────────────

    private ImportSummaryDto ComputeSummary(StrategicPlan plan, List<ParsedFocus> parsed, bool replaceMode, List<string> warnings)
        => Apply(plan, parsed, replaceMode, warnings, previewOnly: true);

    /// <summary>
    /// Aplica el árbol parseado sobre el Draft (o solo cuenta, en previewOnly). Match por
    /// nombres normalizados; conserva ids/lineage de lo existente para no romper referencias.
    /// </summary>
    private ImportSummaryDto Apply(StrategicPlan plan, List<ParsedFocus> parsed, bool replaceMode, List<string> warnings, bool previewOnly = false)
    {
        var ctx = _uow.Context;
        int added = 0, updated = 0, unchanged = 0, removed = 0;

        var matchedFocusIds = new HashSet<Guid>();
        var focusOrder = plan.Focuses.Count == 0 ? 0 : plan.Focuses.Max(f => f.Order);

        foreach (var parsedFocus in parsed)
        {
            var focus = plan.Focuses.FirstOrDefault(f => SameNormalized(f.Name, parsedFocus.Name));
            if (focus is null)
            {
                added++;
                if (previewOnly)
                {
                    added += parsedFocus.Objectives.Count
                        + parsedFocus.Objectives.Sum(o => o.Products.Count)
                        + parsedFocus.Objectives.Sum(o => o.Products.Sum(p => p.Units.Count));
                    continue;
                }

                focus = new StrategicFocus
                {
                    PlanId = plan.Id,
                    LineageId = Guid.NewGuid(),
                    Name = parsedFocus.Name,
                    EndAlignment = parsedFocus.EndAlignment ?? "Alineación END 2030 por definir",
                    Order = ++focusOrder
                };
                plan.Focuses.Add(focus);
                ctx.StrategicFocuses.Add(focus);
            }
            else
            {
                matchedFocusIds.Add(focus.Id);
                if (!previewOnly && parsedFocus.EndAlignment is not null && parsedFocus.EndAlignment != focus.EndAlignment)
                {
                    focus.EndAlignment = parsedFocus.EndAlignment;
                    updated++;
                }
                else
                {
                    unchanged++;
                }
            }

            ApplyObjectives(ctx, plan, focus, parsedFocus, replaceMode, warnings, previewOnly,
                ref added, ref updated, ref unchanged, ref removed);
        }

        if (replaceMode)
        {
            foreach (var focus in plan.Focuses.Where(f => f.Id != Guid.Empty && !matchedFocusIds.Contains(f.Id)).ToList())
            {
                if (focus.Objectives.Any(o => o.Products.Any(HasHistory)))
                {
                    warnings.Add($"El foco «{focus.Name}» no está en el archivo pero tiene historia; se conserva.");
                    continue;
                }

                removed++;
                if (!previewOnly)
                {
                    plan.Focuses.Remove(focus);
                    ctx.StrategicFocuses.Remove(focus);
                }
            }
        }

        return new ImportSummaryDto { Added = added, Updated = updated, Unchanged = unchanged, Removed = removed };
    }

    private static void ApplyObjectives(
        PDSDbContext ctx, StrategicPlan plan, StrategicFocus focus, ParsedFocus parsedFocus,
        bool replaceMode, List<string> warnings, bool previewOnly,
        ref int added, ref int updated, ref int unchanged, ref int removed)
    {
        var matchedObjectiveIds = new HashSet<Guid>();
        var objectiveOrder = focus.Objectives.Count == 0 ? 0 : focus.Objectives.Max(o => o.Order);
        var focusPosition = plan.Focuses.OrderBy(f => f.Order).ToList().FindIndex(f => ReferenceEquals(f, focus)) + 1;

        foreach (var parsedObjective in parsedFocus.Objectives)
        {
            var objective = focus.Objectives.FirstOrDefault(o => SameNormalized(o.Name, parsedObjective.Name));
            if (objective is null)
            {
                added++;
                if (previewOnly)
                {
                    added += parsedObjective.Products.Count + parsedObjective.Products.Sum(p => p.Units.Count);
                    continue;
                }

                objective = new StrategicObjective
                {
                    FocusId = focus.Id,
                    LineageId = Guid.NewGuid(),
                    Code = parsedObjective.Code ?? $"OE {focusPosition}.{focus.Objectives.Count + 1}",
                    Name = parsedObjective.Name,
                    Order = ++objectiveOrder
                };
                focus.Objectives.Add(objective);
                ctx.StrategicObjectives.Add(objective);
            }
            else
            {
                matchedObjectiveIds.Add(objective.Id);
                unchanged++;
            }

            ApplyProducts(ctx, objective, parsedObjective, replaceMode, warnings, previewOnly,
                ref added, ref updated, ref unchanged, ref removed);
        }

        if (replaceMode)
        {
            foreach (var objective in focus.Objectives.Where(o => o.Id != Guid.Empty && !matchedObjectiveIds.Contains(o.Id)).ToList())
            {
                if (objective.Products.Any(HasHistory))
                {
                    warnings.Add($"El objetivo «{objective.Name}» no está en el archivo pero tiene historia; se conserva.");
                    continue;
                }

                removed++;
                if (!previewOnly)
                {
                    focus.Objectives.Remove(objective);
                    ctx.StrategicObjectives.Remove(objective);
                }
            }
        }
    }

    private static void ApplyProducts(
        PDSDbContext ctx, StrategicObjective objective, ParsedObjective parsedObjective,
        bool replaceMode, List<string> warnings, bool previewOnly,
        ref int added, ref int updated, ref int unchanged, ref int removed)
    {
        var matchedProductIds = new HashSet<Guid>();
        var productOrder = objective.Products.Count == 0 ? 0 : objective.Products.Max(p => p.Order);

        foreach (var parsedProduct in parsedObjective.Products)
        {
            var product = objective.Products.FirstOrDefault(p => SameNormalized(p.Name, parsedProduct.Name));
            if (product is null)
            {
                added++;
                if (previewOnly)
                {
                    added += parsedProduct.Units.Count;
                    continue;
                }

                product = new StrategicProduct
                {
                    ObjectiveId = objective.Id,
                    LineageId = Guid.NewGuid(),
                    Name = parsedProduct.Name,
                    Order = ++productOrder
                };
                foreach (var area in parsedProduct.InvolvedAreas)
                {
                    product.InvolvedAreas.Add(new StrategicProductInvolvedArea
                    {
                        AreaId = area.Id,
                        AreaName = area.Name
                    });
                }
                objective.Products.Add(product);
                ctx.StrategicProducts.Add(product);
            }
            else
            {
                matchedProductIds.Add(product.Id);
                unchanged++;
            }

            ApplyUnits(ctx, product, parsedProduct, replaceMode, warnings, previewOnly,
                ref added, ref updated, ref unchanged, ref removed);
        }

        if (replaceMode)
        {
            foreach (var product in objective.Products.Where(p => p.Id != Guid.Empty && !matchedProductIds.Contains(p.Id)).ToList())
            {
                if (HasHistory(product))
                {
                    warnings.Add($"El producto «{product.Name}» no está en el archivo pero tiene historia; se conserva.");
                    continue;
                }

                removed++;
                if (!previewOnly)
                {
                    objective.Products.Remove(product);
                    ctx.StrategicProducts.Remove(product);
                }
            }
        }
    }

    private static void ApplyUnits(
        PDSDbContext ctx, StrategicProduct product, ParsedProduct parsedProduct,
        bool replaceMode, List<string> warnings, bool previewOnly,
        ref int added, ref int updated, ref int unchanged, ref int removed)
    {
        var matchedUnitIds = new HashSet<Guid>();
        var unitOrder = product.MeasureUnits.Count == 0 ? 0 : product.MeasureUnits.Max(u => u.Order);

        foreach (var parsedUnit in parsedProduct.Units)
        {
            var unit = product.MeasureUnits.FirstOrDefault(u => SameNormalized(u.Name, parsedUnit.Name));
            if (unit is null)
            {
                added++;
                if (previewOnly)
                    continue;

                unit = new StrategicMeasureUnit
                {
                    ProductId = product.Id,
                    LineageId = Guid.NewGuid(),
                    Name = parsedUnit.Name,
                    ResponsibleAreaId = parsedUnit.ResponsibleArea?.Id,
                    ResponsibleAreaName = parsedUnit.ResponsibleArea?.Name,
                    Order = ++unitOrder
                };
                foreach (var (year, value) in parsedUnit.Targets)
                    unit.AnnualTargets.Add(new StrategicAnnualTarget { Year = year, Value = value });

                product.MeasureUnits.Add(unit);
                ctx.StrategicMeasureUnits.Add(unit);
                continue;
            }

            matchedUnitIds.Add(unit.Id);

            var changed = false;

            if (parsedUnit.ResponsibleArea is not null && unit.ResponsibleAreaId != parsedUnit.ResponsibleArea.Id)
            {
                changed = true;
                if (!previewOnly)
                {
                    unit.ResponsibleAreaId = parsedUnit.ResponsibleArea.Id;
                    unit.ResponsibleAreaName = parsedUnit.ResponsibleArea.Name;
                }
            }

            // Solo metas de años futuros (las pasadas ya se validaron como idénticas).
            var currentYear = DateTime.Now.Year;
            foreach (var (year, value) in parsedUnit.Targets.Where(t => t.Key > currentYear))
            {
                var stored = unit.AnnualTargets.FirstOrDefault(t => t.Year == year);
                if (stored is null)
                {
                    changed = true;
                    if (!previewOnly)
                        unit.AnnualTargets.Add(new StrategicAnnualTarget { MeasureUnitId = unit.Id, Year = year, Value = value });
                }
                else if (stored.Value != value)
                {
                    changed = true;
                    if (!previewOnly)
                        stored.Value = value;
                }
            }

            foreach (var stale in unit.AnnualTargets.Where(t => t.Year > currentYear && !parsedUnit.Targets.ContainsKey(t.Year)).ToList())
            {
                changed = true;
                if (!previewOnly)
                    unit.AnnualTargets.Remove(stale);
            }

            if (changed) updated++; else unchanged++;
        }

        if (replaceMode)
        {
            foreach (var unit in product.MeasureUnits.Where(u => u.Id != Guid.Empty && !matchedUnitIds.Contains(u.Id)).ToList())
            {
                if (HasHistory(unit))
                {
                    warnings.Add($"La unidad «{unit.Name}» de «{product.Name}» no está en el archivo pero tiene historia; se conserva.");
                    continue;
                }

                removed++;
                if (!previewOnly)
                {
                    product.MeasureUnits.Remove(unit);
                    ctx.StrategicMeasureUnits.Remove(unit);
                }
            }
        }
    }

    // ───────────────────────── Helpers ─────────────────────────

    private async Task<StrategicPlan?> LoadGraphAsync(int organizationId, Guid planId, bool tracked = false)
    {
        IQueryable<StrategicPlan> query = _uow.Context.StrategicPlans
            .Include(p => p.Focuses).ThenInclude(f => f.Objectives)
                .ThenInclude(o => o.Products).ThenInclude(pr => pr.MeasureUnits)
                    .ThenInclude(u => u.AnnualTargets)
            .Include(p => p.Focuses).ThenInclude(f => f.Objectives)
                .ThenInclude(o => o.Products).ThenInclude(pr => pr.InvolvedAreas)
            .AsSplitQuery();

        if (!tracked)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
    }

    private async Task<IReadOnlyList<string>> GetAreaNamesAsync(int organizationId)
    {
        return await _configService.GetQualifyingAreaNamesAsync(organizationId);
    }

    private static List<int> PlanYears(StrategicPlan plan)
        => Enumerable.Range(plan.PeriodStartYear, Math.Max(1, plan.PeriodEndYear - plan.PeriodStartYear + 1)).ToList();

    private static bool SameNormalized(string? a, string? b)
        => ExcelTableReader.Normalize(a) == ExcelTableReader.Normalize(b);

    private static IEnumerable<string> SplitList(string raw)
        => raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("es-DO"), out value);
    }

    private static bool HasHistory(StrategicMeasureUnit unit)
        => unit.AnnualTargets.Any(t => t.Year <= DateTime.Now.Year && t.Value > 0);

    private static bool HasHistory(StrategicProduct product)
        => product.MeasureUnits.Any(HasHistory);

    private static HttpResult<T> Fail<T>(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<T>.Failure(message, detail, status);
    }
}
