using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System.Net;

namespace PDS.Controllers;

/// <summary>
/// Planes anuales (POA) por ciclo (M5) + Excel (M6). La lectura se limita a las áreas del
/// usuario salvo que tenga planning:poa:review o planning:admin. Editar exige
/// planning:poa:edit Y pertenecer al área dueña del plan; devolver/aprobar exigen
/// planning:poa:review. Ciclo cerrado → 409.
/// </summary>
[Authorize]
[ApiController]
[Route("annual-plans")]
[Produces("application/json")]
public class AnnualPlansController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IAnnualPlanService _planService;
    private readonly IPoaExcelService _excelService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnnualPlansController> _logger;

    public AnnualPlansController(
        IAnnualPlanService planService,
        IPoaExcelService excelService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard,
        IConfiguration configuration,
        ILogger<AnnualPlansController> logger)
    {
        _planService = planService;
        _excelService = excelService;
        _contextService = contextService;
        _accessGuard = accessGuard;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Interruptor de rodaje: con el filtro apagado solo se registra qué se ocultaría, para
    /// detectar roles mal configurados en GOB-ID antes de cortar el acceso de nadie.
    /// </summary>
    private bool EnforceAreaFiltering => _configuration.GetValue<bool>("Authorization:EnforceAreaFiltering", false);

    // ───────────────────────── Lectura ─────────────────────────

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AnnualPlanDto>>> List(
        [FromQuery] Guid? cycleId, [FromQuery] Guid? areaId, [FromQuery] string? status, CancellationToken ct = default)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _planService.ListAsync(organizationId, cycleId, areaId, status);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        var plans = result.Value;

        // Planificación (poa:review) y admin ven todos los planes; un enlace ve los de sus áreas.
        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (contextResult.IsSuccess && contextResult.Value is not null)
        {
            var context = contextResult.Value;
            var seesAll = _accessGuard.HasPermission(context, PlanningPermissions.PoaReview);

            if (!seesAll)
            {
                var own = plans.Where(p => context.EligibleAreaIds.Contains(p.AreaId) || context.PlanAreaId == p.AreaId).ToList();

                if (!EnforceAreaFiltering)
                {
                    // Fase de rodaje: se registra lo que se ocultaría, pero no se oculta todavía.
                    var hidden = plans.Count() - own.Count;
                    if (hidden > 0)
                        _logger.LogInformation(
                            "[DRY-RUN] Se ocultarían {Hidden} plan(es) al usuario {UserId} por filtro de área.", hidden, userId);
                }
                else
                {
                    plans = own;
                }
            }
        }

        return Ok(plans);
    }

    [HttpGet("{id:guid}/workspace")]
    public async Task<ActionResult<AnnualPlanWorkspaceDto>> GetWorkspace(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _planService.GetWorkspaceAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/validation")]
    public async Task<ActionResult<PlanValidationDto>> GetValidation(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _planService.GetValidationAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}/excel-template")]
    public async Task<IActionResult> DownloadTemplate(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _excelService.BuildTemplateAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>POA institucional del ciclo (todas las áreas, columna Área al frente).</summary>
    [HttpGet("cycle-export")]
    public async Task<IActionResult> ExportCycle([FromQuery] Guid cycleId)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _excelService.ExportCycleAsync(organizationId, cycleId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _excelService.ExportPlanAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    // ───────────────────────── Edición (área propia) ─────────────────────────

    [HttpPost("{id:guid}/products")]
    public async Task<ActionResult<PoaProductDto>> AddProduct(Guid id, CancellationToken ct)
    {
        var auth = await AuthorizeEditAsync(id, ct);
        if (auth.Error is not null) return auth.Error;

        var result = await _planService.AddProductAsync(auth.OrganizationId, id, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return StatusCode((int)HttpStatusCode.Created, result.Value);
    }

    [HttpPut("{id:guid}/products/{productId:guid}")]
    public async Task<ActionResult<PoaProductDto>> UpdateProduct(
        Guid id, Guid productId, UpdatePoaProductDto dto, CancellationToken ct)
    {
        var auth = await AuthorizeEditAsync(id, ct);
        if (auth.Error is not null) return auth.Error;

        var result = await _planService.UpdateProductAsync(
            auth.OrganizationId, id, productId, dto, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/products/{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, Guid productId, CancellationToken ct)
    {
        var auth = await AuthorizeEditAsync(id, ct);
        if (auth.Error is not null) return auth.Error;

        var result = await _planService.DeleteProductAsync(auth.OrganizationId, id, productId, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<AnnualPlanDto>> Submit(Guid id, CancellationToken ct)
    {
        var auth = await AuthorizeEditAsync(id, ct);
        if (auth.Error is not null) return auth.Error;

        var result = await _planService.SubmitAsync(auth.OrganizationId, id, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ImportResultDto>> Import(
        Guid id,
        IFormFile file,
        [FromQuery] string modo = "actualizar",
        [FromQuery] bool dryRun = true,
        CancellationToken ct = default)
    {
        var auth = await AuthorizeEditAsync(id, ct);
        if (auth.Error is not null) return auth.Error;

        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "Debe adjuntar el archivo Excel (.xlsx)." } });

        await using var stream = file.OpenReadStream();
        var result = await _excelService.ImportAsync(
            auth.OrganizationId, id, stream, modo, dryRun, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    // ───────────────────────── Revisión (Planificación) ─────────────────────────

    [HttpPost("{id:guid}/return")]
    public async Task<ActionResult<AnnualPlanDto>> Return(Guid id, ReturnPlanDto dto, CancellationToken ct)
    {
        var auth = await AuthorizeReviewAsync(ct);
        if (auth.Error is not null) return auth.Error;

        var result = await _planService.ReturnAsync(auth.OrganizationId, id, dto, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<AnnualPlanDto>> Approve(Guid id, CancellationToken ct)
    {
        var auth = await AuthorizeReviewAsync(ct);
        if (auth.Error is not null) return auth.Error;

        var result = await _planService.ApproveAsync(auth.OrganizationId, id, auth.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    // ───────────────────────── Autorización ─────────────────────────

    /// <summary>Editar: planning:poa:edit + área dueña del plan (planning:admin lo salta).</summary>
    private async Task<(ActionResult? Error, Guid UserId, int OrganizationId)> AuthorizeEditAsync(
        Guid planId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return (Unauthorized(new { errors = MissingClaimsError }), userId, organizationId);

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return (StatusCode((int)contextResult.StatusCode, contextResult.Error), userId, organizationId);

        var accessResult = await _planService.GetAccessInfoAsync(organizationId, planId);
        if (!accessResult.IsSuccess)
            return (StatusCode((int)accessResult.StatusCode, accessResult.Error), userId, organizationId);

        if (!_accessGuard.CanActOnArea(contextResult.Value, PlanningPermissions.PoaEdit, accessResult.Value.AreaId)
            && !_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.PoaReview))
            return (StatusCode((int)HttpStatusCode.Forbidden,
                new { errors = new[] { "Solo el área dueña del plan o el rol de Planificación pueden editarlo." } }),
                userId, organizationId);

        return (null, userId, organizationId);
    }

    /// <summary>Devolver/aprobar: planning:poa:review.</summary>
    private async Task<(ActionResult? Error, Guid UserId, int OrganizationId)> AuthorizeReviewAsync(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return (Unauthorized(new { errors = MissingClaimsError }), userId, organizationId);

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return (StatusCode((int)contextResult.StatusCode, contextResult.Error), userId, organizationId);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.PoaReview))
            return (StatusCode((int)HttpStatusCode.Forbidden,
                new { errors = new[] { "No tienes permisos para revisar planes." } }), userId, organizationId);

        return (null, userId, organizationId);
    }
}
