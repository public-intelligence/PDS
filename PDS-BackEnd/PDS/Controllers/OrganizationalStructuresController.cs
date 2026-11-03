using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.OrganizationalStructure;
using PDS.Domain.DTOs.Pagination;
using PDS.Service.Authorization;
using PDS.Service.Services;
using PDS.Service.Services.Planning;
using PDS.Domain.DTOs.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class OrganizationalStructuresController : ApiControllerBase
{
    private readonly ILogger<OrganizationalStructuresController> _logger;
    private readonly IOrganizationalStructureService _organizationalStructureService;
    private readonly IPaginationService _paginationService;
    private readonly IConsolidatorImpactService _impactService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;
    private static readonly string[] value = ["No se pudo obtener userId y organizationId del token."];
    private static readonly string[] StructureForbiddenError =
        ["No tienes permisos para modificar el organigrama institucional."];

    public OrganizationalStructuresController(
        ILogger<OrganizationalStructuresController> logger,
        IOrganizationalStructureService organizationalStructureService,
        IPaginationService paginationService,
        IConsolidatorImpactService impactService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard
    )
    {
        _logger = logger;
        _organizationalStructureService = organizationalStructureService;
        _paginationService = paginationService;
        _impactService = impactService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    /// <summary>
    /// Exige planning:structure:manage. El organigrama es la base sobre la que todo el
    /// ecosistema resuelve el área de un usuario: modificarlo cambia quién puede ver y
    /// editar qué, en PDS y en los sistemas que consumen esta estructura.
    /// </summary>
    private async Task<ActionResult?> RequireStructureManageAsync(
        Guid userId, int organizationId, CancellationToken ct = default)
    {
        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.StructureManage))
            return StatusCode(403, new { errors = StructureForbiddenError });

        return null;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<OrganizationalStructureDto>>> GetOrgStructures()
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var result = await _organizationalStructureService.ListAsync(organizationId);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("organizations/{organizationId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<PagedResponseDto<OrganizationalStructureWhioutDependenciesDto>>> GetOrgStructures(
        int organizationId,
        [FromQuery] PaginationFilterDto paginationFilter,
        [FromQuery] string? search,
        [FromQuery] bool includeExternal = true)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _organizationalStructureService.ListPaginatedAsync(organizationId, paginationFilter, HttpContext, search, includeExternal);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        var pagedData = result.Value;
        _paginationService.SetPaginationHeaders(Response, pagedData);
        return Ok(pagedData.Value);
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<ActionResult> CreateOrganizationalStructure(
        [FromBody] CreateOrganizationalStructureDto createOrganizationalStructureDto
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var denied = await RequireStructureManageAsync(userId, organizationId);
        if (denied is not null) return denied;

        var createOrgStructResult = await _organizationalStructureService.CreateAsync(
            organizationId,
            userId,
            createOrganizationalStructureDto
        );
        if (!createOrgStructResult.IsSuccess)
        {
            return StatusCode((int)createOrgStructResult.StatusCode, createOrgStructResult.Error);
        }

        var organizationalStructure = createOrgStructResult.Value;
        return CreatedAtAction(
            nameof(GetOrganizationalStructureById),
            new { organizationId, structureId = organizationalStructure.Id },
            null
        );
    }

    [HttpPost("import")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<OrganizationalStructureDto>>> ImportStructures(
        [FromBody] ImportStructureDto importStructureDto
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var denied = await RequireStructureManageAsync(userId, organizationId);
        if (denied is not null) return denied;

        var result = await _organizationalStructureService.ImportAsync(
            organizationId,
            userId,
            importStructureDto
        );
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{structureId}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<OrganizationalStructureDto>> GetOrganizationalStructureById(
        Guid structureId
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var result = await _organizationalStructureService.GetAsync(
            organizationId,
            structureId,
            userId
        );
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPut("{structureId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> UpdateOrganizationalStructureById(
        Guid structureId,
        UpdateOrganizationalStructureDto updateOrganizationalStructureDto
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var denied = await RequireStructureManageAsync(userId, organizationId);
        if (denied is not null) return denied;

        var result = await _organizationalStructureService.UpdateAsync(
            organizationId,
            structureId,
            userId,
            updateOrganizationalStructureDto
        );
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return NoContent();
    }

    [HttpDelete("{structureId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> DeleteOrganizationalStructureById(
        Guid structureId
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var denied = await RequireStructureManageAsync(userId, organizationId);
        if (denied is not null) return denied;

        var result = await _organizationalStructureService.DeleteAsync(
            organizationId,
            structureId,
            userId
        );
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return NoContent();
    }

    [HttpGet("search")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<OrganizationalStructureDto>>> SearchOrganizationalStructures(
        string? q
    )
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var result = await _organizationalStructureService.SearchAsync(organizationId, q ?? string.Empty);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id}/deletion-impact")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<ConsolidatorUnitImpactDto>> GetDeletionImpact(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var result = await _impactService.AnalyzeUnitImpactAsync(organizationId, id);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        return Ok(result.Value);
    }
}
