using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDS.Application.Data;
using PDS.Domain.DTOs.Planning;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Service.Services.Excel;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace PDS.Service.Services.Planning;

public sealed partial class PoaExcelService : IPoaExcelService
{
    private const string SheetName = "POA";
    private const string CatalogSheetName = "Catálogos";
    private const string ModuleName = "Plan anual";
    private const string TaskSequenceScope = "POA_TASK";

    // Encabezados normalizados de las columnas fijas.
    private const string ColCode = "codigo";
    private const string ColOrigin = "origen del producto";
    private const string ColFocus = "foco estrategico";
    private const string ColObjective = "objetivo estrategico";
    private const string ColPeiProduct = "producto estrategico (pei)";
    private const string ColProduct = "producto";
    private const string ColJustification = "justificacion operativa";
    private const string ColUnit = "unidad de medida";
    private const string ColTarget = "meta anual";
    private const string ColTargetType = "tipo de medicion";
    private const string ColVerification = "medios de verificacion";
    private const string ColResponsible = "area responsable";
    private const string ColMunicipalities = "municipios de impacto";
    private const string ColInvolved = "areas involucradas";
    private const string ColTasks = "tareas";

    private static readonly string[] FixedHeaders =
    [
        "Código", "Origen del producto", "Foco estratégico", "Objetivo estratégico",
        "Producto estratégico (PEI)", "Producto", "Justificación operativa", "Unidad de medida",
        "Meta anual", "Tipo de medición", "T1", "T2", "T3", "T4", "Medios de verificación",
        "Área responsable", "Municipios de impacto",
        "Área involucrada 1 (Opcional)", "Área involucrada 2 (Opcional)", "Área involucrada 3 (Opcional)", "Área involucrada 4 (Opcional)", "Área involucrada 5 (Opcional)",
        "Tareas"
    ];

    private readonly IUnitOfWork<PDSDbContext> _unitOfWork;
    private readonly ISequenceService _sequenceService;
    private readonly IPlanningActivityService _activityService;
    private readonly IPlanningConfigService _configService;
    private readonly ILogger<PoaExcelService> _logger;

    public PoaExcelService(
        IUnitOfWork<PDSDbContext> unitOfWork,
        ISequenceService sequenceService,
        IPlanningActivityService activityService,
        IPlanningConfigService configService,
        ILogger<PoaExcelService> logger)
    {
        _unitOfWork = unitOfWork;
        _sequenceService = sequenceService;
        _activityService = activityService;
        _configService = configService;
        _logger = logger;
    }

    // ───────────────────────── Plantilla y exportación ─────────────────────────

    public async Task<IHttpResult<ExcelFileDto>> BuildTemplateAsync(int organizationId, Guid planId)
    {
        var data = await LoadPlanContextAsync(organizationId, planId, tracked: false);
        if (data is null)
            return Fail<ExcelFileDto>("El plan no existe", HttpStatusCode.NotFound);

        var areasByName = await AreasByNormalizedNameAsync(organizationId);
        var areaNames = areasByName.Values.Select(a => a.Name).Distinct().OrderBy(n => n).ToList();

        var bytes = BuildWorkbook([data.Value.Plan], data.Value.Columns, data.Value.Pei, includeContent: false, includeAreaColumn: false, areaNames: areaNames);
        return HttpResult<ExcelFileDto>.Success(new ExcelFileDto
        {
            Content = bytes,
            FileName = $"Plantilla POA {data.Value.Plan.Cycle!.Year} — {data.Value.Plan.AreaName}.xlsx"
        });
    }

    public async Task<IHttpResult<ExcelFileDto>> ExportPlanAsync(int organizationId, Guid planId)
    {
        var data = await LoadPlanContextAsync(organizationId, planId, tracked: false);
        if (data is null)
            return Fail<ExcelFileDto>("El plan no existe", HttpStatusCode.NotFound);

        var areasByName = await AreasByNormalizedNameAsync(organizationId);
        var areaNames = areasByName.Values.Select(a => a.Name).Distinct().OrderBy(n => n).ToList();

        var bytes = BuildWorkbook([data.Value.Plan], data.Value.Columns, data.Value.Pei, includeContent: true, includeAreaColumn: false, areaNames: areaNames);
        return HttpResult<ExcelFileDto>.Success(new ExcelFileDto
        {
            Content = bytes,
            FileName = $"POA {data.Value.Plan.Cycle!.Year} — {data.Value.Plan.AreaName}.xlsx"
        });
    }

    public async Task<IHttpResult<ExcelFileDto>> ExportCycleAsync(int organizationId, Guid cycleId)
    {
        var cycle = await _unitOfWork.Context.PlanningCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.OrganizationId == organizationId && c.Id == cycleId && c.DeletedAt == null);
        if (cycle is null)
            return Fail<ExcelFileDto>("El ciclo no existe", HttpStatusCode.NotFound);

        var plans = await PlanGraphQuery(false)
            .Where(p => p.OrganizationId == organizationId && p.CycleId == cycleId)
            .OrderBy(p => p.AreaName)
            .ToListAsync();

        var columns = await TemplateColumnsAsync(cycle.TemplateId);
        var pei = await LoadPeiGraphAsync(cycle.StrategicPlanId);

        var areasByName = await AreasByNormalizedNameAsync(organizationId);
        var areaNames = areasByName.Values.Select(a => a.Name).Distinct().OrderBy(n => n).ToList();

        var bytes = BuildWorkbook(plans, columns, pei, includeContent: true, includeAreaColumn: true, areaNames: areaNames);
        return HttpResult<ExcelFileDto>.Success(new ExcelFileDto
        {
            Content = bytes,
            FileName = $"POA institucional {cycle.Year}.xlsx"
        });
    }

    private static byte[] BuildWorkbook(
        IReadOnlyList<AnnualPlan> plans,
        IReadOnlyList<PlanningTemplateColumn> columns,
        StrategicPlan? pei,
        bool includeContent,
        bool includeAreaColumn,
        IReadOnlyList<string>? areaNames = null)
    {
        var customColumns = columns.Where(c => c.Visible).OrderBy(c => c.Order)
            .Where(c => c.Origin.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            .ToList();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(SheetName);

        var headers = new List<string>();
        if (includeAreaColumn) headers.Add("Área");
        headers.AddRange(FixedHeaders);
        headers.AddRange(customColumns.Select(c => c.VisibleName));

        for (var c = 0; c < headers.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.SetBold();
            cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1F4E79"));
            cell.Style.Font.SetFontColor(XLColor.White);
        }

        // Hoja de catálogos: guía de valores válidos para el llenado.
        var catalog = workbook.Worksheets.Add(CatalogSheetName);
        catalog.Cell(1, 1).Value = "Origen del producto";
        catalog.Cell(2, 1).Value = "PEI";
        catalog.Cell(3, 1).Value = "Operativo";
        catalog.Cell(1, 2).Value = "Tipo de medición";
        catalog.Cell(2, 2).Value = "absoluto";
        catalog.Cell(3, 2).Value = "porcentaje";
        catalog.Cell(1, 3).Value = "Focos del PEI";
        catalog.Cell(1, 4).Value = "Objetivos del PEI";
        catalog.Cell(1, 5).Value = "Productos estratégicos";
        if (pei is not null)
        {
            var focusRow = 2; var objectiveRow = 2; var productRow = 2;
            foreach (var focus in pei.Focuses.OrderBy(f => f.Order))
            {
                catalog.Cell(focusRow++, 3).Value = focus.Name;
                foreach (var objective in focus.Objectives.OrderBy(o => o.Order))
                {
                    catalog.Cell(objectiveRow++, 4).Value = objective.Name;
                    foreach (var product in objective.Products.OrderBy(p => p.Order))
                        catalog.Cell(productRow++, 5).Value = product.Name;
                }
            }
        }
        catalog.Cell(1, 6).Value = "Formato de tareas e Instrucciones";
        catalog.Cell(2, 6).Value = "Tareas: Nombre (meses: 1,2,3) | Nombre (continua) — separadas por |";
        catalog.Cell(3, 6).Value = "Origen: Los productos pueden ser vinculados al PEI o ser actividades operativas propias del área (Origen: Operativo, no necesariamente vinculados al PEI).";

        catalog.Cell(1, 7).Value = "Áreas del organigrama";
        var areaList = areaNames ?? [];
        if (areaList.Count > 0)
        {
            for (var i = 0; i < areaList.Count; i++)
            {
                catalog.Cell(i + 2, 7).Value = areaList[i];
            }
        }

        catalog.Row(1).Style.Font.SetBold();
        catalog.Columns(1, 7).AdjustToContents();

        // Data Validations (Dropdowns in Excel)
        var colOffset = includeAreaColumn ? 1 : 0;
        var originCol = 2 + colOffset;
        var typeCol = 10 + colOffset;
        var respCol = 16 + colOffset;
        var invColStart = 18 + colOffset;
        var invColEnd = 22 + colOffset;

        ws.Range(2, originCol, 500, originCol).CreateDataValidation().List(catalog.Range(2, 1, 3, 1));
        ws.Range(2, typeCol, 500, typeCol).CreateDataValidation().List(catalog.Range(2, 2, 3, 2));

        if (areaList.Count > 0)
        {
            var areaRange = catalog.Range(2, 7, areaList.Count + 1, 7);
            ws.Range(2, respCol, 500, respCol).CreateDataValidation().List(areaRange);
            for (var c = invColStart; c <= invColEnd; c++)
            {
                ws.Range(2, c, 500, c).CreateDataValidation().List(areaRange);
            }
        }

        var row = 2;
        if (includeContent)
        {
            foreach (var plan in plans)
            {
                foreach (var product in plan.Products.OrderBy(p => p.Order))
                {
                    var units = product.MeasureUnits.ToList();
                    if (units.Count == 0)
                        units.Add(null!); // Producto sin unidades: una fila con la parte de producto.

                    foreach (var unit in units)
                    {
                        var col = 1;
                        if (includeAreaColumn) ws.Cell(row, col++).Value = plan.AreaName;

                        ws.Cell(row, col++).Value = product.Code;
                        ws.Cell(row, col++).Value = product.Origin.Equals("Operational", StringComparison.OrdinalIgnoreCase) ? "Operativo" : "PEI";
                        ws.Cell(row, col++).Value = FindPeiName(pei, product.StrategicFocusId, PeiLevel.Focus);
                        ws.Cell(row, col++).Value = FindPeiName(pei, product.StrategicObjectiveId, PeiLevel.Objective);
                        ws.Cell(row, col++).Value = FindPeiName(pei, product.StrategicProductId, PeiLevel.Product);
                        ws.Cell(row, col++).Value = product.ProductName;
                        ws.Cell(row, col++).Value = product.OperationalJustification ?? string.Empty;

                        if (unit is not null)
                        {
                            ws.Cell(row, col++).Value = unit.Name;
                            ws.Cell(row, col++).Value = unit.TargetValue;
                            ws.Cell(row, col++).Value = unit.TargetType.Equals("Percentage", StringComparison.OrdinalIgnoreCase) ? "porcentaje" : "absoluto";
                            ws.Cell(row, col++).Value = unit.Q1;
                            ws.Cell(row, col++).Value = unit.Q2;
                            ws.Cell(row, col++).Value = unit.Q3;
                            ws.Cell(row, col++).Value = unit.Q4;
                            ws.Cell(row, col++).Value = unit.VerificationMeans ?? string.Empty;
                            ws.Cell(row, col++).Value = unit.ResponsibleAreaName ?? string.Empty;
                        }
                        else
                        {
                            col += 9;
                        }

                        ws.Cell(row, col++).Value = string.Join(" | ", product.Municipalities.Select(m => m.MunicipioName));

                        var invList = product.InvolvedAreas.Select(a => a.AreaName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                        for (var i = 0; i < 5; i++)
                        {
                            ws.Cell(row, col++).Value = i < invList.Count ? invList[i] : string.Empty;
                        }

                        ws.Cell(row, col++).Value = string.Join(" | ", product.Tasks.OrderBy(t => t.Code).Select(EncodeTask));

                        foreach (var custom in customColumns)
                        {
                            var value = product.CustomValues.FirstOrDefault(cv => cv.TemplateColumnId == custom.Id)?.Value;
                            ws.Cell(row, col++).Value = value ?? string.Empty;
                        }

                        row++;
                    }
                }
            }
        }
        else if (plans.Count == 1 && pei is not null)
        {
            var plan = plans[0];
            var cycleYear = plan.Cycle?.Year ?? DateTime.UtcNow.Year;

            if (plan.Products.Count > 0)
            {
                foreach (var product in plan.Products.OrderBy(p => p.Order))
                {
                    var units = product.MeasureUnits.ToList();
                    if (units.Count == 0) units.Add(null!);
                    foreach (var unit in units)
                    {
                        var col = 1;
                        if (includeAreaColumn) ws.Cell(row, col++).Value = plan.AreaName;
                        ws.Cell(row, col++).Value = product.Code;
                        ws.Cell(row, col++).Value = product.Origin.Equals("Operational", StringComparison.OrdinalIgnoreCase) ? "Operativo" : "PEI";
                        ws.Cell(row, col++).Value = FindPeiName(pei, product.StrategicFocusId, PeiLevel.Focus);
                        ws.Cell(row, col++).Value = FindPeiName(pei, product.StrategicObjectiveId, PeiLevel.Objective);
                        ws.Cell(row, col++).Value = FindPeiName(pei, product.StrategicProductId, PeiLevel.Product);
                        ws.Cell(row, col++).Value = product.ProductName;
                        ws.Cell(row, col++).Value = product.OperationalJustification ?? string.Empty;
                        if (unit is not null)
                        {
                            ws.Cell(row, col++).Value = unit.Name;
                            ws.Cell(row, col++).Value = unit.TargetValue;
                            ws.Cell(row, col++).Value = unit.TargetType.Equals("Percentage", StringComparison.OrdinalIgnoreCase) ? "porcentaje" : "absoluto";
                            ws.Cell(row, col++).Value = unit.Q1;
                            ws.Cell(row, col++).Value = unit.Q2;
                            ws.Cell(row, col++).Value = unit.Q3;
                            ws.Cell(row, col++).Value = unit.Q4;
                            ws.Cell(row, col++).Value = unit.VerificationMeans ?? string.Empty;
                            ws.Cell(row, col++).Value = unit.ResponsibleAreaName ?? string.Empty;
                        }
                        else
                        {
                            col += 9;
                        }
                        ws.Cell(row, col++).Value = string.Join(" | ", product.Municipalities.Select(m => m.MunicipioName));
                        var invList = product.InvolvedAreas.Select(a => a.AreaName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                        for (var i = 0; i < 5; i++) ws.Cell(row, col++).Value = i < invList.Count ? invList[i] : string.Empty;
                        ws.Cell(row, col++).Value = string.Join(" | ", product.Tasks.OrderBy(t => t.Code).Select(EncodeTask));
                        foreach (var custom in customColumns) ws.Cell(row, col++).Value = product.CustomValues.FirstOrDefault(cv => cv.TemplateColumnId == custom.Id)?.Value ?? string.Empty;
                        row++;
                    }
                }
            }
            else
            {
                var seq = 0;
                foreach (var focus in pei.Focuses.OrderBy(f => f.Order))
                {
                    foreach (var objective in focus.Objectives.OrderBy(o => o.Order))
                    {
                        foreach (var product in objective.Products.OrderBy(p => p.Order))
                        {
                            var matchesResp = product.MeasureUnits.Any(u => u.ResponsibleAreaId == plan.AreaId || string.Equals(u.ResponsibleAreaName, plan.AreaName, StringComparison.OrdinalIgnoreCase));
                            var matchesInv = product.InvolvedAreas.Any(ia => ia.AreaId == plan.AreaId || string.Equals(ia.AreaName, plan.AreaName, StringComparison.OrdinalIgnoreCase));

                            if (!matchesResp && !matchesInv) continue;

                            var units = product.MeasureUnits
                                .Where(u => u.ResponsibleAreaId == plan.AreaId || string.Equals(u.ResponsibleAreaName, plan.AreaName, StringComparison.OrdinalIgnoreCase) || matchesInv)
                                .ToList();

                            if (units.Count == 0) units.Add(null!);

                            foreach (var unit in units)
                            {
                                var col = 1;
                                if (includeAreaColumn) ws.Cell(row, col++).Value = plan.AreaName;

                                ws.Cell(row, col++).Value = $"EJ-{(++seq):D2}";
                                ws.Cell(row, col++).Value = "PEI";
                                ws.Cell(row, col++).Value = focus.Name;
                                ws.Cell(row, col++).Value = objective.Name;
                                ws.Cell(row, col++).Value = product.Name;
                                ws.Cell(row, col++).Value = product.Name;
                                ws.Cell(row, col++).Value = string.Empty;

                                if (unit is not null)
                                {
                                    var targetVal = unit.AnnualTargets.FirstOrDefault(t => t.Year == cycleYear)?.Value ?? 0;
                                    var isPct = unit.Name.Contains("porcentaje", StringComparison.OrdinalIgnoreCase) || unit.Name.Contains("%");

                                    ws.Cell(row, col++).Value = unit.Name;
                                    ws.Cell(row, col++).Value = targetVal;
                                    ws.Cell(row, col++).Value = isPct ? "porcentaje" : "absoluto";
                                    ws.Cell(row, col++).Value = 0;
                                    ws.Cell(row, col++).Value = 0;
                                    ws.Cell(row, col++).Value = 0;
                                    ws.Cell(row, col++).Value = 0;
                                    ws.Cell(row, col++).Value = string.Empty;
                                    ws.Cell(row, col++).Value = unit.ResponsibleAreaName ?? plan.AreaName;
                                }
                                else
                                {
                                    col += 9;
                                }

                                ws.Cell(row, col++).Value = string.Empty;

                                var invList = product.InvolvedAreas.Select(a => a.AreaName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
                                for (var i = 0; i < 5; i++)
                                {
                                    ws.Cell(row, col++).Value = i < invList.Count ? invList[i] : string.Empty;
                                }

                                ws.Cell(row, col++).Value = string.Empty;

                                foreach (var custom in customColumns)
                                {
                                    ws.Cell(row, col++).Value = string.Empty;
                                }

                                row++;
                            }
                        }
                    }
                }
            }
        }

        ws.SheetView.FreezeRows(1);
        ws.Columns(1, Math.Min(headers.Count, 12)).AdjustToContents();
        foreach (var column in ws.Columns(1, headers.Count).Where(c => c.Width > 55))
            column.Width = 55;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string EncodeTask(PoaTask task)
    {
        if (task.IsContinuous)
            return $"{task.Name} (continua)";
        var months = AnnualPlanService.ToMonths(task.MonthsMask).Select(m => m + 1).ToList();
        if (months.Count == 0)
            return task.Name;
        return $"{task.Name} (meses: {string.Join(",", months)})";
    }

    // ───────────────────────── Importación ─────────────────────────

    public async Task<IHttpResult<ImportResultDto>> ImportAsync(
        int organizationId, Guid planId, Stream file, string mode, bool dryRun, Guid userId, string? userName)
    {
        var replaceMode = string.Equals(mode, "reemplazar", StringComparison.OrdinalIgnoreCase);
        if (!replaceMode && !string.Equals(mode, "actualizar", StringComparison.OrdinalIgnoreCase))
            return Fail<ImportResultDto>("El modo de importación debe ser 'actualizar' o 'reemplazar'");

        var data = await LoadPlanContextAsync(organizationId, planId, tracked: true);
        if (data is null)
            return Fail<ImportResultDto>("El plan no existe", HttpStatusCode.NotFound);

        var (plan, columns, pei) = data.Value;
        if (string.Equals(plan.Cycle!.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            return Fail<ImportResultDto>("El ciclo está cerrado", HttpStatusCode.Conflict);
        if (!AnnualPlanStatuses.Editables.Contains(plan.Status))
            return Fail<ImportResultDto>($"El plan no es editable en estado «{plan.Status}»", HttpStatusCode.Conflict);

        ExcelTable table;
        try
        {
            table = ExcelTableReader.Read(file, SheetName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("excede") || ex.Message.Contains("máximo") || ex.Message.Contains("límite"))
        {
            return Fail<ImportResultDto>(ex.Message);
        }
        catch (InvalidOperationException)
        {
            file.Position = 0;
            try { table = ExcelTableReader.Read(file); }
            catch (InvalidOperationException ex2) when (ex2.Message.Contains("excede") || ex2.Message.Contains("máximo") || ex2.Message.Contains("límite"))
            {
                return Fail<ImportResultDto>(ex2.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Archivo Excel ilegible al importar el POA {PlanId}.", planId);
                return Fail<ImportResultDto>("El archivo no es un Excel válido (.xlsx)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Archivo Excel ilegible al importar el POA {PlanId}.", planId);
            return Fail<ImportResultDto>("El archivo no es un Excel válido (.xlsx)");
        }

        var errors = new List<ImportErrorDto>();
        var warnings = new List<string>();

        foreach (var required in new[] { ColCode, ColProduct, ColUnit, ColTarget })
        {
            if (!table.Headers.Contains(required))
                errors.Add(new ImportErrorDto { Row = 1, Column = required, Message = $"Falta la columna «{required}» de la plantilla" });
        }

        if (!table.Headers.Contains(ColTasks))
            warnings.Add("La columna 'Tareas/Meses' no fue incluida en el Excel. Se preservaron las tareas existentes.");
        if (!table.Headers.Contains(ColMunicipalities))
            warnings.Add("La columna 'Municipios' no fue incluida en el Excel. Se preservaron los municipios existentes.");
        if (!table.Headers.Contains(ColInvolved))
            warnings.Add("La columna 'Áreas involucradas' no fue incluida en el Excel. Se preservaron las áreas involucradas existentes.");

        if (table.Rows.Count == 0)
            errors.Add(new ImportErrorDto { Row = 2, Message = "El archivo no tiene filas de datos" });

        if (errors.Count > 0)
            return HttpResult<ImportResultDto>.Success(new ImportResultDto { Applied = false, Errors = errors, Warnings = warnings });

        // Catálogos para resolver nombres → ids.
        var areasByName = await AreasByNormalizedNameAsync(organizationId);
        var municipiosByName = await MunicipiosByNormalizedNameAsync();
        var customByName = columns
            .Where(c => c.Visible && c.Origin.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(c => ExcelTableReader.Normalize(c.VisibleName), c => c);

        var parsed = ParseRows(table, pei, areasByName, municipiosByName, customByName, errors, warnings);

        var fileCodes = parsed.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var summary = new ImportSummaryDto
        {
            Added = parsed.Count(p => !plan.Products.Any(x => x.Code.Equals(p.Code, StringComparison.OrdinalIgnoreCase))),
            Updated = parsed.Count(p => plan.Products.Any(x => x.Code.Equals(p.Code, StringComparison.OrdinalIgnoreCase))),
            Unchanged = replaceMode ? 0 : plan.Products.Count(x => !fileCodes.Contains(x.Code)),
            Removed = replaceMode ? plan.Products.Count(x => !fileCodes.Contains(x.Code)) : 0
        };

        if (errors.Count > 0 || dryRun)
        {
            return HttpResult<ImportResultDto>.Success(new ImportResultDto
            {
                Applied = false,
                Errors = errors,
                Warnings = warnings,
                Summary = errors.Count > 0 ? null : summary
            });
        }

        // Todos los cambios se escriben en un único SaveChanges, que ya es atómico (EF lo ejecuta en su
        // propia transacción) y la estrategia de reintentos lo reintenta completo; una transacción
        // manual fallaría con EnableRetryOnFailure.
        try
        {
            await ApplyAsync(organizationId, plan, parsed, table.Headers, replaceMode);

            if (plan.Status == AnnualPlanStatuses.SinIniciar)
                plan.Status = AnnualPlanStatuses.Borrador;
            plan.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al aplicar la importación del POA {PlanId}.", planId);
            return Fail<ImportResultDto>("No se pudo aplicar la importación");
        }

        await _activityService.LogAsync(organizationId, userId, userName, ModuleName,
            "Importó el plan desde Excel",
            $"«{plan.Name}» (modo {mode.ToLowerInvariant()}): {summary.Added} agregado(s), {summary.Updated} actualizado(s), {summary.Removed} eliminado(s).",
            nameof(AnnualPlan), plan.Id);

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
        public required string Name { get; init; }
        public decimal TargetValue { get; init; }
        public required string TargetType { get; init; }
        public string? VerificationMeans { get; init; }
        public OrganizationalStructure? ResponsibleArea { get; init; }
        public decimal Q1 { get; init; }
        public decimal Q2 { get; init; }
        public decimal Q3 { get; init; }
        public decimal Q4 { get; init; }
    }

    private sealed class ParsedTask
    {
        public required string Name { get; init; }
        public bool IsContinuous { get; init; }
        public required IReadOnlyCollection<int> Months { get; init; }
    }

    private sealed class ParsedProduct
    {
        public required int Row { get; init; }
        public required string Code { get; init; }
        public required string Origin { get; init; }
        public StrategicFocus? Focus { get; init; }
        public StrategicObjective? Objective { get; init; }
        public StrategicProduct? PeiProduct { get; init; }
        public required string Name { get; set; }
        public string? Justification { get; init; }
        public List<OrganizationalStructure> InvolvedAreas { get; } = [];
        public List<Municipio> Municipalities { get; } = [];
        public List<ParsedUnit> Units { get; } = [];
        public List<ParsedTask> Tasks { get; } = [];
        public Dictionary<Guid, string> CustomValues { get; } = [];
    }

    private List<ParsedProduct> ParseRows(
        ExcelTable table,
        StrategicPlan? pei,
        Dictionary<string, OrganizationalStructure> areasByName,
        Dictionary<string, Municipio> municipiosByName,
        Dictionary<string, PlanningTemplateColumn> customByName,
        List<ImportErrorDto> errors,
        List<string> warnings)
    {
        var products = new List<ParsedProduct>();

        foreach (var row in table.Rows)
        {
            var code = row.Get(ColCode).Trim();
            if (code.Length == 0)
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Código", Message = "La fila no tiene código de producto" });
                continue;
            }

            var existing = products.FirstOrDefault(p => p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = ParseProductRow(row, code, pei, areasByName, municipiosByName, customByName, errors);
                if (existing is null)
                    continue;
                products.Add(existing);
            }
            else
            {
                var name = row.Get(ColProduct).Trim();
                if (name.Length > 0 && !string.Equals(ExcelTableReader.Normalize(name), ExcelTableReader.Normalize(existing.Name)))
                    warnings.Add($"Fila {row.RowNumber}: el producto «{code}» tiene nombres distintos entre filas; se usa el primero.");
            }

            // Una fila por unidad de medida.
            var unitName = row.Get(ColUnit).Trim();
            if (unitName.Length == 0)
                continue;

            if (existing.Units.Any(u => string.Equals(ExcelTableReader.Normalize(u.Name), ExcelTableReader.Normalize(unitName))))
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Unidad de medida", Message = $"La unidad «{unitName}» está repetida para el producto «{code}»" });
                continue;
            }

            var targetTypeRaw = ExcelTableReader.Normalize(row.Get(ColTargetType));
            if (targetTypeRaw.Length > 0 && targetTypeRaw != "absoluto" && targetTypeRaw != "porcentaje")
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Tipo de medición", Message = $"Tipo de medición inválido: «{row.Get(ColTargetType)}» (use absoluto o porcentaje)" });
                continue;
            }

            OrganizationalStructure? responsible = null;
            var responsibleName = row.Get(ColResponsible).Trim();
            if (responsibleName.Length > 0
                && !areasByName.TryGetValue(ExcelTableReader.Normalize(responsibleName), out responsible))
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Área responsable", Message = $"El área responsable «{responsibleName}» no existe en el organigrama" });
                continue;
            }

            if (!TryParseDecimalCell(row, ColTarget, errors, out var target)
                || !TryParseDecimalCell(row, "t1", errors, out var q1)
                || !TryParseDecimalCell(row, "t2", errors, out var q2)
                || !TryParseDecimalCell(row, "t3", errors, out var q3)
                || !TryParseDecimalCell(row, "t4", errors, out var q4))
            {
                continue;
            }

            existing.Units.Add(new ParsedUnit
            {
                Name = unitName,
                TargetValue = target,
                TargetType = targetTypeRaw == "porcentaje" ? "Percentage" : "Absolute",
                VerificationMeans = row.Get(ColVerification).Trim() is { Length: > 0 } vm ? vm : null,
                ResponsibleArea = responsible,
                Q1 = q1, Q2 = q2, Q3 = q3, Q4 = q4
            });
        }

        return products;
    }

    private ParsedProduct? ParseProductRow(
        ExcelRow row,
        string code,
        StrategicPlan? pei,
        Dictionary<string, OrganizationalStructure> areasByName,
        Dictionary<string, Municipio> municipiosByName,
        Dictionary<string, PlanningTemplateColumn> customByName,
        List<ImportErrorDto> errors)
    {
        var originRaw = ExcelTableReader.Normalize(row.Get(ColOrigin));
        var isOperational = originRaw is "operativo" or "operational";
        var name = row.Get(ColProduct).Trim();
        if (name.Length == 0)
        {
            errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Producto", Message = $"El producto «{code}» no tiene nombre" });
            return null;
        }

        StrategicFocus? focus = null;
        StrategicObjective? objective = null;
        StrategicProduct? peiProduct = null;

        if (!isOperational)
        {
            var focusName = row.Get(ColFocus).Trim();
            var objectiveName = row.Get(ColObjective).Trim();

            if (focusName.Length > 0)
            {
                focus = pei?.Focuses.FirstOrDefault(f => SameNormalized(f.Name, focusName));
                if (focus is null)
                {
                    errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Foco estratégico", Message = $"El foco «{focusName}» no existe en el PEI del ciclo" });
                    return null;
                }
            }

            if (objectiveName.Length > 0)
            {
                objective = focus?.Objectives.FirstOrDefault(o => SameNormalized(o.Name, objectiveName))
                    ?? pei?.Focuses.SelectMany(f => f.Objectives).FirstOrDefault(o => SameNormalized(o.Name, objectiveName));
                if (objective is null)
                {
                    errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Objetivo estratégico", Message = $"El objetivo «{objectiveName}» no existe en el foco indicado" });
                    return null;
                }
                if (focus is not null && objective.FocusId != focus.Id)
                {
                    errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Objetivo estratégico", Message = $"El objetivo «{objectiveName}» no pertenece al foco «{focus.Name}»" });
                    return null;
                }
            }

            var peiProductName = row.Get(ColPeiProduct).Trim();
            if (peiProductName.Length > 0)
            {
                peiProduct = objective?.Products.FirstOrDefault(p => SameNormalized(p.Name, peiProductName));
                if (peiProduct is null)
                {
                    errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Producto estratégico (PEI)", Message = $"El producto estratégico «{peiProductName}» no pertenece al objetivo indicado" });
                    return null;
                }
            }
        }

        var justification = row.Get(ColJustification).Trim();
        if (isOperational && justification.Length == 0)
        {
            errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Justificación operativa", Message = $"El producto operativo «{code}» requiere justificación" });
            return null;
        }

        var parsed = new ParsedProduct
        {
            Row = row.RowNumber,
            Code = code,
            Origin = isOperational ? "Operational" : "Pei",
            Focus = focus,
            Objective = objective,
            PeiProduct = peiProduct,
            Name = name,
            Justification = justification.Length > 0 ? justification : null
        };

        var valid = true;

        foreach (var municipioName in SplitList(row.Get(ColMunicipalities)))
        {
            if (municipiosByName.TryGetValue(ExcelTableReader.Normalize(municipioName), out var municipio))
            {
                if (!parsed.Municipalities.Any(m => m.Id == municipio.Id))
                    parsed.Municipalities.Add(municipio);
            }
            else
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Municipios de impacto", Message = $"El municipio «{municipioName}» no existe en el catálogo territorial" });
                valid = false;
            }
        }

        var involvedRawList = new List<string>();
        for (var i = 1; i <= 5; i++)
        {
            var val = row.Get($"area involucrada {i}").Trim();
            if (val.Length > 0)
                involvedRawList.Add(val);
        }
        var legacyInvolved = row.Get(ColInvolved).Trim();
        if (legacyInvolved.Length > 0)
            involvedRawList.AddRange(SplitList(legacyInvolved));

        foreach (var areaName in involvedRawList.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (areasByName.TryGetValue(ExcelTableReader.Normalize(areaName), out var area))
            {
                if (!parsed.InvolvedAreas.Any(a => a.Id == area.Id))
                    parsed.InvolvedAreas.Add(area);
            }
            else
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Áreas involucradas", Message = $"El área involucrada «{areaName}» no existe en el organigrama (o no está habilitada por el guardrail)" });
                valid = false;
            }
        }

        foreach (var rawTask in SplitList(row.Get(ColTasks)))
        {
            var task = ParseTask(rawTask);
            if (task is null)
            {
                errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = "Tareas", Message = $"Tarea inválida: «{rawTask}» (meses fuera de 1-12)" });
                valid = false;
                continue;
            }
            parsed.Tasks.Add(task);
        }

        foreach (var (normalizedName, column) in customByName)
        {
            if (table_HasHeader(row, normalizedName))
            {
                var value = row.Get(normalizedName).Trim();
                if (value.Length > 0)
                    parsed.CustomValues[column.Id] = value;
            }
        }

        return valid ? parsed : null;
    }

    private static bool table_HasHeader(ExcelRow row, string normalizedHeader)
        => row.Cells.ContainsKey(normalizedHeader);

    [GeneratedRegex(@"^(?<name>.+?)\s*\((?:(?<continua>continua)|meses:\s*(?<months>[\d,\s]*))\)\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex TaskPattern();

    private static ParsedTask? ParseTask(string raw)
    {
        var match = TaskPattern().Match(raw);
        if (!match.Success)
            return new ParsedTask { Name = raw.Trim(), IsContinuous = false, Months = [] };

        var name = match.Groups["name"].Value.Trim();
        if (match.Groups["continua"].Success)
            return new ParsedTask { Name = name, IsContinuous = true, Months = [] };

        var months = new List<int>();
        foreach (var part in match.Groups["months"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var month) || month is < 1 or > 12)
                return null;
            months.Add(month - 1);
        }

        return new ParsedTask { Name = name, IsContinuous = false, Months = months };
    }

    private static bool TryParseDecimalCell(ExcelRow row, string column, List<ImportErrorDto> errors, out decimal value)
    {
        value = 0;
        var raw = row.Get(column).Trim();
        if (raw.Length == 0)
            return true;

        if (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
            || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.GetCultureInfo("es-DO"), out value))
        {
            if (value >= 0)
                return true;
        }

        errors.Add(new ImportErrorDto { Row = row.RowNumber, Column = column.ToUpperInvariant(), Message = $"El valor «{raw}» no es un número válido" });
        return false;
    }

    // ───────────────────────── Aplicación ─────────────────────────

    private async Task ApplyAsync(int organizationId, AnnualPlan plan, List<ParsedProduct> parsed, IReadOnlyList<string> headers, bool replaceMode)
    {
        var ctx = _unitOfWork.Context;
        var fileCodes = parsed.Select(p => p.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var hasOrigin = headers.Contains(ColOrigin);
        var hasJustification = headers.Contains(ColJustification);
        var hasFocus = headers.Contains(ColFocus);
        var hasObjective = headers.Contains(ColObjective);
        var hasPeiProduct = headers.Contains(ColPeiProduct);
        var hasMunicipalities = headers.Contains(ColMunicipalities);
        var hasInvolvedAreas = headers.Contains(ColInvolved);
        var hasTasks = headers.Contains(ColTasks);

        if (replaceMode)
        {
            foreach (var removed in plan.Products.Where(p => !fileCodes.Contains(p.Code)).ToList())
            {
                plan.Products.Remove(removed);
                ctx.PoaProducts.Remove(removed);
            }
        }

        var order = plan.Products.Count == 0 ? 0 : plan.Products.Max(p => p.Order);

        foreach (var parsedProduct in parsed)
        {
            var product = plan.Products.FirstOrDefault(p => p.Code.Equals(parsedProduct.Code, StringComparison.OrdinalIgnoreCase));
            var isNew = product is null;
            if (isNew)
            {
                product = new PoaProduct
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    Code = parsedProduct.Code.ToUpperInvariant(),
                    Origin = parsedProduct.Origin,
                    ProductName = parsedProduct.Name,
                    Order = ++order
                };
                plan.Products.Add(product);
                ctx.PoaProducts.Add(product);
            }

            product.ProductName = parsedProduct.Name;
            if (isNew || hasOrigin) product.Origin = parsedProduct.Origin;
            if (isNew || hasJustification) product.OperationalJustification = parsedProduct.Justification;
            if (isNew || hasFocus) product.StrategicFocusId = parsedProduct.Focus?.Id;
            if (isNew || hasObjective) product.StrategicObjectiveId = parsedProduct.Objective?.Id;
            if (isNew || hasPeiProduct) product.StrategicProductId = parsedProduct.PeiProduct?.Id;
            product.UpdatedAt = DateTime.UtcNow;

            // Unidades: match por nombre normalizado (conserva ids).
            var keptUnitIds = new HashSet<Guid>();
            foreach (var parsedUnit in parsedProduct.Units)
            {
                var unit = product.MeasureUnits.FirstOrDefault(u => SameNormalized(u.Name, parsedUnit.Name));
                if (unit is null)
                {
                    unit = new PoaMeasureUnit { ProductId = product.Id, Name = parsedUnit.Name, TargetType = "Absolute" };
                    product.MeasureUnits.Add(unit);
                    ctx.PoaMeasureUnits.Add(unit);
                }
                if (unit.Id != Guid.Empty)
                    keptUnitIds.Add(unit.Id);

                unit.Name = parsedUnit.Name;
                unit.TargetValue = parsedUnit.TargetValue;
                unit.TargetType = parsedUnit.TargetType;
                unit.VerificationMeans = parsedUnit.VerificationMeans;
                unit.ResponsibleAreaId = parsedUnit.ResponsibleArea?.Id;
                unit.ResponsibleAreaName = parsedUnit.ResponsibleArea?.Name;
                unit.Q1 = parsedUnit.Q1;
                unit.Q2 = parsedUnit.Q2;
                unit.Q3 = parsedUnit.Q3;
                unit.Q4 = parsedUnit.Q4;
            }

            foreach (var removed in product.MeasureUnits
                         .Where(u => u.Id != Guid.Empty && !keptUnitIds.Contains(u.Id)
                             && !parsedProduct.Units.Any(x => SameNormalized(x.Name, u.Name)))
                         .ToList())
            {
                product.MeasureUnits.Remove(removed);
                ctx.PoaMeasureUnits.Remove(removed);
            }

            // Tareas: match por nombre normalizado (conserva ids — protege enlaces de requerimientos, M7).
            if (isNew || hasTasks)
            {
                var keptTaskIds = new HashSet<Guid>();
                foreach (var parsedTask in parsedProduct.Tasks)
                {
                    var task = product.Tasks.FirstOrDefault(t => SameNormalized(t.Name, parsedTask.Name));
                    if (task is null)
                    {
                        var number = await _sequenceService.NextAsync(organizationId, TaskSequenceScope, product.Id.ToString());
                        task = new PoaTask { ProductId = product.Id, Code = $"TR-{number:D2}", Name = parsedTask.Name };
                        product.Tasks.Add(task);
                        ctx.PoaTasks.Add(task);
                    }
                    if (task.Id != Guid.Empty)
                        keptTaskIds.Add(task.Id);

                    task.Name = parsedTask.Name;
                    task.IsContinuous = parsedTask.IsContinuous;
                    task.MonthsMask = ToMask(parsedTask.Months);
                }

                foreach (var removed in product.Tasks
                             .Where(t => t.Id != Guid.Empty && !keptTaskIds.Contains(t.Id)
                                 && !parsedProduct.Tasks.Any(x => SameNormalized(x.Name, t.Name)))
                             .ToList())
                {
                    product.Tasks.Remove(removed);
                    ctx.PoaTasks.Remove(removed);
                }
            }

            // Municipios: reemplazo solo si vino la columna en el Excel.
            if (isNew || hasMunicipalities)
            {
                foreach (var removed in product.Municipalities
                             .Where(m => !parsedProduct.Municipalities.Any(x => x.Id == m.MunicipioId)).ToList())
                {
                    product.Municipalities.Remove(removed);
                    ctx.PoaProductMunicipalities.Remove(removed);
                }
                foreach (var municipio in parsedProduct.Municipalities)
                {
                    if (!product.Municipalities.Any(m => m.MunicipioId == municipio.Id))
                    {
                        var link = new PoaProductMunicipality { ProductId = product.Id, MunicipioId = municipio.Id, MunicipioName = municipio.Nombre };
                        product.Municipalities.Add(link);
                        ctx.PoaProductMunicipalities.Add(link);
                    }
                }
            }

            // Áreas involucradas: reemplazo solo si vino la columna en el Excel.
            if (isNew || hasInvolvedAreas)
            {
                foreach (var removed in product.InvolvedAreas
                             .Where(a => !parsedProduct.InvolvedAreas.Any(x => x.Id == a.AreaId)).ToList())
                {
                    product.InvolvedAreas.Remove(removed);
                    ctx.PoaProductInvolvedAreas.Remove(removed);
                }
                foreach (var area in parsedProduct.InvolvedAreas)
                {
                    if (!product.InvolvedAreas.Any(a => a.AreaId == area.Id))
                    {
                        var link = new PoaProductInvolvedArea { ProductId = product.Id, AreaId = area.Id, AreaName = area.Name };
                        product.InvolvedAreas.Add(link);
                        ctx.PoaProductInvolvedAreas.Add(link);
                    }
                }
            }

            // Columnas personalizadas: solo se tocan las presentes en el archivo.
            foreach (var (columnId, value) in parsedProduct.CustomValues)
            {
                var existing = product.CustomValues.FirstOrDefault(cv => cv.TemplateColumnId == columnId);
                if (existing is null)
                {
                    var customValue = new PoaProductCustomValue { ProductId = product.Id, TemplateColumnId = columnId, Value = value };
                    product.CustomValues.Add(customValue);
                    ctx.PoaProductCustomValues.Add(customValue);
                }
                else
                {
                    existing.Value = value;
                }
            }
        }
    }

    // ───────────────────────── Helpers ─────────────────────────

    private async Task<(AnnualPlan Plan, List<PlanningTemplateColumn> Columns, StrategicPlan? Pei)?> LoadPlanContextAsync(
        int organizationId, Guid planId, bool tracked)
    {
        var plan = await PlanGraphQuery(tracked)
            .FirstOrDefaultAsync(p => p.OrganizationId == organizationId && p.Id == planId);
        if (plan is null)
            return null;

        var columns = await TemplateColumnsAsync(plan.Cycle!.TemplateId);
        var pei = await LoadPeiGraphAsync(plan.Cycle.StrategicPlanId);
        return (plan, columns, pei);
    }

    private IQueryable<AnnualPlan> PlanGraphQuery(bool tracked)
    {
        IQueryable<AnnualPlan> query = _unitOfWork.Context.AnnualPlans
            .Include(p => p.Cycle)
            .Include(p => p.Products).ThenInclude(pr => pr.MeasureUnits)
            .Include(p => p.Products).ThenInclude(pr => pr.Tasks)
            .Include(p => p.Products).ThenInclude(pr => pr.Municipalities)
            .Include(p => p.Products).ThenInclude(pr => pr.InvolvedAreas)
            .Include(p => p.Products).ThenInclude(pr => pr.CustomValues)
            .AsSplitQuery();

        return tracked ? query : query.AsNoTracking();
    }

    private async Task<StrategicPlan?> LoadPeiGraphAsync(Guid strategicPlanId)
        => await _unitOfWork.Context.StrategicPlans
            .AsNoTracking()
            .Include(p => p.Focuses)
                .ThenInclude(f => f.Objectives)
                .ThenInclude(o => o.Products)
                    .ThenInclude(pr => pr.MeasureUnits)
                        .ThenInclude(mu => mu.AnnualTargets)
            .Include(p => p.Focuses)
                .ThenInclude(f => f.Objectives)
                .ThenInclude(o => o.Products)
                    .ThenInclude(pr => pr.InvolvedAreas)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == strategicPlanId);

    private async Task<List<PlanningTemplateColumn>> TemplateColumnsAsync(Guid templateId)
        => await _unitOfWork.Context.PlanningTemplateColumns
            .AsNoTracking()
            .Where(c => c.TemplateId == templateId && c.DeletedAt == null)
            .OrderBy(c => c.Order)
            .ToListAsync();

    private async Task<Dictionary<string, OrganizationalStructure>> AreasByNormalizedNameAsync(int organizationId)
    {
        var areas = await _unitOfWork.Context.OrganizationalStructures
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId && s.DeletedAt == null)
            .ToListAsync();

        var areaMap = areas.ToDictionary(a => a.Id);
        var maxLevel = await _configService.GetMaxLevelAsync(organizationId);

        var byName = new Dictionary<string, OrganizationalStructure>();
        foreach (var area in areas)
        {
            if (PlanningConfigService.CalculateDepth(area, areaMap) <= maxLevel)
            {
                byName.TryAdd(ExcelTableReader.Normalize(area.Name), area);
            }
        }
        return byName;
    }

    private async Task<Dictionary<string, Municipio>> MunicipiosByNormalizedNameAsync()
    {
        var municipios = await _unitOfWork.Context.Municipios.AsNoTracking().ToListAsync();
        var byName = new Dictionary<string, Municipio>();
        foreach (var municipio in municipios)
            byName.TryAdd(ExcelTableReader.Normalize(municipio.Nombre), municipio);
        return byName;
    }

    private enum PeiLevel { Focus, Objective, Product }

    private static string FindPeiName(StrategicPlan? pei, Guid? id, PeiLevel level)
    {
        if (pei is null || id is null)
            return string.Empty;

        return level switch
        {
            PeiLevel.Focus => pei.Focuses.FirstOrDefault(f => f.Id == id)?.Name ?? string.Empty,
            PeiLevel.Objective => pei.Focuses.SelectMany(f => f.Objectives).FirstOrDefault(o => o.Id == id)?.Name ?? string.Empty,
            _ => pei.Focuses.SelectMany(f => f.Objectives).SelectMany(o => o.Products).FirstOrDefault(p => p.Id == id)?.Name ?? string.Empty
        };
    }

    private static short ToMask(IReadOnlyCollection<int> months)
    {
        short mask = 0;
        foreach (var month in months.Where(m => m is >= 0 and <= 11))
            mask |= (short)(1 << month);
        return mask;
    }

    private static bool SameNormalized(string? a, string? b)
        => ExcelTableReader.Normalize(a) == ExcelTableReader.Normalize(b);

    private static IEnumerable<string> SplitList(string raw)
        => raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static HttpResult<T> Fail<T>(string message, HttpStatusCode status = HttpStatusCode.BadRequest)
    {
        var detail = new Dictionary<string, IEnumerable<string>> { { "errors", [message] } };
        return HttpResult<T>.Failure(message, detail, status);
    }
}
