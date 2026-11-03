using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Services;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

/// <summary>
/// Lectura de la bitácora del módulo de Planificación (M1). La escritura es interna: cada
/// servicio de negocio registra sus acciones vía IPlanningActivityService.
/// </summary>
[Authorize]
[ApiController]
[Route("planning-activity")]
[Produces("application/json")]
public class PlanningActivityController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IPlanningActivityService _activityService;
    private readonly IPaginationService _paginationService;

    public PlanningActivityController(
        IPlanningActivityService activityService,
        IPaginationService paginationService)
    {
        _activityService = activityService;
        _paginationService = paginationService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponseDto<ActivityLogDto>>> GetActivity(
        [FromQuery] PaginationFilterDto pagination,
        [FromQuery] string? module,
        [FromQuery] Guid? entityId)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var result = await _activityService.ListAsync(organizationId, pagination, HttpContext, module, entityId);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        _paginationService.SetPaginationHeaders(Response, result.Value);
        return Ok(result.Value.Value);
    }
}
