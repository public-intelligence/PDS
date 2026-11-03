using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("consolidation")]
[Produces("application/json")]
public class ConsolidationController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IConsolidationService _consolidationService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public ConsolidationController(
        IConsolidationService consolidationService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _consolidationService = consolidationService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    [HttpGet("board")]
    public async Task<ActionResult<ConsolidationBoardDto>> GetBoardAsync([FromQuery] Guid cycleId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview)
            && !_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate)
            && !_accessGuard.HasPermission(context, PlanningPermissions.PoaEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para consultar el tablero de consolidación." } });

        var result = await _consolidationService.GetBoardAsync(organizationId, cycleId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("product-board")]
    public async Task<ActionResult<ProductDemandBoardDto>> GetProductBoardAsync([FromQuery] Guid cycleId, [FromQuery] Guid? consolidatorAreaId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview)
            && !_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate)
            && !_accessGuard.HasPermission(context, PlanningPermissions.PoaEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para consultar el tablero por artículo." } });

        var result = await _consolidationService.GetProductBoardAsync(organizationId, cycleId, consolidatorAreaId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("by-expense-object")]
    public async Task<ActionResult<ExpenseObjectsBoardPagedDto>> GetExpenseObjectsBoardAsync(
        [FromQuery] Guid cycleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 40,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview)
            && !_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate)
            && !_accessGuard.HasPermission(context, PlanningPermissions.PoaEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para consultar el tablero por objeto del gasto." } });

        var result = await _consolidationService.GetExpenseObjectsBoardAsync(organizationId, cycleId, pageNumber, pageSize, search);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("by-product")]
    public async Task<ActionResult<ProductsBoardPagedDto>> GetProductsBoardAsync(
        [FromQuery] Guid cycleId,
        [FromQuery] Guid? consolidatorAreaId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 40,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview)
            && !_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate)
            && !_accessGuard.HasPermission(context, PlanningPermissions.PoaEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para consultar el tablero por producto." } });

        var result = await _consolidationService.GetProductsBoardAsync(organizationId, cycleId, consolidatorAreaId, pageNumber, pageSize, search);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("by-consolidator-area")]
    public async Task<ActionResult<ConsolidatorAreasBoardPagedDto>> GetConsolidatorAreasBoardAsync(
        [FromQuery] Guid cycleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 40,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview)
            && !_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate)
            && !_accessGuard.HasPermission(context, PlanningPermissions.PoaEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para consultar el tablero por área." } });

        var result = await _consolidationService.GetConsolidatorAreasBoardAsync(organizationId, cycleId, pageNumber, pageSize, search);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }


    [HttpPost("open-adjustments")]
    public async Task<IActionResult> OpenAdjustmentsPhaseAsync([FromQuery] Guid cycleId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para abrir la fase de ajustes." } });

        var result = await _consolidationService.OpenAdjustmentsPhaseAsync(organizationId, cycleId, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("submit-adjustments")]
    public async Task<IActionResult> SubmitAdjustmentsAsync([FromQuery] Guid planId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaEdit) && !_accessGuard.HasPermission(context, PlanningPermissions.PoaReview))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para someter ajustes de este plan." } });

        var result = await _consolidationService.SubmitAdjustmentsAsync(organizationId, planId, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpGet("pacc-export")]
    public async Task<IActionResult> ExportPaccAsync([FromQuery] Guid cycleId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.PoaReview)
            && !_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para exportar el PACC." } });

        var result = await _consolidationService.ExportPaccAsync(organizationId, cycleId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return File(
            result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"PACC_{cycleId}.xlsx");
    }
}
