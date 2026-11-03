using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("planning-templates")]
[Produces("application/json")]
public class PlanningTemplatesController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IPlanningTemplateService _templateService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public PlanningTemplatesController(
        IPlanningTemplateService templateService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _templateService = templateService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PlanningTemplateDto>>> ListAsync(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _templateService.ListAsync(organizationId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PlanningTemplateDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _templateService.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<ActionResult<PlanningTemplateDetailDto>> CreateAsync([FromBody] CreatePlanningTemplateDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.CreateAsync(organizationId, dto, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PlanningTemplateDetailDto>> UpdateAsync(Guid id, [FromBody] UpdatePlanningTemplateDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.UpdateAsync(organizationId, id, dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/columns")]
    public async Task<ActionResult<PlanningTemplateColumnDto>> AddColumnAsync(Guid id, [FromBody] CreatePlanningTemplateColumnDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.AddColumnAsync(organizationId, id, dto, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{id}/columns/{columnId}")]
    public async Task<ActionResult<PlanningTemplateColumnDto>> UpdateColumnAsync(Guid id, Guid columnId, [FromBody] UpdatePlanningTemplateColumnDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.UpdateColumnAsync(organizationId, id, columnId, dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<PlanningTemplateDetailDto>> ActivateAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.ActivateAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPost("{id}/seed-standard-columns")]
    public async Task<ActionResult<PlanningTemplateDetailDto>> SeedStandardColumnsAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.SeedStandardColumnsAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}/columns/{columnId}")]
    public async Task<ActionResult<PlanningTemplateDetailDto>> DeleteColumnAsync(Guid id, Guid columnId, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.DeleteColumnAsync(organizationId, id, columnId, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
            
        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = new[] { "No tienes permisos para gestionar configuraciones." } });

        var result = await _templateService.DeleteAsync(organizationId, id, userId, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return NoContent();
    }
}
