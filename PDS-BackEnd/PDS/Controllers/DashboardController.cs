using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Reporting;
using PDS.Service.Services;

namespace PDS.Controllers;

// RF-RE-07: dashboard de indicadores definicionales del contexto seleccionado, en un solo payload.
[Authorize]
[ApiController]
[Route("dashboard")]
[Produces("application/json")]
public class DashboardController : ApiControllerBase
{
    private readonly ILogger<DashboardController> _logger;
    private readonly IDashboardService _dashboardService;
    private static readonly string[] tokenError = ["No se pudo obtener userId y organizationId del token."];

    public DashboardController(ILogger<DashboardController> logger, IDashboardService dashboardService)
    {
        _logger = logger;
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult> GetDashboard(
        [FromQuery] Guid? versionId,
        [FromQuery] DateOnly? asOfDate)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = tokenError });

        var context = new ReportingContextDto { VersionId = versionId, AsOfDate = asOfDate };
        var result = await _dashboardService.GetAsync(organizationId, context, GetUserName());
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }
}
