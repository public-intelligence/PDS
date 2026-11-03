using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;

namespace PDS.Controllers;

[Authorize]
[ApiController]
[Route("planning-config")]
[Produces("application/json")]
public class PlanningConfigController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IPlanningConfigService _configService;
    private readonly IPlanningContextService _contextService;
    private readonly IPlanningAccessGuard _accessGuard;

    public PlanningConfigController(
        IPlanningConfigService configService,
        IPlanningContextService contextService,
        IPlanningAccessGuard accessGuard)
    {
        _configService = configService;
        _contextService = contextService;
        _accessGuard = accessGuard;
    }

    private static readonly string[] ForbiddenError =
        ["No tienes permisos para consultar o cambiar la configuración de planificación."];

    [HttpGet("area-guardrails")]
    public async Task<ActionResult<PlanningAreaConfigDto>> GetAreaGuardrails(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        // La configuración define qué áreas formulan plan: leerla exige gestionarla o, al
        // menos, revisar POA (Planificación). No es información abierta a cualquier token.
        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage) &&
            !_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.PoaReview))
        {
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = ForbiddenError });
        }

        var result = await _configService.GetAreaConfigAsync(organizationId, ct);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    [HttpPut("area-guardrails")]
    public async Task<ActionResult<PlanningAreaConfigDto>> UpdateAreaGuardrails(
        [FromBody] UpdateAreaGuardrailDto dto, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();
        var contextResult = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);

        if (!contextResult.IsSuccess)
            return StatusCode((int)contextResult.StatusCode, contextResult.Error);

        // Cambiar los guardrails redefine qué áreas deben formular plan: es configuración
        // institucional, no una preferencia de usuario.
        if (!_accessGuard.HasPermission(contextResult.Value, PlanningPermissions.ConfigManage))
            return StatusCode((int)HttpStatusCode.Forbidden, new { errors = ForbiddenError });

        var result = await _configService.UpdateAreaConfigAsync(
            organizationId, dto.MaxLevel, userId, GetUserName(), dto.CustomAreaIds, ct);

        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }
}

public record UpdateAreaGuardrailDto
{
    public int MaxLevel { get; init; } = 2;
    public List<Guid>? CustomAreaIds { get; init; }
}
