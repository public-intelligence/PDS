using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDS.Domain.DTOs.Calendario;
using PDS.Filters;
using PDS.Service.Services;
using PDS.Service.Services.Calendario;

namespace PDS.Controllers;

/// <summary>
/// RF-LO-27 / RF-LO-28 / RF-LO-40: calendarios de feriados compartidos y sus entradas. Detrás del flag
/// <c>Calendario:Habilitado</c> (404 si está apagado). Las escrituras de cabecera son de administrador de calendario; las de entradas,
/// de administrador o del gestor de la localidad gestora (<see cref="PDS.Service.Authorization.ICalendarioAccessGuard"/>).
/// </summary>
[Authorize]
[ApiController]
[Route("catalogos/calendarios-feriados")]
[Produces("application/json")]
[RequireCalendarioHabilitado]
public class CalendariosFeriadosController(ICalendarioFeriadosService service, IPaginationService paginationService) : ApiControllerBase
{
    private static readonly string[] TokenError = ["No se pudo obtener userId y organizationId del token."];

    // La lista es un arreglo JSON plano; la paginación viaja en los encabezados X-Page-Size, X-Total, etc.
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CalendarioFeriadosListItemDto>), 200)]
    public async Task<ActionResult> GetAll([FromQuery] CalendarioFeriadosFilterDto filter, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.ListAsync(actor, filter, HttpContext, ct);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);

        paginationService.SetPaginationHeaders(Response, result.Value);
        return Ok(result.Value.Value);
    }

    // RF-LO-40: calendarios cuyas entradas puede mantener quien llama.
    [HttpGet("gestionables")]
    [ProducesResponseType(typeof(IEnumerable<CalendarioFeriadosListItemDto>), 200)]
    public async Task<ActionResult> GetGestionables(CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.ListGestionablesAsync(actor, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CalendarioFeriadosDetailDto), 200)]
    public async Task<ActionResult> GetById(Guid id, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.GetAsync(actor, id, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CalendarioFeriadosDetailDto), 201)]
    public async Task<ActionResult> Create([FromBody] UpsertCalendarioFeriadosDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.CreateAsync(actor, dto, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? StatusCode(201, result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CalendarioFeriadosDetailDto), 200)]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpsertCalendarioFeriadosDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.UpdateAsync(actor, id, dto, IfMatch, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.DeleteAsync(actor, id, IfMatch, ct);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    // ───────────────────────────── Entradas (feriados) ─────────────────────────────

    [HttpGet("{id:guid}/dias")]
    [ProducesResponseType(typeof(IEnumerable<FeriadoDiaDto>), 200)]
    public async Task<ActionResult> GetDias(
        Guid id, [FromQuery] int? anio, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.ListDiasAsync(actor, id, anio, desde, hasta, ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode((int)result.StatusCode, result.Error);
    }

    [HttpGet("{id:guid}/dias/{diaId:guid}")]
    [ProducesResponseType(typeof(FeriadoDiaDto), 200)]
    public async Task<ActionResult> GetDia(Guid id, Guid diaId, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.GetDiaAsync(actor, id, diaId, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpPost("{id:guid}/dias")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(FeriadoDiaDto), 201)]
    public async Task<ActionResult> CreateDia(Guid id, [FromBody] UpsertFeriadoDiaDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.CreateDiaAsync(actor, id, dto, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? StatusCode(201, result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    [HttpPut("{id:guid}/dias/{diaId:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(FeriadoDiaDto), 200)]
    public async Task<ActionResult> UpdateDia(Guid id, Guid diaId, [FromBody] UpsertFeriadoDiaDto dto, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.UpdateDiaAsync(actor, id, diaId, dto, IfMatch, ct);
        return ConEtag(result.IsSuccess ? result.Value.ETag : null, () => result.IsSuccess
            ? Ok(result.Value)
            : StatusCode((int)result.StatusCode, result.Error));
    }

    /// <summary><paramref name="motivo"/>: obligatorio cuando un administrador elimina un feriado de una fecha pasada (RF-LO-33).</summary>
    [HttpDelete("{id:guid}/dias/{diaId:guid}")]
    public async Task<ActionResult> DeleteDia(Guid id, Guid diaId, [FromQuery] string? motivo, CancellationToken ct)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.DeleteDiaAsync(actor, id, diaId, motivo, IfMatch, ct);
        return result.IsSuccess ? NoContent() : StatusCode((int)result.StatusCode, result.Error);
    }

    /// <summary>
    /// RF-LO-29: carga masiva atómica de feriados (arreglo JSON). 200 con el informe; 400 con el informe si alguna línea falla
    /// (no se guarda nada). <paramref name="validarSolamente"/> responde siempre 200 con el informe y no guarda.
    /// </summary>
    [HttpPost("{id:guid}/dias/importar")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ImportacionFeriadosResultadoDto), 200)]
    [ProducesResponseType(typeof(ImportacionFeriadosResultadoDto), 400)]
    public async Task<ActionResult> ImportarDias(
        Guid id, [FromBody] List<UpsertFeriadoDiaDto> filas, [FromQuery] ModoImportacionFeriados modo = ModoImportacionFeriados.Agregar,
        [FromQuery] bool validarSolamente = false, [FromQuery] string? motivo = null, CancellationToken ct = default)
    {
        var actor = await TryBuildCalendarioActorAsync();
        if (actor is null) return Unauthorized(new { errors = TokenError });

        var result = await service.ImportDiasAsync(actor, id, filas, modo, validarSolamente, motivo, ct);
        if (!result.IsSuccess) return StatusCode((int)result.StatusCode, result.Error);
        return result.Value.Errores.Count > 0 && !validarSolamente ? BadRequest(result.Value) : Ok(result.Value);
    }

    private string? IfMatch => Request.Headers.IfMatch.Count == 0 ? null : Request.Headers.IfMatch.ToString();

    private ActionResult ConEtag(string? etag, Func<ActionResult> resultado)
    {
        if (!string.IsNullOrEmpty(etag)) Response.Headers.ETag = etag;
        return resultado();
    }
}
