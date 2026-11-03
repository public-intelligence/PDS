using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Calendario;
using PDS.Filters;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

/// <summary>
/// Calendario laboral de una localidad (SPEC-LOC-CAL-01 §11.2): definición, versiones de la jornada semanal, suscripciones a
/// calendarios de feriados y excepciones. Detrás del flag <c>Calendario:Habilitado</c> (404 si está apagado). El tenant sale del
/// token; una localidad de otra organización responde 404.
/// </summary>
[Authorize]
[ApiController]
[Route("localidades/{id:guid}/calendario")]
[Produces("application/json")]
[RequireCalendarioHabilitado]
public class LocalidadCalendarioController(ICalendarioLaboralService laboral, ICalendarioExcepcionesService excepciones) : ApiControllerBase
{
    private static readonly string[] TokenError = ["No se pudo obtener userId y organizationId del token."];

    // RF-LO-24 a 31: definición del calendario (zona horaria, versiones de jornada y suscripciones).
    [HttpGet]
    [ProducesResponseType(typeof(LocalidadCalendarioDto), 200)]
    public async Task<ActionResult> GetCalendario(Guid id, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await laboral.GetCalendarioAsync(actor, id, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    // RF-LO-25: nueva versión de la jornada semanal.
    [HttpPost("jornadas")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JornadaCreadaDto), 201)]
    public async Task<ActionResult> CrearJornada(Guid id, [FromBody] CrearJornadaDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await laboral.CrearJornadaAsync(actor, id, dto, ct);
        return result.IsSuccess ? StatusCode(201, result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    // RF-LO-25: solo se eliminan versiones que todavía no entraron en vigor (409 si ya rige o ya rigió).
    [HttpDelete("jornadas/{jornadaId:guid}")]
    public async Task<ActionResult> EliminarJornada(Guid id, Guid jornadaId, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await laboral.EliminarJornadaAsync(actor, id, jornadaId, ct);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    /// <summary>
    /// RF-LO-30: reemplaza el conjunto de calendarios de feriados de la localidad. El cuerpo es la lista completa; <c>motivo</c>
    /// (query) es obligatorio cuando un administrador cambia fechas pasadas (RF-LO-33).
    /// </summary>
    [HttpPut("suscripciones")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SuscripcionesActualizadasDto), 200)]
    public async Task<ActionResult> ReemplazarSuscripciones(
        Guid id, [FromBody] List<SuscripcionInputDto>? suscripciones, [FromQuery] string? motivo, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await laboral.ReemplazarSuscripcionesAsync(actor, id, suscripciones, motivo, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    /// <summary>
    /// RF-LO-36: marca la localidad como la predeterminada de calendario de la organización (la anterior deja de serlo). Sin cuerpo ni
    /// <c>If-Match</c>; 204. Solo administradores de calendario (403); 404 si no existe o es de otra organización; 409 si no está ACTIVA.
    /// </summary>
    [HttpPut("predeterminada")]
    [ProducesResponseType(204)]
    public async Task<ActionResult> MarcarPredeterminada(Guid id, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await laboral.MarcarPredeterminadaAsync(actor, id, ct);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    // ───────────────────────────── Excepciones (RF-LO-31) ─────────────────────────────

    // Incluye las excepciones de sistema (suspensión, cierre) con esSistema = true; las de fecha de fin abierta solapan hacia adelante.
    [HttpGet("excepciones")]
    [ProducesResponseType(typeof(IEnumerable<ExcepcionCalendarioDto>), 200)]
    public async Task<ActionResult> GetExcepciones(Guid id, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await excepciones.ListAsync(actor, id, desde, hasta, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("excepciones/{excepcionId:guid}")]
    [ProducesResponseType(typeof(ExcepcionCalendarioDto), 200)]
    public async Task<ActionResult> GetExcepcion(Guid id, Guid excepcionId, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await excepciones.GetAsync(actor, id, excepcionId, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpPost("excepciones")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ExcepcionCalendarioDto), 201)]
    public async Task<ActionResult> CrearExcepcion(Guid id, [FromBody] UpsertExcepcionDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await excepciones.CreateAsync(actor, id, dto, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? StatusCode(201, result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpPut("excepciones/{excepcionId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ExcepcionCalendarioDto), 200)]
    public async Task<ActionResult> ActualizarExcepcion(Guid id, Guid excepcionId, [FromBody] UpsertExcepcionDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await excepciones.UpdateAsync(actor, id, excepcionId, dto, IfMatch, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    /// <summary><paramref name="motivo"/>: obligatorio cuando un administrador elimina una excepción de fechas pasadas (RF-LO-33).</summary>
    [HttpDelete("excepciones/{excepcionId:guid}")]
    public async Task<ActionResult> EliminarExcepcion(Guid id, Guid excepcionId, [FromQuery] string? motivo, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await excepciones.DeleteAsync(actor, id, excepcionId, motivo, IfMatch, ct);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    private string? IfMatch => Request.Headers.IfMatch.Count == 0 ? null : Request.Headers.IfMatch.ToString();

    private ActionResult ConEtag(string? etag, Func<ActionResult> resultado)
    {
        if (!string.IsNullOrEmpty(etag)) Response.Headers.ETag = etag;
        return resultado();
    }
}
