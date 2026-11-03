using System;
using System.Collections.Generic;
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
[Route("planning-cycles")]
[Produces("application/json")]
public class PlanningCyclesController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IPlanningCycleService _cycleService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public PlanningCyclesController(
        IPlanningCycleService cycleService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _cycleService = cycleService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanningCycleDto>>> ListAsync(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _cycleService.ListAsync(organizationId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("eligible-areas")]
    public async Task<ActionResult<IEnumerable<EligibleAreaDto>>> GetEligibleAreasAsync(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _cycleService.GetEligibleAreasAsync(organizationId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}", Name = "GetPlanningCycleById")]
    public async Task<ActionResult<PlanningCycleDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _cycleService.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<PlanningCycleDetailDto>> CreateDraftAsync([FromBody] CreatePlanningCycleDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), null, permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.CyclesManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar ciclos de planificación." } });

        var result = await _cycleService.CreateDraftAsync(organizationId, dto, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return CreatedAtRoute("GetPlanningCycleById", new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PlanningCycleDetailDto>> UpdateDraftAsync(Guid id, [FromBody] UpdatePlanningCycleDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.CyclesManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar ciclos de planificación." } });

        var result = await _cycleService.UpdateDraftAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult<PlanningCycleDetailDto>> PublishAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.CyclesManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para publicar ciclos de planificación." } });

        var result = await _cycleService.PublishAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/close")]
    public async Task<IActionResult> CloseAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.CyclesManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para cerrar ciclos de planificación." } });

        var result = await _cycleService.CloseAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }

    [HttpPost("{id}/reopen")]
    public async Task<IActionResult> ReopenAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.CyclesManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para reabrir ciclos de planificación." } });

        var result = await _cycleService.ReopenAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }
}
