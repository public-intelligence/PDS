using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Calendario;
using PDS.Filters;
using PDS.Service.Authorization;
using PDS.Service.Services;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

/// <summary>
/// Lecturas M2M del calendario laboral para WF y SDS (SPEC-LOC-CAL-01 §11.2 «M2M endpoints» y spec M2M de SDS #2 y #3).
/// <para>
/// Solo cuentas de servicio con <c>access:M2M</c> y <c>localities:calendar:read</c> (RF-LO-39): un token de usuario recibe 403.
/// La organización sale de la ruta; una localidad de otra organización responde 404. Todas las respuestas llevan <c>ETag</c> y honran
/// <c>If-None-Match</c> (304). Detrás del flag <c>Calendario:Habilitado</c>.
/// </para>
/// <para>
/// Rutas: las de la spec principal cuelgan de <c>/api/v1/m2m/organization/{organizationId}</c> (convención de prefijo del
/// controlador); las dos de SDS (<c>working-hours</c>, <c>calendar</c>) son absolutas como la ya existente
/// <c>/m2m/organization/{organizationId}/locality/{localityId}</c> (M2M-01).
/// </para>
/// </summary>
[Authorize]
[ApiController]
[Route("m2m/organization/{organizationId:int}")]
[Produces("application/json")]
[RequireCalendarioHabilitado]
public class M2MCalendarioController(ICalendarioLecturaService lectura, ICalendarioLaboralService laboral) : ApiControllerBase
{
    // ───────────────────────────── Spec principal (B20) ─────────────────────────────

    /// <summary>Definición del calendario de la localidad (zona horaria, versiones de jornada, suscripciones).</summary>
    [HttpGet("localidades/{id:guid}/calendario")]
    [ProducesResponseType(typeof(LocalidadCalendarioDto), 200)]
    [ProducesResponseType(304)]
    public async Task<ActionResult> GetCalendario(int organizationId, Guid id, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;

        var permisos = await GetPermissionFromTokenAsync() ?? Array.Empty<string>();
        var actor = new CalendarioActor(organizationId, Guid.Empty, GetUserName(), CanalActual, permisos);
        var result = await laboral.GetCalendarioAsync(actor, id, ct);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);

        // La versión del calendario viaja en el ETag de la definición (RF-LO-42).
        var etag = result.Value.ETag;
        if (!string.IsNullOrEmpty(etag))
        {
            Response.Headers.ETag = etag;
            if (ETags.Matches(IfNoneMatchHeader, etag)) return StatusCode(StatusCodes.Status304NotModified);
        }
        return Ok(result.Value);
    }

    /// <summary>RF-LO-36: localidad predeterminada de calendario de la organización (404 si aún no hay ninguna).</summary>
    [HttpGet("calendario/localidad-predeterminada")]
    [ProducesResponseType(typeof(LocalidadPredeterminadaDto), 200)]
    [ProducesResponseType(304)]
    public async Task<ActionResult> GetLocalidadPredeterminada(int organizationId, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;
        return ResponderLectura(await laboral.GetLocalidadPredeterminadaAsync(organizationId, IfNoneMatchHeader, ct));
    }

    [HttpGet("localidades/{id:guid}/calendario/efectivo")]
    [ProducesResponseType(typeof(CalendarioEfectivoDto), 200)]
    [ProducesResponseType(304)]
    public async Task<ActionResult> GetEfectivo(
        int organizationId, Guid id, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;
        return ResponderLectura(await lectura.GetEfectivoAsync(organizationId, id, desde, hasta, IfNoneMatchHeader, ct));
    }

    [HttpGet("localidades/{id:guid}/calendario/estado")]
    [ProducesResponseType(typeof(EstadoCalendarioDto), 200)]
    [ProducesResponseType(304)]
    public async Task<ActionResult> GetEstado(int organizationId, Guid id, [FromQuery] DateTimeOffset? instante, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;
        return ResponderLectura(await lectura.GetEstadoAsync(organizationId, id, instante, IfNoneMatchHeader, ct));
    }

    /// <summary>
    /// RF-LO-43: feed de cambios de la organización posterior a <c>cursor</c> (0 = desde el principio). <c>limite</c> se limita a 1..1000
    /// (por defecto 200). Un cursor cuya fila ya se purgó responde 410 con clave <c>cursor</c>: el consumidor resincroniza por completo.
    /// </summary>
    [HttpGet("calendario/cambios")]
    [ProducesResponseType(typeof(CalendarioCambiosDto), 200)]
    [ProducesResponseType(304)]
    [ProducesResponseType(410)]
    public async Task<ActionResult> GetCambios(int organizationId, [FromQuery] long cursor = 0, [FromQuery] int? limite = null, CancellationToken ct = default)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;
        return ResponderLectura(await lectura.GetCambiosAsync(organizationId, cursor, limite, IfNoneMatchHeader, ct));
    }

    // ───────────────────────────── Spec M2M de SDS (M2M-02) ─────────────────────────────

    /// <summary>
    /// Spec M2M #2: horario semanal vigente en <c>on</c> (yyyy-MM-dd; por defecto hoy en la zona horaria de la localidad). Siempre los
    /// siete días ISO (1 = lunes); un día cerrado lleva <c>intervals: []</c>. 404 con clave <c>jornada</c> si ninguna versión rige ese día.
    /// </summary>
    [HttpGet("/m2m/organization/{organizationId:int}/locality/{localityId:guid}/working-hours")]
    [ProducesResponseType(typeof(WorkingHoursDto), 200)]
    [ProducesResponseType(304)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> GetWorkingHours(
        [FromRoute] int organizationId, [FromRoute] Guid localityId, [FromQuery(Name = "on")] DateOnly? fecha, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;
        return ResponderLectura(await lectura.GetWorkingHoursAsync(organizationId, localityId, fecha, IfNoneMatchHeader, ct));
    }

    /// <summary>
    /// Spec M2M #3: cierres de <c>from</c> a <c>to</c> (yyyy-MM-dd, obligatorios, máximo <c>Calendario:MaxRangoDias</c> días; 400 clave
    /// <c>rango</c>). Ver <c>CierresCalendario</c> para qué es un cierre y cómo se deriva <c>source</c>.
    /// </summary>
    [HttpGet("/m2m/organization/{organizationId:int}/locality/{localityId:guid}/calendar")]
    [ProducesResponseType(typeof(LocalityClosuresDto), 200)]
    [ProducesResponseType(304)]
    [ProducesResponseType(404)]
    public async Task<ActionResult> GetClosures(
        [FromRoute] int organizationId, [FromRoute] Guid localityId,
        [FromQuery(Name = "from")] DateOnly? desde, [FromQuery(Name = "to")] DateOnly? hasta, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;
        return ResponderLectura(await lectura.GetClosuresAsync(organizationId, localityId, desde, hasta, IfNoneMatchHeader, ct));
    }
}
