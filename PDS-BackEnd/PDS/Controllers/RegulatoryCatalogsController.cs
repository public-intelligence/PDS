using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("regulatory-catalogs")]
[Produces("application/json")]
public class RegulatoryCatalogsController : ApiControllerBase
{
    private readonly IRegulatoryCatalogService _service;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _guard;

    public RegulatoryCatalogsController(
        IRegulatoryCatalogService service,
        IPlanningContextService contextService,
        IPlanningAccessGuard guard)
    {
        _service = service;
        _contextService = contextService;
        _guard = guard;
    }

    [HttpGet("expense-objects")]
    public async Task<ActionResult<PagedResponseDto<ExpenseObjectDto>>> ListExpenseObjects(
        [FromQuery] string? q, [FromQuery] bool? onlyDetail, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
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

        var result = await _service.ListExpenseObjectsAsync(organizationId, q, onlyDetail, page, pageSize);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("accounting-accounts")]
    public async Task<ActionResult<PagedResponseDto<AccountingAccountDto>>> ListAccountingAccounts(
        [FromQuery] string? q, [FromQuery] bool? onlyPostable, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
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

        var result = await _service.ListAccountingAccountsAsync(organizationId, q, onlyPostable, page, pageSize);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("consolidator-area-profiles")]
    public async Task<ActionResult<IReadOnlyList<ConsolidatorAreaProfileDto>>> ListConsolidatorAreaProfiles()
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

        var result = await _service.ListConsolidatorAreaProfilesAsync(organizationId);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    // ───────────── Catálogo Regulatorio MasterSoft ─────────────

    [HttpGet("mastersoft-accounts")]
    public async Task<ActionResult<PagedResponseDto<MastersoftAccountDto>>> ListMastersoftAccounts(
        [FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogRead);
        if (m2mCheck != null) return m2mCheck;

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

        var result = await _service.ListMastersoftAccountsAsync(organizationId, q, page, pageSize);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("mastersoft-mappings")]
    public async Task<ActionResult<PagedResponseDto<MastersoftDigecobMappingDto>>> ListMastersoftMappings(
        [FromQuery] string? q, [FromQuery] string? filterMode, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogRead);
        if (m2mCheck != null) return m2mCheck;

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

        var result = await _service.ListMastersoftMappingsAsync(organizationId, q, filterMode, page, pageSize);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost("mastersoft-mappings")]
    public async Task<ActionResult<MastersoftDigecobMappingDto>> CreateOrUpdateMastersoftMapping([FromBody] CreateMastersoftMappingDto dto)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogManage);
        if (m2mCheck != null) return m2mCheck;

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

        var result = await _service.CreateOrUpdateMastersoftMappingAsync(organizationId, dto);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpDelete("mastersoft-mappings/{id:guid}")]
    public async Task<ActionResult> DeleteMastersoftMapping(Guid id)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogManage);
        if (m2mCheck != null) return m2mCheck;

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

        var result = await _service.DeleteMastersoftMappingAsync(organizationId, id);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    [HttpPost("mastersoft-import")]
    public async Task<ActionResult<MastersoftImportSummaryDto>> ImportMastersoftCatalog(Microsoft.AspNetCore.Http.IFormFile file)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogManage);
        if (m2mCheck != null) return m2mCheck;

        if (file == null || file.Length == 0) return BadRequest(new { errors = new[] { "Debe proporcionar un archivo Excel válido." } });

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

        using var stream = file.OpenReadStream();
        var result = await _service.ImportMastersoftCatalogAndMappingsAsync(organizationId, stream);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("mastersoft-export-homologation")]
    public async Task<IActionResult> ExportMastersoftHomologationReport()
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogRead);
        if (m2mCheck != null) return m2mCheck;

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

        var result = await _service.ExportMastersoftHomologationReportAsync(organizationId);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);

        var fileName = $"Homologacion_DIGECOB_MasterSoft_{System.DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(result.Value, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
