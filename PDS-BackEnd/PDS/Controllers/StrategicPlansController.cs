using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System.Net;

namespace PDS.Controllers;

/// <summary>
/// PEI versionado por enmiendas (M3). Lectura para cualquier usuario autenticado del tenant;
/// edición del Draft con planning:pei:edit (Planificación); aprobación con planning:pei:approve
/// (MAE). Import/export Excel sobre la enmienda en elaboración.
/// </summary>
[Authorize]
[ApiController]
[Route("strategic-plans")]
[Produces("application/json")]
public class StrategicPlansController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IStrategicPlanService _planService;
    private readonly IStrategicPlanExcelService _excelService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public StrategicPlansController(
        IStrategicPlanService planService,
        IStrategicPlanExcelService excelService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _planService = planService;
        _excelService = excelService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    // ───────────────────────── Lectura ─────────────────────────

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StrategicPlanDto>>> List()
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _planService.ListAsync(organizationId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StrategicPlanDetailDto>> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _planService.GetByIdAsync(organizationId, id);
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

    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> Export(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _excelService.ExportAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    // ───────────────────────── Versionado ─────────────────────────

    [HttpPost]
    public async Task<ActionResult<StrategicPlanDetailDto>> CreateInitial(
        CreateStrategicPlanDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.CreateInitialAsync(
            authResult.OrganizationId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPost("amendments")]
    public async Task<ActionResult<StrategicPlanDetailDto>> StartAmendment(
        CreateAmendmentDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.StartAmendmentAsync(
            authResult.OrganizationId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpDelete("amendments/current")]
    public async Task<IActionResult> DiscardAmendment(CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.DiscardDraftAsync(
            authResult.OrganizationId, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("amendments/current/approve")]
    public async Task<ActionResult<StrategicPlanDto>> ApproveAmendment(CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiApprove, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.ApproveDraftAsync(
            authResult.OrganizationId, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<StrategicPlanDto>> UpdatePlan(
        Guid id, UpdateStrategicPlanDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.UpdatePlanAsync(
            authResult.OrganizationId, id, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    // ───────────────────────── Focos / Objetivos / Productos ─────────────────────────

    [HttpPost("{id:guid}/focuses")]
    public async Task<ActionResult<StrategicFocusDto>> AddFocus(
        Guid id, CreateStrategicFocusDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.AddFocusAsync(
            authResult.OrganizationId, id, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return StatusCode((int)HttpStatusCode.Created, result.Value);
    }

    [HttpPut("{id:guid}/focuses/{focusId:guid}")]
    public async Task<ActionResult<StrategicFocusDto>> UpdateFocus(
        Guid id, Guid focusId, UpdateStrategicFocusDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.UpdateFocusAsync(
            authResult.OrganizationId, id, focusId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/focuses/{focusId:guid}")]
    public async Task<IActionResult> DeleteFocus(Guid id, Guid focusId, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.DeleteFocusAsync(
            authResult.OrganizationId, id, focusId, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("{id:guid}/focuses/{focusId:guid}/objectives")]
    public async Task<ActionResult<StrategicObjectiveDto>> AddObjective(
        Guid id, Guid focusId, CreateStrategicObjectiveDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.AddObjectiveAsync(
            authResult.OrganizationId, id, focusId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return StatusCode((int)HttpStatusCode.Created, result.Value);
    }

    [HttpPut("{id:guid}/objectives/{objectiveId:guid}")]
    public async Task<ActionResult<StrategicObjectiveDto>> UpdateObjective(
        Guid id, Guid objectiveId, UpdateStrategicObjectiveDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.UpdateObjectiveAsync(
            authResult.OrganizationId, id, objectiveId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/objectives/{objectiveId:guid}")]
    public async Task<IActionResult> DeleteObjective(Guid id, Guid objectiveId, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.DeleteObjectiveAsync(
            authResult.OrganizationId, id, objectiveId, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("{id:guid}/objectives/{objectiveId:guid}/products")]
    public async Task<ActionResult<StrategicProductDto>> AddProduct(
        Guid id, Guid objectiveId, CreateStrategicProductDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.AddProductAsync(
            authResult.OrganizationId, id, objectiveId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return StatusCode((int)HttpStatusCode.Created, result.Value);
    }

    [HttpPut("{id:guid}/products/{productId:guid}")]
    public async Task<ActionResult<StrategicProductDto>> UpdateProduct(
        Guid id, Guid productId, UpdateStrategicProductDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.UpdateProductAsync(
            authResult.OrganizationId, id, productId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}/products/{productId:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id, Guid productId, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.DeleteProductAsync(
            authResult.OrganizationId, id, productId, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("{id:guid}/products/{productId:guid}/discontinue")]
    public async Task<ActionResult<StrategicProductDto>> DiscontinueProduct(
        Guid id, Guid productId, DiscontinueDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.DiscontinueProductAsync(
            authResult.OrganizationId, id, productId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    // ───────────────────────── Unidades de medida ─────────────────────────

    [HttpPut("{id:guid}/products/{productId:guid}/measure-units")]
    public async Task<ActionResult<StrategicProductDto>> ReplaceMeasureUnits(
        Guid id, Guid productId, ReplaceMeasureUnitsDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.ReplaceMeasureUnitsAsync(
            authResult.OrganizationId, id, productId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/measure-units/{unitId:guid}/discontinue")]
    public async Task<ActionResult<StrategicProductDto>> DiscontinueMeasureUnit(
        Guid id, Guid unitId, DiscontinueDto dto, CancellationToken ct)
    {
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        var result = await _planService.DiscontinueMeasureUnitAsync(
            authResult.OrganizationId, id, unitId, dto, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    // ───────────────────────── Importación Excel ─────────────────────────

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
        var authResult = await AuthorizeAsync(PlanningPermissions.PeiEdit, ct);
        if (authResult.Error is not null) return authResult.Error;

        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "Debe adjuntar el archivo Excel (.xlsx)." } });

        await using var stream = file.OpenReadStream();
        var result = await _excelService.ImportAsync(
            authResult.OrganizationId, id, stream, modo, dryRun, authResult.UserId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    // ───────────────────────── Autorización ─────────────────────────

    private async Task<(ActionResult? Error, Guid UserId, int OrganizationId)> AuthorizeAsync(
        string permission, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return (Unauthorized(new { errors = MissingClaimsError }), userId, organizationId);

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return (StatusCode((int)contextResult.StatusCode, contextResult.Error), userId, organizationId);

        if (!_accessGuard.HasPermission(contextResult.Value, permission))
            return (StatusCode((int)HttpStatusCode.Forbidden,
                new { errors = new[] { "No tienes permisos para esta operación del PEI." } }), userId, organizationId);

        return (null, userId, organizationId);
    }
}
