using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDS.Service.Services;
using PDS.Service.Settings;

namespace PDS.Controllers;

/// <summary>
/// Organigrama público (RF-OG-01/04). Anónimo y de solo lectura: expone únicamente versiones
/// publicadas y marcadas como públicas (Status Published/Superseded + IsPublic). La organización se
/// resuelve desde configuración, con override opcional vía ?organizationId.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("public/org-chart")]
[Produces("application/json")]
public class PublicOrgChartController : ControllerBase
{
    private readonly ILogger<PublicOrgChartController> _logger;
    private readonly IOrgChartService _orgChartService;
    private readonly IOrganizationDirectoryService _organizationDirectoryService;
    private readonly int _defaultOrganizationId;

    public PublicOrgChartController(
        ILogger<PublicOrgChartController> logger,
        IOrgChartService orgChartService,
        IOrganizationDirectoryService organizationDirectoryService,
        IOptions<PublicOrgChartSettings> settings)
    {
        _logger = logger;
        _orgChartService = orgChartService;
        _organizationDirectoryService = organizationDirectoryService;
        _defaultOrganizationId = settings.Value.DefaultOrganizationId;
    }

    private int ResolveOrganizationId(int? organizationId) => organizationId ?? _defaultOrganizationId;

    /// <summary>Resuelve una organización pública por slug → { organizationId, name, acronym, slug }.</summary>
    [HttpGet("organization/{slug}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetOrganization(string slug)
    {
        var result = await _organizationDirectoryService.ResolveBySlugAsync(slug);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("current")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetCurrent(
        [FromQuery] int? organizationId,
        [FromQuery] Guid? rootUnitId,
        [FromQuery] int? maxLevel,
        [FromQuery] bool includePositions = false)
    {
        var result = await _orgChartService.GetCurrentAsync(
            ResolveOrganizationId(organizationId), rootUnitId, maxLevel, includePositions, requirePublic: true);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("as-of")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetAsOf(
        [FromQuery] DateOnly date,
        [FromQuery] int? organizationId,
        [FromQuery] Guid? rootUnitId,
        [FromQuery] int? maxLevel,
        [FromQuery] bool includePositions = false)
    {
        var result = await _orgChartService.GetAsOfAsync(
            ResolveOrganizationId(organizationId), date, rootUnitId, maxLevel, includePositions, requirePublic: true);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("versions")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetVersions([FromQuery] int? organizationId)
    {
        var result = await _orgChartService.ListPublicVersionsAsync(ResolveOrganizationId(organizationId));
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("versions/{number:int}")]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetByNumber(
        int number,
        [FromQuery] int? organizationId,
        [FromQuery] Guid? rootUnitId,
        [FromQuery] int? maxLevel,
        [FromQuery] bool includePositions = false)
    {
        var result = await _orgChartService.GetByNumberAsync(
            ResolveOrganizationId(organizationId), number, rootUnitId, maxLevel, includePositions, requirePublic: true);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }
}
