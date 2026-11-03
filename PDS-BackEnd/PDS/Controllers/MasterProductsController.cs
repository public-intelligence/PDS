using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("master-products")]
[Produces("application/json")]
public class MasterProductsController : ApiControllerBase
{
    private readonly IMasterProductService _service;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _guard;

    public MasterProductsController(
        IMasterProductService service,
        IPlanningContextService contextService,
        IPlanningAccessGuard guard)
    {
        _service = service;
        _contextService = contextService;
        _guard = guard;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<MasterProductSearchItemDto>>> Search(
        [FromQuery] string q, [FromQuery] string? itemType, [FromQuery] int take = 20)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.RequirementsEdit) &&
            !_guard.HasPermission(ctx, PlanningPermissions.PoaEdit) &&
            !_guard.HasPermission(ctx, PlanningPermissions.CatalogRead) &&
            !_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.SearchAsync(organizationId, q, itemType, take);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<MasterProductDto>>> List(
        [FromQuery] string? q, [FromQuery] string? state, [FromQuery] Guid? consolidatorAreaId,
        [FromQuery] bool? missingMapping, [FromQuery] bool? requiresReview, [FromQuery] string? unspscPrefix,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (ctx.Permissions.Count > 0 &&
            !_guard.HasPermission(ctx, PlanningPermissions.CatalogRead) &&
            !_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.ListAsync(organizationId, q, state, consolidatorAreaId, missingMapping, requiresReview, unspscPrefix, page, pageSize);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("mapping-summary")]
    public async Task<ActionResult<MasterProductMappingSummaryDto>> GetMappingSummary()
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (ctx.Permissions.Count > 0 && !_guard.HasPermission(ctx, PlanningPermissions.CatalogManage) && !_guard.HasPermission(ctx, PlanningPermissions.CatalogRead))
        {
            return Forbid();
        }

        var result = await _service.GetMappingSummaryAsync(organizationId);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MasterProductDetailDto>> GetById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.CatalogRead) &&
            !_guard.HasPermission(ctx, PlanningPermissions.CatalogManage))
        {
            return Forbid();
        }

        var result = await _service.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<MasterProductDetailDto>> Create([FromBody] CreateMasterProductDto dto)
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

        var result = await _service.CreateAsync(organizationId, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MasterProductDetailDto>> Update(Guid id, [FromBody] UpdateMasterProductDto dto)
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

        var result = await _service.UpdateAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/mapping")]
    public async Task<ActionResult<MasterProductDetailDto>> UpdateMapping(Guid id, [FromBody] UpdateMasterProductMappingDto dto)
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

        var result = await _service.UpdateMappingAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/consolidator-area")]
    public async Task<ActionResult<MasterProductDetailDto>> RatifyConsolidatorArea(Guid id, [FromBody] RatifyConsolidatorAreaDto dto)
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

        var result = await _service.RatifyConsolidatorAreaAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("bulk-ratify")]
    public async Task<ActionResult<int>> BulkRatify([FromBody] BulkRatifyByUnspscDto dto)
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

        var result = await _service.BulkRatifyAsync(organizationId, dto, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(new { count = result.Value });
    }

    [HttpPost("request")]
    public async Task<ActionResult<MasterProductDetailDto>> RequestProduct([FromBody] RequestMasterProductDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });

        var perms = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(organizationId, userId, GetUserName(), GetPersonnelId(), perms);
        if (!contextResult.IsSuccess) return StatusCode((int)contextResult.StatusCode, contextResult.Error);
        var ctx = contextResult.Value;

        if (!_guard.HasPermission(ctx, PlanningPermissions.RequirementsEdit) && !_guard.HasPermission(ctx, PlanningPermissions.PoaEdit))
        {
            return Forbid();
        }

        var areaId = ctx.PlanAreaId ?? ctx.UnitAreaId;
        var areaName = ctx.PlanAreaName ?? ctx.UnitAreaName;

        var result = await _service.RequestProductAsync(organizationId, dto, userId, GetUserName(), areaId, areaName);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/state")]
    public async Task<ActionResult<MasterProductDetailDto>> SetState(Guid id, [FromQuery] string state)
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

        var result = await _service.SetStateAsync(organizationId, id, state, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
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

        var result = await _service.DeleteAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }
}
