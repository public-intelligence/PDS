using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;
using System.Globalization;
using System.Net;

namespace PDS.Service.Services.Reporting;

// RF-RE-06: arma el ReportTable de cada reporte (misma agregación que la pantalla) y delega en el
// renderer del formato solicitado. Construye el nombre de archivo {reporte}_{contexto}_{fecha-hora}.{ext}.
public class ReportExportService(
    ILogger<ReportExportService> logger,
    IAuthorizedPostsReportService authorizedPostsService,
    IStaffingComplementReportService staffingComplementService,
    IEstimatedCostReportService estimatedCostService,
    IComparativeReportService comparativeService,
    IStructuralChangesReportService structuralChangesService,
    IEnumerable<IReportRenderer> renderers
) : IReportExportService
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-DO");

    private readonly ILogger<ReportExportService> _logger = logger;
    private readonly IAuthorizedPostsReportService _authorizedPostsService = authorizedPostsService;
    private readonly IStaffingComplementReportService _staffingComplementService = staffingComplementService;
    private readonly IEstimatedCostReportService _estimatedCostService = estimatedCostService;
    private readonly IComparativeReportService _comparativeService = comparativeService;
    private readonly IStructuralChangesReportService _structuralChangesService = structuralChangesService;
    private readonly IReadOnlyList<IReportRenderer> _renderers = renderers.ToList();

    public async Task<IHttpResult<ReportExportFile>> ExportAuthorizedPostsAsync(
        int organizationId, ReportingContextDto context, ReportGrouping groupBy, ReportFormat format, string? generatedByName)
    {
        var result = await _authorizedPostsService.GetAsync(organizationId, context, groupBy, generatedByName);
        if (!result.IsSuccess) return Propagate(result);
        return Build(BuildAuthorizedPostsTable(result.Value), format, "plazas-autorizadas", result.Value.Context);
    }

    public async Task<IHttpResult<ReportExportFile>> ExportStaffingComplementAsync(
        int organizationId, ReportingContextDto context, ReportFormat format, string? generatedByName)
    {
        var result = await _staffingComplementService.GetAsync(organizationId, context, generatedByName);
        if (!result.IsSuccess) return Propagate(result);
        return Build(BuildStaffingComplementTable(result.Value), format, "dotacion", result.Value.Context);
    }

    public async Task<IHttpResult<ReportExportFile>> ExportEstimatedCostAsync(
        int organizationId, ReportingContextDto context, SalaryBasis basis, ReportGrouping groupBy, ReportFormat format, string? generatedByName)
    {
        var result = await _estimatedCostService.GetAsync(organizationId, context, basis, groupBy, generatedByName);
        if (!result.IsSuccess) return Propagate(result);
        return Build(BuildEstimatedCostTable(result.Value), format, "costo-estimado", result.Value.Context);
    }

    public async Task<IHttpResult<ReportExportFile>> ExportComparativeAsync(
        int organizationId, Guid? fromVersionId, DateOnly? fromDate, Guid? toVersionId, DateOnly? toDate,
        SalaryBasis basis, ReportFormat format, string? generatedByName)
    {
        var result = await _comparativeService.GetAsync(
            organizationId, fromVersionId, fromDate, toVersionId, toDate, basis, generatedByName);
        if (!result.IsSuccess) return Propagate(result);
        return Build(BuildComparativeTable(result.Value), format, "comparativo", result.Value.ToContext);
    }

    public async Task<IHttpResult<ReportExportFile>> ExportStructuralChangesAsync(
        int organizationId, StructuralChangesFilterDto filter, ReportFormat format, string? generatedByName)
    {
        var result = await _structuralChangesService.GetAsync(organizationId, filter, generatedByName);
        if (!result.IsSuccess) return Propagate(result);
        return Build(BuildStructuralChangesTable(result.Value), format, "cambios-estructurales", null,
            $"{filter.From:yyyyMMdd}-{filter.To:yyyyMMdd}");
    }

    // ───────────────────────── render + nombre de archivo ─────────────────────────

    private IHttpResult<ReportExportFile> Build(
        ReportTable table, ReportFormat format, string reportKey, ReportContextLabelDto? context, string? contextSuffix = null)
    {
        var renderer = _renderers.FirstOrDefault(r => r.Format == format);
        if (renderer is null)
            return Fail($"Formato de exportación no soportado: {format}.");

        var rendered = renderer.Render(table);
        var suffix = contextSuffix ?? ContextSuffix(context);
        var stamp = (context?.GeneratedAtUtc ?? table.Context.GeneratedAtUtc).ToString("yyyyMMdd-HHmm");
        var fileName = $"{reportKey}_{suffix}_{stamp}.{rendered.FileExtension}";

        return HttpResult<ReportExportFile>.Success(new ReportExportFile
        {
            Content = rendered.Content,
            ContentType = rendered.ContentType,
            FileName = fileName
        });
    }

    private static string ContextSuffix(ReportContextLabelDto? c)
    {
        if (c is null) return "contexto";
        return c.VersionNumber.HasValue ? $"v{c.VersionNumber}" : $"al-{c.ResolvedAsOf:yyyyMMdd}";
    }

    // ───────────────────────── proyección DTO → ReportTable ─────────────────────────

    private static ReportTable BuildAuthorizedPostsTable(AuthorizedPostsReportDto dto)
    {
        var columns = Columns(
            ("Código", false), ("Unidad", false), ("Tipo de área", false), ("Nivel", true),
            ("Cargo", false), ("Grupo ocupacional", false), ("Grado", true), ("Plazas autorizadas", true));

        var rows = new List<ReportTableRow>();
        foreach (var subtotal in dto.Subtotals)
        {
            foreach (var r in dto.Rows.Where(r => r.GroupKey == subtotal.GroupKey))
                rows.Add(Row(
                    r.UnitCode, r.UnitName, r.AreaTypeName, Num(r.Level),
                    r.PositionName, r.OccupationalGroupName, Num(r.Grade), Num(r.AuthorizedPosts)));

            rows.Add(Subtotal(
                $"Subtotal: {subtotal.GroupLabel}", "", "", "",
                $"{Num(subtotal.DistinctPositionCount)} cargos", "", "", Num(subtotal.AuthorizedPosts)));
        }
        rows.Add(GrandTotal(
            "Total general", "", "", "",
            $"{Num(dto.GrandTotal.DistinctPositionCount)} cargos", "", "", Num(dto.GrandTotal.AuthorizedPosts)));

        return new ReportTable
        {
            Title = "Reporte de plazas autorizadas",
            Context = dto.Context,
            Columns = columns,
            Rows = rows,
            Note = "Plazas autorizadas (definición), no ocupación real. La ocupación está fuera del alcance de PDS por diseño."
        };
    }

    private static ReportTable BuildStaffingComplementTable(StaffingComplementReportDto dto)
    {
        var columns = Columns(
            ("Código", false), ("Unidad", false), ("Tipo de área", false), ("Nivel", true),
            ("Cargos propios", true), ("Plazas propias", true),
            ("Cargos consolidados", true), ("Plazas consolidadas", true), ("Supervisor definido", false));

        var rows = new List<ReportTableRow>();
        foreach (var u in dto.Units)
            rows.Add(Row(
                u.Code, u.Name, u.AreaTypeName, Num(u.Level),
                Num(u.OwnDistinctPositions), Num(u.OwnAuthorizedPosts),
                Num(u.ConsolidatedDistinctPositions), Num(u.ConsolidatedAuthorizedPosts),
                u.HasSupervisorDefined ? "Sí" : "No"));

        foreach (var lvl in dto.ByLevel)
            rows.Add(Subtotal(
                $"Subtotal {lvl.GroupLabel}", "", "", "",
                Num(lvl.DistinctPositionCount), Num(lvl.AuthorizedPosts), "", "", ""));

        rows.Add(GrandTotal(
            "Total general", "", "", "",
            Num(dto.GrandTotal.DistinctPositionCount), Num(dto.GrandTotal.AuthorizedPosts), "", "", ""));

        return new ReportTable
        {
            Title = "Reporte de dotación definida (plantilla)",
            Context = dto.Context,
            Columns = columns,
            Rows = rows,
            Note = "Dotación DEFINIDA (cargos y plazas autorizadas). No representa ocupación ni personal real."
        };
    }

    private static ReportTable BuildEstimatedCostTable(EstimatedCostReportDto dto)
    {
        var columns = Columns(
            ("Código", false), ("Unidad", false), ("Nivel", true), ("Cargo", false),
            ("Grupo ocupacional", false), ("Grado", true), ("Plazas", true),
            ("Salario unitario (estimado)", true), ("Costo estimado", true));

        var rows = new List<ReportTableRow>();
        foreach (var subtotal in dto.Subtotals)
        {
            foreach (var r in dto.Rows.Where(r => r.GroupKey == subtotal.GroupKey))
                rows.Add(Row(
                    r.UnitCode, r.UnitName, Num(r.Level), r.PositionName,
                    r.OccupationalGroupName, Num(r.Grade), Num(r.AuthorizedPosts),
                    Money(r.UnitSalary), Money(r.LineCost)));

            rows.Add(Subtotal(
                $"Subtotal: {subtotal.GroupLabel}", "", "", "", "", "",
                Num(subtotal.AuthorizedPosts), "", Money(subtotal.EstimatedCost)));
        }
        rows.Add(GrandTotal(
            "Total general (estimado)", "", "", "", "", "",
            Num(dto.GrandTotal.AuthorizedPosts), "", Money(dto.GrandTotal.EstimatedCost)));

        var note = $"{dto.HistoricalFidelityNote} Base salarial: {BasisLabel(dto.Basis)}." +
                   (dto.UnresolvedPositionCount > 0
                       ? $" Cargos sin salario utilizable (excluidos del costo): {dto.UnresolvedPositionCount}."
                       : string.Empty);

        return new ReportTable
        {
            Title = "Reporte de costo estimado de la plantilla",
            Context = dto.Context,
            Columns = columns,
            Rows = rows,
            Note = note
        };
    }

    private static ReportTable BuildComparativeTable(ComparativeReportDto dto)
    {
        var columns = Columns(
            ("Categoría", false), ("Elemento", false), ("Antes", true), ("Después", true), ("Cambio", true));

        var s = dto.UnitComparison.Summary;
        var p = dto.PositionDeltas;
        var c = dto.CostDelta;

        var rows = new List<ReportTableRow>
        {
            Row("Resumen unidades", "Creadas", "", "", Num(s.CreatedCount)),
            Row("Resumen unidades", "Modificadas", "", "", Num(s.ModifiedCount)),
            Row("Resumen unidades", "Reubicadas", "", "", Num(s.RelocatedCount)),
            Row("Resumen unidades", "Eliminadas", "", "", Num(s.DeletedCount)),
            Row("Resumen cargos", "Agregados", "", "", Num(p.AddedCount)),
            Row("Resumen cargos", "Removidos", "", "", Num(p.RemovedCount)),
            Row("Resumen cargos", "Plazas modificadas", "", "", Num(p.PostCountChangedCount)),
            Subtotal("Plazas autorizadas", "Total", Num(p.FromTotalAuthorizedPosts), Num(p.ToTotalAuthorizedPosts), Num(p.AuthorizedPostsDelta)),
            Subtotal("Costo estimado", $"Total ({BasisLabel(c.Basis)})", Money(c.FromEstimatedCost), Money(c.ToEstimatedCost), Money(c.CostDelta))
        };

        foreach (var u in dto.UnitComparison.UnitChanges)
            rows.Add(Row(
                "Unidad",
                $"{u.After?.Code ?? u.Before?.Code} — {u.After?.Name ?? u.Before?.Name}",
                u.Before?.Name, u.After?.Name, string.Join(", ", u.ChangeType)));

        foreach (var d in dto.PositionDeltas.Changes)
            rows.Add(Row(
                "Cargo", d.PositionName, Num(d.FromAuthorizedPosts), Num(d.ToAuthorizedPosts),
                $"{Signed(d.AuthorizedPostsDelta)} ({ChangeLabel(d.ChangeType)})"));

        return new ReportTable
        {
            Title = $"Reporte comparativo: {ContextShort(dto.FromContext)} → {ContextShort(dto.ToContext)}",
            Context = dto.ToContext,
            Columns = columns,
            Rows = rows,
            Note = "Variaciones entre dos contextos. El costo es estimado (salario × plazas autorizadas)."
        };
    }

    private static ReportTable BuildStructuralChangesTable(StructuralChangesReportDto dto)
    {
        var columns = Columns(
            ("Versión", true), ("Nombre", false), ("Estado", false), ("Vigencia desde", false),
            ("Instrumento legal", false), ("Aprobó", false), ("Publicó", false),
            ("Unidades (C/M/R/E)", false), ("Cargos (+/−/Δ)", false));

        var rows = dto.Entries.Select(e =>
        {
            var u = e.UnitChangeSummary;
            var p = e.PositionDeltaSummary;
            var unitCell = u is null ? "—" : $"{u.CreatedCount}/{u.ModifiedCount}/{u.RelocatedCount}/{u.DeletedCount}";
            var posCell = p is null ? "—" : $"+{p.AddedCount}/−{p.RemovedCount}/{Signed(p.AuthorizedPostsDelta)}";
            var legal = e.LegalInstrument is null ? "—" : $"{e.LegalInstrument.Type} {e.LegalInstrument.Number}";
            return Row(
                e.Number?.ToString(Culture) ?? "—", e.Name, e.Status.ToString(),
                e.EffectiveFrom?.ToString("yyyy-MM-dd") ?? "—", legal,
                e.ApprovedByName ?? "—", e.PublishedByName ?? "—", unitCell, posCell);
        }).ToList();

        // Etiqueta de contexto sintética para el período (no es una versión única).
        var context = new ReportContextLabelDto
        {
            VersionId = Guid.Empty,
            VersionName = $"Período {dto.From:yyyy-MM-dd} a {dto.To:yyyy-MM-dd}",
            Status = VersionStatus.Published,
            ResolvedAsOf = dto.To,
            GeneratedAtUtc = DateTime.UtcNow
        };

        return new ReportTable
        {
            Title = "Reporte de cambios estructurales aprobados",
            Context = context,
            Columns = columns,
            Rows = rows,
            Note = "Cada versión publicada en el período = un cambio estructural aprobado (RF-FA-01)."
        };
    }

    // ───────────────────────── helpers de formato ─────────────────────────

    private static IReadOnlyList<ReportColumn> Columns(params (string Header, bool IsNumeric)[] cols)
        => cols.Select(c => new ReportColumn { Header = c.Header, IsNumeric = c.IsNumeric }).ToList();

    private static ReportTableRow Row(params string?[] cells)
        => new() { Cells = cells };

    private static ReportTableRow Subtotal(params string?[] cells)
        => new() { Cells = cells, IsSubtotal = true };

    private static ReportTableRow GrandTotal(params string?[] cells)
        => new() { Cells = cells, IsGrandTotal = true };

    private static string Num(int? value) => value?.ToString("N0", Culture) ?? string.Empty;

    private static string Signed(int value) => (value > 0 ? "+" : string.Empty) + value.ToString("N0", Culture);

    private static string Money(decimal? value)
        => value.HasValue ? $"RD$ {value.Value.ToString("N2", Culture)}" : "—";

    private static string BasisLabel(SalaryBasis basis) => basis switch
    {
        SalaryBasis.Min => "mínimo del rango",
        SalaryBasis.Max => "máximo del rango",
        _ => "punto medio del rango"
    };

    private static string ChangeLabel(string changeType) => changeType switch
    {
        "Added" => "Agregado",
        "Removed" => "Removido",
        "PostsChanged" => "Plazas modificadas",
        _ => changeType
    };

    private static string ContextShort(ReportContextLabelDto c)
        => c.VersionNumber.HasValue ? $"v{c.VersionNumber}" : $"{c.ResolvedAsOf:yyyy-MM-dd}";

    private static IHttpResult<ReportExportFile> Propagate<T>(IHttpResult<T> source)
        => HttpResult<ReportExportFile>.Failure(source.Error.Title, source.Error.Detail, source.StatusCode);

    private static HttpResult<ReportExportFile> Fail(string message, HttpStatusCode code = HttpStatusCode.BadRequest)
        => HttpResult<ReportExportFile>.Failure("Error al exportar el reporte",
            new Dictionary<string, IEnumerable<string>> { { "errors", [message] } }, code);
}
