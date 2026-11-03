using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Calendario;
using PDS.Filters;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

/// <summary>
/// Lectura del calendario efectivo de una localidad para usuarios (SPEC-LOC-CAL-01 §11.2, RF-LO-34): calendario efectivo por rango y
/// estado en un instante. Cualquier usuario autenticado de la organización puede leer (§10.4). El tenant sale del token y una
/// localidad de otra organización responde 404. Todas las respuestas llevan <c>ETag</c> y honran <c>If-None-Match</c> (304).
/// Detrás del flag <c>Calendario:Habilitado</c>.
/// </summary>
[Authorize]
[ApiController]
[Route("localidades/{id:guid}/calendario")]
[Produces("application/json")]
[RequireCalendarioHabilitado]
public class LocalidadCalendarioEfectivoController(ICalendarioLecturaService lectura) : ApiControllerBase
{
    private static readonly string[] TokenError = ["No se pudo obtener userId y organizationId del token."];

    /// <summary>Calendario efectivo de <c>desde</c> a <c>hasta</c> (yyyy-MM-dd, ambos obligatorios, máximo <c>Calendario:MaxRangoDias</c> días; 400 clave <c>rango</c>).</summary>
    [HttpGet("efectivo")]
    [ProducesResponseType(typeof(CalendarioEfectivoDto), 200)]
    [ProducesResponseType(304)]
    public async Task<ActionResult> GetEfectivo(Guid id, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId)) return Unauthorized(new { errors = TokenError });
        return ResponderLectura(await lectura.GetEfectivoAsync(organizationId, id, desde, hasta, IfNoneMatchHeader, ct));
    }

    /// <summary>¿Trabaja la localidad en <c>instante</c> (ISO 8601; por defecto ahora)? Incluye el intervalo actual y el próximo instante laborable.</summary>
    [HttpGet("estado")]
    [ProducesResponseType(typeof(EstadoCalendarioDto), 200)]
    [ProducesResponseType(304)]
    public async Task<ActionResult> GetEstado(Guid id, [FromQuery] DateTimeOffset? instante, CancellationToken ct)
    {
        if (!TryGetUserAndOrganizationIds(out _, out var organizationId)) return Unauthorized(new { errors = TokenError });
        return ResponderLectura(await lectura.GetEstadoAsync(organizationId, id, instante, IfNoneMatchHeader, ct));
    }
}
