using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("requirements")]
[Produces("application/json")]
public class RequirementsController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IRequirementService _requirementService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public RequirementsController(
        IRequirementService requirementService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _requirementService = requirementService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<RequirementDto>>> ListAsync(
        [FromQuery] Guid? cycleId,
        [FromQuery] Guid? consolidatorAreaId,
        [FromQuery] Guid? originAreaId,
        [FromQuery] string? status,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var pagination = new PaginationFilterDto(pageNumber, pageSize);
        var result = await _requirementService.ListAsync(
            organizationId, cycleId, consolidatorAreaId, originAreaId, status, pagination);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("tasks")]
    public async Task<ActionResult<IEnumerable<PoaTaskOptionDto>>> GetAvailableTasksAsync(
        [FromQuery] Guid? cycleId,
        [FromQuery] Guid? areaId,
        [FromQuery] Guid? productId,
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _requirementService.GetAvailableTasksAsync(organizationId, cycleId, areaId, productId, search);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RequirementDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _requirementService.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<RequirementDetailDto>> CreateFromTaskAsync(
        [FromBody] CreateRequirementDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para crear requerimientos." } });

        var originAreaId = context.PlanAreaId ?? context.UnitAreaId ?? Guid.Empty;
        var originAreaName = context.PlanAreaName ?? context.UnitAreaName ?? "Área del usuario";

        var result = await _requirementService.CreateFromTaskAsync(
            organizationId, dto, userId, GetUserName(), originAreaId, originAreaName);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return CreatedAtAction("GetById", new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RequirementDetailDto>> UpdateAsync(
        Guid id, [FromBody] UpdateRequirementDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para editar requerimientos." } });

        var userAreaId = context.PlanAreaId ?? context.UnitAreaId ?? Guid.Empty;

        var result = await _requirementService.UpdateAsync(
            organizationId, id, dto, userId, GetUserName(), userAreaId);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsEdit))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para eliminar requerimientos." } });

        var userAreaId = context.PlanAreaId ?? context.UnitAreaId ?? Guid.Empty;

        var result = await _requirementService.DeleteAsync(
            organizationId, id, userId, GetUserName(), userAreaId);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("{id}/confirm-price")]
    public async Task<ActionResult<RequirementDetailDto>> ConfirmPriceAsync(
        Guid id, [FromBody] ConfirmPriceDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para consolidar requerimientos." } });

        // Pertenencia real a áreas consolidadoras (perfil enlazado al organigrama). Vacío =
        // se deniega en el servicio: el permiso solo, sin pertenencia, no consolida.
        var result = await _requirementService.ConfirmPriceAsync(
            organizationId, id, dto.Price, userId, GetUserName(), context.ConsolidatorAreaIds);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/decide")]
    public async Task<ActionResult<RequirementDetailDto>> DecideAsync(
        Guid id, [FromBody] RequirementDecisionDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para decidir sobre requerimientos." } });

        var result = await _requirementService.DecideAsync(
            organizationId, id, dto, userId, GetUserName(), context.ConsolidatorAreaIds);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/reduce")]
    public async Task<ActionResult<RequirementDetailDto>> ReduceAsync(
        Guid id, [FromBody] ReduceRequirementDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para reducir requerimientos." } });

        var result = await _requirementService.ReduceAsync(
            organizationId, id, dto.ApprovedAmount, dto.Reason, userId, GetUserName(), context.ConsolidatorAreaIds);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("merge")]
    public async Task<ActionResult<RequirementDetailDto>> MergeAsync(
        [FromBody] MergeRequirementsDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        var context = contextResult.Value;
        if (!_accessGuard.HasPermission(context, PlanningPermissions.RequirementsConsolidate))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para fusionar requerimientos." } });

        var result = await _requirementService.MergeAsync(
            organizationId, dto, userId, GetUserName(), context.ConsolidatorAreaIds);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("by-task/{taskId}")]
    public async Task<ActionResult<IEnumerable<RequirementDto>>> ListByTaskAsync(Guid taskId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _requirementService.ListByTaskAsync(organizationId, taskId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<RequirementCommentDto>> AddCommentAsync(
        Guid id, [FromBody] AddRequirementCommentDto dto, [FromQuery] string? areaName, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _requirementService.AddCommentAsync(
            organizationId, id, dto.Text, userId, GetUserName(), areaName);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }
}
