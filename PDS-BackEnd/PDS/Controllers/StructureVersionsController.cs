using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.StructureVersion;
using PDS.Domain.Enums;
using PDS.Service.Services;
using PDS.Service.Services.Planning;
using PDS.Domain.DTOs.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
[Consumes("application/json")]
[Produces("application/json")]
public class StructureVersionsController : ApiControllerBase
{
    private readonly ILogger<StructureVersionsController> _logger;
    private readonly IStructureVersionService _versionService;
    private readonly IOrgChartService _orgChartService;
    private readonly IOrganizationDirectoryService _organizationDirectoryService;
    private readonly IConsolidatorImpactService _impactService;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];

    public StructureVersionsController(
        ILogger<StructureVersionsController> logger,
        IStructureVersionService versionService,
        IOrgChartService orgChartService,
        IOrganizationDirectoryService organizationDirectoryService,
        IConsolidatorImpactService impactService)
    {
        _logger = logger;
        _versionService = versionService;
        _orgChartService = orgChartService;
        _organizationDirectoryService = organizationDirectoryService;
        _impactService = impactService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetVersions([FromQuery] VersionStatus? status)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.ListAsync(organizationId, status);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetVersionById(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.GetAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(201)]
    public async Task<ActionResult> CreateDraft([FromBody] CreateStructureVersionDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.CreateDraftAsync(organizationId, userId, GetUserName(), dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return CreatedAtAction(nameof(GetVersionById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPost("{id}/submit-review")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> SubmitReview(Guid id, [FromBody] ReviewActionDto? dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.SubmitReviewAsync(organizationId, id, userId, GetUserName(), dto?.Comment);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    [HttpPost("{id}/approve")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Approve(Guid id, [FromBody] ReviewActionDto? dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.ApproveAsync(organizationId, id, userId, GetUserName(), dto?.Comment);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> Reject(Guid id, [FromBody] ReviewActionDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.RejectAsync(organizationId, id, userId, GetUserName(), dto?.Comment);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return NoContent();
    }

    [HttpPost("{id}/publish")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> Publish(Guid id, [FromBody] PublishVersionDto dto)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.PublishAsync(organizationId, id, userId, GetUserName(), dto);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        // Tras publicar, registramos/actualizamos el directorio del tenant (desde los claims) para que
        // el organigrama público pueda resolverse por slug sin autenticación.
        var (orgName, orgAcronym) = GetOrganizationProfile();
        await _organizationDirectoryService.UpsertAsync(organizationId, orgName, orgAcronym);

        return Ok(result.Value);
    }

    [HttpGet("{id}/org-chart")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetVersionOrgChart(
        Guid id,
        [FromQuery] Guid? rootUnitId,
        [FromQuery] int? maxLevel,
        [FromQuery] bool includePositions = false)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _orgChartService.GetByVersionIdAsync(
            organizationId, id, rootUnitId, maxLevel, includePositions, requirePublic: false);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("as-of")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetAsOf(
        [FromQuery] DateOnly date,
        [FromQuery] Guid? rootUnitId,
        [FromQuery] int? maxLevel,
        [FromQuery] bool includePositions = false)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _orgChartService.GetAsOfAsync(
            organizationId, date, rootUnitId, maxLevel, includePositions, requirePublic: false);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("compare")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> Compare(
        [FromQuery] Guid? fromVersionId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] Guid? toVersionId,
        [FromQuery] DateOnly? toDate)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _versionService.CompareAsync(organizationId, fromVersionId, fromDate, toVersionId, toDate);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id}/consolidator-impact")]
    [ProducesResponseType(200)]
    public async Task<ActionResult<StructureVersionImpactReportDto>> GetConsolidatorImpact(Guid id)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var result = await _impactService.AnalyzeVersionImpactAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }
}
