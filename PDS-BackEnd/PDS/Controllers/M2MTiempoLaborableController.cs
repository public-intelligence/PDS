using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Calendario;
using PDS.Filters;
using PDS.Service.Authorization;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

/// <summary>
/// RF-LO-35 (SPEC-LOC-CAL-01 §11.3.6): endpoints M2M de referencia de tiempo laborable (convención v1) para WF y SDS. Mismo
/// control de acceso que las lecturas del calendario: solo cuentas de servicio con <c>access:M2M</c> y <c>localities:calendar:read</c>
/// (RF-LO-39); la organización sale de la ruta y una localidad de otra organización responde 404. Son cálculos, no escrituras: no llevan
/// ETag. Detrás del flag <c>Calendario:Habilitado</c>.
/// </summary>
[Authorize]
[ApiController]
[Route("m2m/organization/{organizationId:int}")]
[Produces("application/json")]
[RequireCalendarioHabilitado]
public class M2MTiempoLaborableController(ITiempoLaborableService tiempo) : ApiControllerBase
{
    /// <summary>
    /// Vencimiento de un plazo (<c>unidad</c>: <c>min</c>, <c>hours</c>, <c>bizdays</c> o <c>caldays</c>). Con <c>items</c> es un lote de
    /// hasta 100 cálculos y cada ítem trae su propio <c>error</c>. 400 por campos inválidos (clave del campo), 404 localidad, 422
    /// <c>horizonte</c> si el vencimiento queda fuera de <c>Calendario:HorizonteCalculoDias</c>.
    /// </summary>
    [HttpPost("calendario/calcular-vencimiento")]
    [ProducesResponseType(typeof(VencimientoDto), 200)]
    [ProducesResponseType(typeof(VencimientoLoteDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<ActionResult> CalcularVencimiento(int organizationId, [FromBody] CalcularVencimientoRequestDto body, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;

        if (body.Items is not null)
        {
            var lote = await tiempo.CalcularVencimientoLoteAsync(organizationId, body.Items, ct);
            return lote.IsSuccess ? Ok(lote.Value) : StatusCode((int)lote.StatusCode, lote.Error);
        }
        var result = await tiempo.CalcularVencimientoAsync(organizationId, body, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    /// <summary>
    /// Minutos laborables en <c>[desde, hasta)</c>. Con <c>items</c> es un lote de hasta 100 cálculos con <c>error</c> por ítem. 400 por
    /// campos inválidos (<c>rango</c> si <c>desde</c> &gt; <c>hasta</c>), 404 localidad, 422 <c>horizonte</c> si el rango supera el horizonte.
    /// </summary>
    [HttpPost("calendario/tiempo-laborable")]
    [ProducesResponseType(typeof(MinutosLaborablesDto), 200)]
    [ProducesResponseType(typeof(MinutosLaborablesLoteDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<ActionResult> TiempoLaborable(int organizationId, [FromBody] TiempoLaborableRequestDto body, CancellationToken ct)
    {
        var check = await RequireM2MServiceAccountAsync(LocalidadPermissions.CalendarRead);
        if (check != null) return check;

        if (body.Items is not null)
        {
            var lote = await tiempo.TiempoLaborableLoteAsync(organizationId, body.Items, ct);
            return lote.IsSuccess ? Ok(lote.Value) : StatusCode((int)lote.StatusCode, lote.Error);
        }
        var result = await tiempo.TiempoLaborableAsync(organizationId, body, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }
}
