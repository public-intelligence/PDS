using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Position;
using PDS.Service.Authorization;
using PDS.Service.Services;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class PositionsController : ApiControllerBase
{
    private readonly ILogger<PositionsController> _logger;
    private readonly IPositionService _positionService;
    private readonly IPaginationService _paginationService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public PositionsController(
        ILogger<PositionsController> logger,
        IPositionService positionService,
        IPaginationService paginationService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _logger = logger;
        _positionService = positionService;
        _paginationService = paginationService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    private static readonly string[] value = ["No se pudo obtener userId y organizationId del token."];
    private static readonly string[] StructureForbiddenError =
        ["No tienes permisos para modificar los cargos del organigrama."];

    [HttpGet()]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> GetOrganizationPositions(
        [FromQuery] PaginationFilterDto pagination)
    {
        // Flujo normal: Bearer JWT de usuario
        if (!TryGetUserAndOrganizationIds(out var userId, out var orgId))
            return Unauthorized(new { errors = value });

        var result = await _positionService.ListAsync(orgId, userId, pagination, HttpContext);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        _paginationService.SetPaginationHeaders(Response, result.Value);
        return Ok(result.Value.Value);
    }
    [HttpGet("/m2m/organizations/{organizationId}/positions")]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> GetOrganizationPositionsM2M(
        int organizationId,
        [FromQuery] PaginationFilterDto pagination)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var m2mResult = await _positionService.ListAsync(organizationId, Guid.Empty, pagination, HttpContext);
        if (!m2mResult.IsSuccess)
            return StatusCode((int)m2mResult.StatusCode, m2mResult.Error);

        _paginationService.SetPaginationHeaders(Response, m2mResult.Value);
        return Ok(m2mResult.Value.Value);
    }
    [HttpPost()]
    [ProducesResponseType(201)]
    public async Task<ActionResult> CreateOrganizationPosition(
    CreatePositionDto createPositionDto
)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        // Los cargos cuelgan de una unidad organizacional y son el puente por el que se
        // resuelve el área de cada usuario: crearlos es administración del organigrama.
        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, CancellationToken.None);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.StructureManage))
            return StatusCode(403, new { errors = StructureForbiddenError });

        var createPositionResult = await _positionService.CreateAsync(
            organizationId,
            userId,
            createPositionDto
        );
        if (!createPositionResult.IsSuccess)
        {
            return StatusCode((int)createPositionResult.StatusCode, createPositionResult.Error);
        }

        var positionDto = createPositionResult.Value;

        return CreatedAtAction(
            nameof(GetOrganizationPosition),
            new { organizationId, positionId = positionDto.Id },
            null
        );
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> SearchOrganizationPositions(
        string? q,
        [FromQuery] PaginationFilterDto pagination
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var result = await _positionService.SearchAsync(organizationId, q ?? string.Empty, pagination, HttpContext);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }

        var pagedData = result.Value;

        _paginationService.SetPaginationHeaders(Response, pagedData);

        return Ok(pagedData.Value);
    }

    
    [HttpGet("structures/{structureId}/positions/search")]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> SearchOrganizationStructurePositions(
        Guid structureId,
        string? q,
        [FromQuery] PaginationFilterDto pagination,
        [FromQuery] bool includeDescendants = false
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var result = await _positionService.SearchAsync(organizationId, structureId, q ?? string.Empty, pagination, HttpContext, includeDescendants);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var pagedData = result.Value;
        _paginationService.SetPaginationHeaders(Response, pagedData);
        return Ok(pagedData.Value);
    }

    [HttpGet("/m2m/organization/{organizationId}/structures/{structureId}/positions/search")]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> OrganizationStructurePositionsAsync(
        Guid structureId,
        int organizationId,
        string? q,
        [FromQuery] PaginationFilterDto pagination,
        [FromQuery] bool includeDescendants = false
    )
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _positionService.SearchAsync(organizationId, structureId, q ?? string.Empty, pagination, HttpContext, includeDescendants);
        if (!result.IsSuccess)
        {
            return StatusCode((int)result.StatusCode, result.Error);
        }
        var pagedData = result.Value;
        _paginationService.SetPaginationHeaders(Response, pagedData);
        return Ok(pagedData.Value);
    }


    [HttpGet("{positionId}")]
    public async Task<ActionResult<PositionDto>> GetOrganizationPosition(
        Guid positionId
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var positionResult = await _positionService.GetAsync(organizationId, userId, positionId);
        if (!positionResult.IsSuccess)
        {
            return StatusCode((int)positionResult.StatusCode, positionResult.Error);
        }

        var positionDto = positionResult.Value;
        return Ok(positionDto);
    }
    
    [HttpGet("/m2m/organizations/{organizationId}/positions/search")]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> SearchOrganizationPositionsM2M(
        int organizationId,
        string? q,
        [FromQuery] PaginationFilterDto pagination)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _positionService.SearchAsync(organizationId, q ?? string.Empty, pagination, HttpContext);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        var pagedData = result.Value;
        _paginationService.SetPaginationHeaders(Response, pagedData);
        return Ok(pagedData.Value);
    }

    [HttpGet("/m2m/organizations/{organizationId}/positions/supervisors")]
    public async Task<ActionResult<IEnumerable<PositionSupervisorLevelDto>>> GetPositionSupervisorChainM2M(
        int organizationId,
        [FromQuery] List<Guid> positionIds)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        if (positionIds == null || positionIds.Count == 0)
            return BadRequest(new { errors = new[] { "Debe proporcionar al menos un positionId." } });

        var result = await _positionService.GetSupervisorChainAsync(organizationId, positionIds);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("/m2m/organizations/{organizationId}/positions/{positionId}/subordinates")]
    public async Task<ActionResult<IEnumerable<PositionSupervisorLevelDto>>> GetSubordinatePositionsM2M(
        int organizationId,
        Guid positionId)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _positionService.GetSubordinatePositionsAsync(organizationId, positionId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("/m2m/organizations/{organizationId}/positions/{positionId}")]
    public async Task<ActionResult<PagedResponseDto<PositionDto>>> GetOrganizationPositionByIdM2M(
        int organizationId,
        Guid positionId)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var positionResult = await _positionService.GetAsync(organizationId, positionId);
        if (!positionResult.IsSuccess)
            return StatusCode((int)positionResult.StatusCode, positionResult.Error);

        return Ok(positionResult.Value);
    }
    [HttpGet("/m2m/organizations/{organizationId}/positions/{positionId}/direct-manager")]
    public async Task<ActionResult<PositionDto?>> GetDirectManagerM2M(
        int organizationId,
        Guid positionId)
 {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _positionService.GetDirectManagerByPositionAsync(organizationId, positionId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("/m2m/organizations/{organizationId}/structures/{structureId}/positions")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetPositionIdsByStructureM2M(
        int organizationId,
        Guid structureId,
        [FromQuery] bool includeDescendants = false)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        var result = await _positionService.GetPositionIdsByStructureAsync(organizationId, structureId, includeDescendants);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpGet("/m2m/organizations/{organizationId}/positions/by-ids")]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositionsByIdsM2M(
        int organizationId,
        [FromQuery] List<Guid> positionIds)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync();
        if (m2mCheck != null) return m2mCheck;

        if (positionIds == null || positionIds.Count == 0)
            return BadRequest(new { errors = new[] { "Debe proporcionar al menos un positionId." } });

        var result = await _positionService.GetByIdsAsync(organizationId, positionIds);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPut("{positionId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> UpdateOrganizationPosition(
        Guid positionId,
        UpdatePositionDto updatePositionDto
    )
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var updatePositionResult = await _positionService.UpdateAsync(
            organizationId,
            positionId,
            userId,
            updatePositionDto
        );
        if (!updatePositionResult.IsSuccess)
        {
            return StatusCode((int)updatePositionResult.StatusCode, updatePositionResult.Error);
        }

        return NoContent();
    }

    [HttpDelete("{positionId}")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> DeleteOrganizationPosition(Guid positionId)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
        {
            return Unauthorized(new { errors = value });
        }

        var deletePositionResult = await _positionService.DeleteAsync(
            organizationId,
            userId,
            positionId
        );
        if (!deletePositionResult.IsSuccess)
        {
            return StatusCode((int)deletePositionResult.StatusCode, deletePositionResult.Error);
        }

        return NoContent();
    }

}
