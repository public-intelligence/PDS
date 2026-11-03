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
[Route("unified-codes")]
[Produces("application/json")]
public class UnifiedCodesController : ApiControllerBase
{
    private readonly IUnifiedCodeService _service;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _guard;

    public UnifiedCodesController(
        IUnifiedCodeService service,
        IPlanningContextService contextService,
        IPlanningAccessGuard guard)
    {
        _service = service;
        _contextService = contextService;
        _guard = guard;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UnifiedCodeDto>>> List(
        [FromQuery] string? level, [FromQuery] Guid? parentId,
        [FromQuery] string? q, [FromQuery] bool? enabled,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogRead);
        if (m2mCheck is not null) return m2mCheck;
        
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });
        
        var result = await _service.ListAsync(organizationId, level, parentId, q, enabled, pageNumber, pageSize);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnifiedCodeDetailDto>> GetById(Guid id)
    {
        var m2mCheck = await ValidateM2MPermissionsAsync(PlanningPermissions.CatalogRead);
        if (m2mCheck is not null) return m2mCheck;
        
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId))
            return Unauthorized(new { errors = new[] { "Claims requeridos no encontrados." } });
        
        var result = await _service.GetByIdAsync(organizationId, id);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);
        return Ok(result.Value);
    }
}
