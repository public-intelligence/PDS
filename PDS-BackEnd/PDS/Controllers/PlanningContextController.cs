using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Planning;
using PDS.Service.Authorization;
using PDS.Service.Services.Planning;
using System.Security.Claims;

namespace PDS.Controllers;

/// <summary>
/// Contexto de planificación del usuario autenticado (M1): su área organizacional —resuelta
/// vía claim personnel_id → GOBID Personnel → Position local— y sus permisos efectivos.
/// El frontend lo carga una vez tras login; es la base de los filtros por área del módulo.
/// </summary>
[Authorize]
[ApiController]
[Route("planning-context")]
[Produces("application/json")]
public class PlanningContextController : ApiControllerBase
{
    private static readonly string[] MissingClaimsError =
        ["No se pudo obtener userId y organizationId del token."];

    private readonly IPlanningContextService _contextService;

    public PlanningContextController(IPlanningContextService contextService)
    {
        _contextService = contextService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<PlanningContextDto>> GetMyContext(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        var permissions = await GetPermissionFromTokenAsync();

        var result = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }

    /// <summary>
    /// Catálogo de códigos que PDS reconoce, marcando cuáles tiene el usuario. Permite pedir a
    /// Ciberseguridad el código exacto sin adivinarlo, y verificar lo concedido antes de
    /// activar los filtros por área.
    /// </summary>
    [HttpGet("me/permission-catalog")]
    public async Task<ActionResult> GetPermissionCatalog()
    {
        var permissions = await GetPermissionFromTokenAsync() ?? [];
        var held = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);
        var isAdmin = held.Contains(PlanningPermissions.Admin);

        return Ok(PlanningPermissions.All
            .Select(code => new
            {
                code,
                // planning:admin salta todos los chequeos: se refleja para no marcar como
                // faltante algo que el usuario sí puede ejercer.
                granted = isAdmin || held.Contains(code),
                viaAdminWildcard = isAdmin && !held.Contains(code),
            })
            .ToArray());
    }

    /// <summary>Invalida el contexto cacheado y lo vuelve a resolver (p. ej. tras una reestructuración).</summary>
    [HttpPost("me/refresh")]
    public async Task<ActionResult<PlanningContextDto>> RefreshMyContext(CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out var userId, out var organizationId))
            return Unauthorized(new { errors = MissingClaimsError });

        _contextService.InvalidateContext(organizationId, userId, GetPersonnelId());

        var permissions = await GetPermissionFromTokenAsync();

        var result = await _contextService.GetContextAsync(
            organizationId, userId, GetUserName(), GetPersonnelId(), permissions, ct);
        if (!result.IsSuccess)
            return StatusCode((int)result.StatusCode, result.Error);

        return Ok(result.Value);
    }
}
