using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Calendario;
using PDS.Filters;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

/// <summary>
/// RF-LO-40: «qué puedo gestionar». <c>GET /api/v1/localidades/calendario/gestionables</c> devuelve las localidades cuyo calendario
/// puede mantener quien llama. Va en su propio controlador porque la ruta no lleva el id de la localidad (el segmento literal
/// <c>calendario</c> no choca con <c>localidades/{id:guid}/calendario</c>). Detrás de <c>Calendario:Habilitado</c>.
/// </summary>
[Authorize]
[ApiController]
[Route("localidades/calendario/gestionables")]
[Produces("application/json")]
[RequireCalendarioHabilitado]
public class LocalidadesGestionablesController(ICalendarioLaboralService laboral) : ApiControllerBase
{
    private static readonly string[] TokenError = ["No se pudo obtener userId y organizationId del token."];

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocalidadGestionableDto>), 200)]
    public async Task<ActionResult> Get(CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await laboral.ListarLocalidadesGestionablesAsync(actor, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }
}
