using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Error;
using PDS.Domain.DTOs.Reporting;
using PDS.Domain.Enums;
using PDS.Domain.Response;
using PDS.Service.Services;
using PDS.Service.Services.Reporting;

namespace PDS.Controllers;

// RF-RE-01..06: reportes definicionales sobre la estructura publicada. Solo lectura.
[Authorize]
[ApiController]
[Route("reports")]
public class ReportsController : ApiControllerBase
{
    private readonly ILogger<ReportsController> _logger;
    private readonly IAuthorizedPostsReportService _authorizedPostsService;
    private readonly IStaffingComplementReportService _staffingComplementService;
    private readonly IEstimatedCostReportService _estimatedCostService;
    private readonly IComparativeReportService _comparativeService;
    private readonly IStructuralChangesReportService _structuralChangesService;
    private readonly IReportExportService _exportService;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];

    public ReportsController(
        ILogger<ReportsController> logger,
        IAuthorizedPostsReportService authorizedPostsService,
        IStaffingComplementReportService staffingComplementService,
        IEstimatedCostReportService estimatedCostService,
        IComparativeReportService comparativeService,
        IStructuralChangesReportService structuralChangesService,
        IReportExportService exportService)
    {
        _logger = logger;
        _authorizedPostsService = authorizedPostsService;
        _staffingComplementService = staffingComplementService;
        _estimatedCostService = estimatedCostService;
        _comparativeService = comparativeService;
        _structuralChangesService = structuralChangesService;
        _exportService = exportService;
    }

    // ───────────────────────── datos (JSON) ─────────────────────────

    [HttpGet("authorized-posts")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetAuthorizedPosts(
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate,
        [FromQuery] ReportGrouping groupBy = ReportGrouping.Unit)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _authorizedPostsService.GetAsync(organizationId, Context(versionId, asOfDate), groupBy, GetUserName());
        return Respond(result);
    }

    [HttpGet("staffing-complement")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetStaffingComplement(
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _staffingComplementService.GetAsync(organizationId, Context(versionId, asOfDate), GetUserName());
        return Respond(result);
    }

    [HttpGet("estimated-cost")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetEstimatedCost(
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate,
        [FromQuery] SalaryBasis basis = SalaryBasis.Midpoint,
        [FromQuery] ReportGrouping groupBy = ReportGrouping.Unit)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _estimatedCostService.GetAsync(organizationId, Context(versionId, asOfDate), basis, groupBy, GetUserName());
        return Respond(result);
    }

    [HttpGet("comparative")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetComparative(
        [FromQuery] Guid? fromVersionId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] Guid? toVersionId,
        [FromQuery] DateOnly? toDate,
        [FromQuery] SalaryBasis basis = SalaryBasis.Midpoint)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _comparativeService.GetAsync(
            organizationId, fromVersionId, fromDate, toVersionId, toDate, basis, GetUserName());
        return Respond(result);
    }

    [HttpGet("structural-changes")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetStructuralChanges([FromQuery] StructuralChangesFilterDto filter)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _structuralChangesService.GetAsync(organizationId, filter, GetUserName());
        return Respond(result);
    }

    // ───────────────────────── exportación (PDF / XLSX / CSV) ─────────────────────────

    [HttpGet("authorized-posts/export")]
    public async Task<ActionResult> ExportAuthorizedPosts(
        [FromQuery] string format,
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate,
        [FromQuery] ReportGrouping groupBy = ReportGrouping.Unit)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        if (!TryParseFormat(format, out var fmt))
            return InvalidFormat();

        var result = await _exportService.ExportAuthorizedPostsAsync(
            organizationId, Context(versionId, asOfDate), groupBy, fmt, GetUserName());
        return FileFrom(result);
    }

    [HttpGet("staffing-complement/export")]
    public async Task<ActionResult> ExportStaffingComplement(
        [FromQuery] string format,
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        if (!TryParseFormat(format, out var fmt))
            return InvalidFormat();

        var result = await _exportService.ExportStaffingComplementAsync(
            organizationId, Context(versionId, asOfDate), fmt, GetUserName());
        return FileFrom(result);
    }

    [HttpGet("estimated-cost/export")]
    public async Task<ActionResult> ExportEstimatedCost(
        [FromQuery] string format,
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate,
        [FromQuery] SalaryBasis basis = SalaryBasis.Midpoint,
        [FromQuery] ReportGrouping groupBy = ReportGrouping.Unit)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        if (!TryParseFormat(format, out var fmt))
            return InvalidFormat();

        var result = await _exportService.ExportEstimatedCostAsync(
            organizationId, Context(versionId, asOfDate), basis, groupBy, fmt, GetUserName());
        return FileFrom(result);
    }

    [HttpGet("comparative/export")]
    public async Task<ActionResult> ExportComparative(
        [FromQuery] string format,
        [FromQuery] Guid? fromVersionId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] Guid? toVersionId,
        [FromQuery] DateOnly? toDate,
        [FromQuery] SalaryBasis basis = SalaryBasis.Midpoint)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        if (!TryParseFormat(format, out var fmt))
            return InvalidFormat();

        var result = await _exportService.ExportComparativeAsync(
            organizationId, fromVersionId, fromDate, toVersionId, toDate, basis, fmt, GetUserName());
        return FileFrom(result);
    }

    [HttpGet("structural-changes/export")]
    public async Task<ActionResult> ExportStructuralChanges(
        [FromQuery] string format,
        [FromQuery] StructuralChangesFilterDto filter)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });
        if (!TryParseFormat(format, out var fmt))
            return InvalidFormat();

        var result = await _exportService.ExportStructuralChangesAsync(organizationId, filter, fmt, GetUserName());
        return FileFrom(result);
    }

    // ───────────────────────── helpers ─────────────────────────

    private static ReportingContextDto Context(Guid? versionId, DateOnly? asOfDate)
        => new() { VersionId = versionId, AsOfDate = asOfDate };

    private ActionResult Respond<T>(IHttpResult<T> result)
        => result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);

    private ActionResult FileFrom(IHttpResult<ReportExportFile> result)
    {
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        var file = result.Value;
        return File(file.Content, file.ContentType, file.FileName);
    }

    private static bool TryParseFormat(string? format, out ReportFormat result)
    {
        switch (format?.Trim().ToLowerInvariant())
        {
            case "pdf": result = ReportFormat.Pdf; return true;
            case "xlsx" or "excel": result = ReportFormat.Xlsx; return true;
            case "csv": result = ReportFormat.Csv; return true;
            default: result = default; return false;
        }
    }

    private BadRequestObjectResult InvalidFormat()
        => BadRequest(new ErrorDto("Formato de exportación inválido",
            new Dictionary<string, IEnumerable<string>> { { "format", ["Use pdf, xlsx o csv."] } }));
}
