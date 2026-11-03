using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("consolidator-routing-rules")]
[Produces("application/json")]
public class ConsolidatorRoutingRulesController : ApiControllerBase
{
    private readonly IConsolidatorRoutingService _service;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _guard;

    public ConsolidatorRoutingRulesController(
        IConsolidatorRoutingService service,
        IPlanningContextService contextService,
        IPlanningAccessGuard guard)
    {
        _service = service;
        _contextService = contextService;
        _guard = guard;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConsolidatorRoutingRuleDto>>> List()
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage) && !_guard.HasPermission(ctx, PlanningPermissions.CatalogRead))
        {
            return Forbid();
        }

        var result = await _service.ListRulesAsync(organizationId);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConsolidatorRoutingRuleDto>> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage) && !_guard.HasPermission(ctx, PlanningPermissions.CatalogRead))
        {
            return Forbid();
        }

        var result = await _service.GetRuleByIdAsync(organizationId, id);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<ConsolidatorRoutingRuleDto>> Create([FromBody] CreateConsolidatorRoutingRuleDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.CreateRuleAsync(organizationId, dto);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ConsolidatorRoutingRuleDto>> UpdateMetadata(Guid id, [FromBody] UpdateConsolidatorRoutingRuleMetadataDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.UpdateMetadataAsync(organizationId, id, dto);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/new-version")]
    public async Task<ActionResult<ConsolidatorRoutingRuleDto>> CreateNewVersion(Guid id, [FromBody] CreateNewRuleVersionDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.CreateNewVersionAsync(organizationId, id, dto);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/activate")]
    public async Task<ActionResult> SetActive(Guid id, [FromQuery] bool isActive)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.SetActiveAsync(organizationId, id, isActive);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.DeleteRuleAsync(organizationId, id);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    [HttpPost("{id:guid}/preview")]
    public async Task<ActionResult<RulePreviewResultDto>> Preview(Guid id, [FromQuery] int take = 20)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.PreviewAsync(organizationId, id, take);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("reevaluate")]
    public async Task<ActionResult<int>> Reevaluate([FromQuery] bool onlyUnratified = true, [FromQuery] Guid? masterProductId = null)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!onlyUnratified && !_guard.HasPermission(ctx, PlanningPermissions.Admin))
        {
            return Forbid();
        }

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.ReevaluateAsync(organizationId, masterProductId, onlyUnratified, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(new { updatedCount = result.Value });
    }
}
