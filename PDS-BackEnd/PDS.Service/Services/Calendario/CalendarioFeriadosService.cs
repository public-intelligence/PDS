using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Calendario;
using PDS.Domain.DTOs.Pagination;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Domain.ValueObjects;
using PDS.Service.Authorization;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// Calendarios de feriados compartidos (RF-LO-27) y sus entradas (RF-LO-28). Cada escritura deja, en la MISMA transacción, la
/// bitácora <c>CalendarioFeriadosAuditoria</c> (RF-LO-44) y, si afecta calendarios efectivos, una fila del feed y un incremento de
/// versión por localidad suscrita (RF-LO-43).
/// </summary>
public interface ICalendarioFeriadosService
{
    Task<IHttpResult<PagedResponseDto<CalendarioFeriadosListItemDto>>> ListAsync(
        CalendarioActor actor, CalendarioFeriadosFilterDto filter, HttpContext httpContext, CancellationToken ct = default);

    /// <summary>RF-LO-40: calendarios cuyas entradas puede mantener quien llama.</summary>
    Task<IHttpResult<IReadOnlyList<CalendarioFeriadosListItemDto>>> ListGestionablesAsync(CalendarioActor actor, CancellationToken ct = default);

    Task<IHttpResult<CalendarioFeriadosDetailDto>> GetAsync(CalendarioActor actor, Guid id, CancellationToken ct = default);
    Task<IHttpResult<CalendarioFeriadosDetailDto>> CreateAsync(CalendarioActor actor, UpsertCalendarioFeriadosDto dto, CancellationToken ct = default);
    Task<IHttpResult<CalendarioFeriadosDetailDto>> UpdateAsync(
        CalendarioActor actor, Guid id, UpsertCalendarioFeriadosDto dto, string? ifMatch, CancellationToken ct = default);
    Task<IHttpResult> DeleteAsync(CalendarioActor actor, Guid id, string? ifMatch, CancellationToken ct = default);

    Task<IHttpResult<IReadOnlyList<FeriadoDiaDto>>> ListDiasAsync(
        CalendarioActor actor, Guid calendarioId, int? anio, DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
    Task<IHttpResult<FeriadoDiaDto>> GetDiaAsync(CalendarioActor actor, Guid calendarioId, Guid diaId, CancellationToken ct = default);
    Task<IHttpResult<FeriadoDiaDto>> CreateDiaAsync(CalendarioActor actor, Guid calendarioId, UpsertFeriadoDiaDto dto, CancellationToken ct = default);
    Task<IHttpResult<FeriadoDiaDto>> UpdateDiaAsync(
        CalendarioActor actor, Guid calendarioId, Guid diaId, UpsertFeriadoDiaDto dto, string? ifMatch, CancellationToken ct = default);
    Task<IHttpResult> DeleteDiaAsync(
        CalendarioActor actor, Guid calendarioId, Guid diaId, string? motivo, string? ifMatch, CancellationToken ct = default);

    /// <summary>
    /// RF-LO-29: carga masiva atómica. Con errores de línea responde éxito con el informe y no guarda nada; con
    /// <paramref name="validarSolamente"/> calcula el resultado sin guardar.
    /// </summary>
    Task<IHttpResult<ImportacionFeriadosResultadoDto>> ImportDiasAsync(
        CalendarioActor actor, Guid calendarioId, IReadOnlyList<UpsertFeriadoDiaDto>? filas, ModoImportacionFeriados modo,
        bool validarSolamente, string? motivo, CancellationToken ct = default);
}

public class CalendarioFeriadosService(
    IUnitOfWork<PDSDbContext> uow,
    IPaginationService paginationService,
    ICalendarioAccessGuard guard,
    ICalendarioCambioService cambios,
    ICalendarioAuditoriaService auditoria,
    IRetroactiveWindowPolicy ventana,
    TimeProvider clock) : ICalendarioFeriadosService
{
    private const int MaxNombre = 250;
    private const int MaxCodigo = 50;
    private const int MaxDescripcion = 1000;
    private const int MaxReferencia = 500;
    private const int MaxNotas = 1000;
    private const int PageSizeMax = 100;
    private const int MaxFilasImportacion = 2000;

    private PDSDbContext Ctx => uow.Context;
    private DateTime Ahora => clock.GetUtcNow().UtcDateTime;

    // ───────────────────────────── Calendarios ─────────────────────────────

    public async Task<IHttpResult<PagedResponseDto<CalendarioFeriadosListItemDto>>> ListAsync(
        CalendarioActor actor, CalendarioFeriadosFilterDto filter, HttpContext httpContext, CancellationToken ct = default)
    {
        var query = Ctx.CalendariosFeriados.AsNoTracking().Where(c => c.OrganizationId == actor.OrganizationId && c.DeletedAt == null);
        if (filter.PaisId is { } paisId) query = query.Where(c => c.PaisId == paisId);
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q.Trim().ToUpper();
            query = query.Where(c => c.Codigo.ToUpper().Contains(q) || c.Nombre.ToUpper().Contains(q));
        }

        var total = await query.CountAsync(ct);
        var page = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var size = filter.PageSize is < 1 or > PageSizeMax ? 20 : filter.PageSize;

        var filas = await Proyectar(query.OrderBy(c => c.Nombre).ThenBy(c => c.Codigo).Skip((page - 1) * size).Take(size)).ToListAsync(ct);
        var items = new List<CalendarioFeriadosListItemDto>(filas.Count);
        foreach (var fila in filas) items.Add(await MapItemAsync(actor, fila, ct));

        var paged = paginationService.BuildPaginationUrl(httpContext, page, size, total, items);
        return HttpResult<PagedResponseDto<CalendarioFeriadosListItemDto>>.Success(paged);
    }

    public async Task<IHttpResult<IReadOnlyList<CalendarioFeriadosListItemDto>>> ListGestionablesAsync(
        CalendarioActor actor, CancellationToken ct = default)
    {
        var query = Ctx.CalendariosFeriados.AsNoTracking().Where(c => c.OrganizationId == actor.OrganizationId && c.DeletedAt == null);
        var filas = await Proyectar(query.OrderBy(c => c.Nombre).ThenBy(c => c.Codigo)).ToListAsync(ct);

        var items = new List<CalendarioFeriadosListItemDto>();
        foreach (var fila in filas)
        {
            var item = await MapItemAsync(actor, fila, ct);
            if (item.PuedeGestionar) items.Add(item);
        }
        return HttpResult<IReadOnlyList<CalendarioFeriadosListItemDto>>.Success(items);
    }

    public async Task<IHttpResult<CalendarioFeriadosDetailDto>> GetAsync(CalendarioActor actor, Guid id, CancellationToken ct = default)
    {
        var detalle = await CargarDetalleAsync(actor, id, ct);
        return detalle is null
            ? NoEncontrado<CalendarioFeriadosDetailDto>("Calendario de feriados")
            : HttpResult<CalendarioFeriadosDetailDto>.Success(detalle);
    }

    public async Task<IHttpResult<CalendarioFeriadosDetailDto>> CreateAsync(
        CalendarioActor actor, UpsertCalendarioFeriadosDto dto, CancellationToken ct = default)
    {
        if (!guard.CanManage(actor.Permissions)) return CalendarioResults.SinPermiso<CalendarioFeriadosDetailDto>();

        var codigo = dto.Codigo?.Trim();
        var error = await ValidarCabeceraAsync<CalendarioFeriadosDetailDto>(actor, dto, codigo, ct);
        if (error is not null) return error;

        var duplicado = await Ctx.CalendariosFeriados.AnyAsync(
            c => c.OrganizationId == actor.OrganizationId && c.DeletedAt == null && c.Codigo.ToUpper() == codigo!.ToUpper(), ct);
        if (duplicado)
            return CalendarioResults.Fail<CalendarioFeriadosDetailDto>("Código duplicado", "codigo",
                $"Ya existe un calendario de feriados con el código «{codigo}» en la organización.", HttpStatusCode.BadRequest);

        var ahora = Ahora;
        var entity = new CalendarioFeriados
        {
            Id = Guid.NewGuid(),
            OrganizationId = actor.OrganizationId,
            Codigo = codigo!,
            Nombre = dto.Nombre!.Trim(),
            Descripcion = LimpiarTexto(dto.Descripcion),
            PaisId = dto.PaisId,
            Ambito = dto.Ambito!.Value,
            LocalidadGestoraId = dto.LocalidadGestoraId,
            Activo = dto.Activo ?? true,
            CreatedAt = ahora,
            UpdatedAt = ahora,
        };
        Ctx.CalendariosFeriados.Add(entity);
        auditoria.RegistrarFeriados(actor.OrganizationId, entity.Id, CalendarioFeriadosAcciones.Created, actor.UserId, actor.UserName,
            actor.Canal, detalle: AuditDiff.Compute(null, entity));

        var guardado = await GuardarAsync(ct);
        if (guardado is not null) return Reencauzar<CalendarioFeriadosDetailDto>(guardado);

        var detalle = (await CargarDetalleAsync(actor, entity.Id, ct))!;
        return HttpResult<CalendarioFeriadosDetailDto>.Success(detalle, HttpStatusCode.Created);
    }

    public async Task<IHttpResult<CalendarioFeriadosDetailDto>> UpdateAsync(
        CalendarioActor actor, Guid id, UpsertCalendarioFeriadosDto dto, string? ifMatch, CancellationToken ct = default)
    {
        if (!guard.CanManage(actor.Permissions)) return CalendarioResults.SinPermiso<CalendarioFeriadosDetailDto>();

        var entity = await Ctx.CalendariosFeriados.FirstOrDefaultAsync(
            c => c.Id == id && c.OrganizationId == actor.OrganizationId && c.DeletedAt == null, ct);
        if (entity is null) return NoEncontrado<CalendarioFeriadosDetailDto>("Calendario de feriados");
        if (!ETags.IfMatchSatisfied(ifMatch, ETags.FromRowVersion(entity.RowVersion)))
            return CalendarioResults.Concurrencia<CalendarioFeriadosDetailDto>();

        // RF-LO-27 / UI-09: el código es referencia de integraciones; no se cambia tras crear el calendario.
        var codigo = dto.Codigo?.Trim();
        if (!string.IsNullOrEmpty(codigo) && !string.Equals(codigo, entity.Codigo, StringComparison.OrdinalIgnoreCase))
            return CalendarioResults.Fail<CalendarioFeriadosDetailDto>("Código no modificable", "codigo",
                "El código del calendario no se puede modificar después de crearlo.", HttpStatusCode.BadRequest);

        var error = await ValidarCabeceraAsync<CalendarioFeriadosDetailDto>(actor, dto, entity.Codigo, ct);
        if (error is not null) return error;

        var antes = AuditDiff.Snapshot(entity);
        entity.Nombre = dto.Nombre!.Trim();
        entity.Descripcion = LimpiarTexto(dto.Descripcion);
        entity.PaisId = dto.PaisId;
        entity.Ambito = dto.Ambito!.Value;
        entity.LocalidadGestoraId = dto.LocalidadGestoraId;
        if (dto.Activo is { } activo) entity.Activo = activo;

        var diff = AuditDiff.Compute(antes, AuditDiff.Snapshot(entity));
        if (diff is not null)
        {
            entity.UpdatedAt = Ahora;
            auditoria.RegistrarFeriados(actor.OrganizationId, entity.Id, CalendarioFeriadosAcciones.Updated, actor.UserId, actor.UserName,
                actor.Canal, detalle: diff);

            // Los campos que se muestran en el calendario efectivo (nombre, país, ámbito, activo) cambian la versión de los suscriptores.
            if (CambiaCalendarioEfectivo(antes, entity))
                await cambios.RegistrarSuscriptoresAsync(entity.Id, ct: ct);
        }

        var guardado = await GuardarAsync(ct);
        if (guardado is not null) return Reencauzar<CalendarioFeriadosDetailDto>(guardado);

        return HttpResult<CalendarioFeriadosDetailDto>.Success((await CargarDetalleAsync(actor, entity.Id, ct))!);
    }

    public async Task<IHttpResult> DeleteAsync(CalendarioActor actor, Guid id, string? ifMatch, CancellationToken ct = default)
    {
        if (!guard.CanManage(actor.Permissions)) return CalendarioResults.SinPermiso();

        var entity = await Ctx.CalendariosFeriados.FirstOrDefaultAsync(
            c => c.Id == id && c.OrganizationId == actor.OrganizationId && c.DeletedAt == null, ct);
        if (entity is null) return NoEncontrado("Calendario de feriados");
        if (!ETags.IfMatchSatisfied(ifMatch, ETags.FromRowVersion(entity.RowVersion))) return CalendarioResults.Concurrencia();

        // RF-LO-27 AC2: no se elimina mientras alguna localidad lo use; la respuesta lista a los suscriptores.
        var suscriptores = await Ctx.LocalidadCalendariosFeriados.AsNoTracking()
            .Where(s => s.CalendarioFeriadosId == id && s.Localidad!.DeletedAt == null)
            .OrderBy(s => s.Localidad!.Codigo)
            .Select(s => s.Localidad!.Codigo + " · " + s.Localidad!.NombreOficial)
            .ToListAsync(ct);
        if (suscriptores.Count > 0)
            return CalendarioResults.FailMany("No se puede eliminar el calendario", "suscriptores",
                [$"No se puede eliminar: lo usan {suscriptores.Count} localidades.", .. suscriptores], HttpStatusCode.Conflict);

        entity.DeletedAt = Ahora;
        entity.UpdatedAt = entity.DeletedAt.Value;
        auditoria.RegistrarFeriados(actor.OrganizationId, entity.Id, CalendarioFeriadosAcciones.Deleted, actor.UserId, actor.UserName,
            actor.Canal, detalle: AuditDiff.Compute(new { entity.Codigo, entity.Nombre }, null));

        var guardado = await GuardarAsync(ct);
        return guardado is not null ? Reencauzar(guardado) : HttpResult.Success();
    }

    // ───────────────────────────── Entradas (feriados) ─────────────────────────────

    public async Task<IHttpResult<IReadOnlyList<FeriadoDiaDto>>> ListDiasAsync(
        CalendarioActor actor, Guid calendarioId, int? anio, DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        if (!await ExisteCalendarioAsync(actor, calendarioId, ct)) return NoEncontrado<IReadOnlyList<FeriadoDiaDto>>("Calendario de feriados");

        var query = Ctx.CalendarioFeriadosDias.AsNoTracking().Where(d => d.CalendarioFeriadosId == calendarioId && d.DeletedAt == null);
        if (anio is { } a)
        {
            var inicio = new DateOnly(a, 1, 1);
            var fin = new DateOnly(a, 12, 31);
            query = query.Where(d => d.Fecha >= inicio && d.Fecha <= fin);
        }
        if (desde is { } d1) query = query.Where(d => d.Fecha >= d1);
        if (hasta is { } h1) query = query.Where(d => d.Fecha <= h1);

        var dias = await query.OrderBy(d => d.Fecha).ToListAsync(ct);
        return HttpResult<IReadOnlyList<FeriadoDiaDto>>.Success(dias.Select(MapDia).ToList());
    }

    public async Task<IHttpResult<FeriadoDiaDto>> GetDiaAsync(CalendarioActor actor, Guid calendarioId, Guid diaId, CancellationToken ct = default)
    {
        if (!await ExisteCalendarioAsync(actor, calendarioId, ct)) return NoEncontrado<FeriadoDiaDto>("Calendario de feriados");
        var dia = await Ctx.CalendarioFeriadosDias.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == diaId && d.CalendarioFeriadosId == calendarioId && d.DeletedAt == null, ct);
        return dia is null ? NoEncontrado<FeriadoDiaDto>("Feriado") : HttpResult<FeriadoDiaDto>.Success(MapDia(dia));
    }

    public async Task<IHttpResult<FeriadoDiaDto>> CreateDiaAsync(
        CalendarioActor actor, Guid calendarioId, UpsertFeriadoDiaDto dto, CancellationToken ct = default)
    {
        var calendario = await CargarCalendarioAsync(actor, calendarioId, ct);
        if (calendario is null) return NoEncontrado<FeriadoDiaDto>("Calendario de feriados");
        if (!await PuedeGestionarEntradasAsync(actor, calendario, ct)) return CalendarioResults.SinPermiso<FeriadoDiaDto>();

        var parseo = ValidarDia(dto);
        if (parseo.Error is not null) return parseo.Error;

        if (await ExisteFechaAsync(calendarioId, parseo.Fecha, null, ct))
            return DuplicadoFecha<FeriadoDiaDto>(parseo.Fecha);

        var retro = await EvaluarVentanaAsync<FeriadoDiaDto>(actor, calendario, [parseo.Fecha], dto.Motivo, ct);
        if (retro is not null) return retro;

        var ahora = Ahora;
        var dia = new CalendarioFeriadosDia
        {
            Id = Guid.NewGuid(),
            CalendarioFeriadosId = calendarioId,
            Fecha = parseo.Fecha,
            Nombre = dto.Nombre!.Trim(),
            Alcance = parseo.Alcance,
            Intervalos = parseo.Intervalos,
            ReferenciaNormativa = LimpiarTexto(dto.ReferenciaNormativa),
            Notas = LimpiarTexto(dto.Notas),
            CreatedAt = ahora,
            UpdatedAt = ahora,
        };
        Ctx.CalendarioFeriadosDias.Add(dia);
        auditoria.RegistrarFeriados(actor.OrganizationId, calendarioId, CalendarioFeriadosAcciones.DiaAdded, actor.UserId, actor.UserName,
            actor.Canal, LimpiarTexto(dto.Motivo), AuditDiff.Compute(null, dia));
        await cambios.RegistrarSuscriptoresAsync(calendarioId, parseo.Fecha, parseo.Fecha, ct);

        var guardado = await GuardarAsync(ct);
        if (guardado is not null) return Reencauzar<FeriadoDiaDto>(guardado);

        var guardada = await Ctx.CalendarioFeriadosDias.AsNoTracking().FirstAsync(d => d.Id == dia.Id, ct);
        return HttpResult<FeriadoDiaDto>.Success(MapDia(guardada), HttpStatusCode.Created);
    }

    public async Task<IHttpResult<FeriadoDiaDto>> UpdateDiaAsync(
        CalendarioActor actor, Guid calendarioId, Guid diaId, UpsertFeriadoDiaDto dto, string? ifMatch, CancellationToken ct = default)
    {
        var calendario = await CargarCalendarioAsync(actor, calendarioId, ct);
        if (calendario is null) return NoEncontrado<FeriadoDiaDto>("Calendario de feriados");
        if (!await PuedeGestionarEntradasAsync(actor, calendario, ct)) return CalendarioResults.SinPermiso<FeriadoDiaDto>();

        var dia = await Ctx.CalendarioFeriadosDias.FirstOrDefaultAsync(
            d => d.Id == diaId && d.CalendarioFeriadosId == calendarioId && d.DeletedAt == null, ct);
        if (dia is null) return NoEncontrado<FeriadoDiaDto>("Feriado");
        if (!ETags.IfMatchSatisfied(ifMatch, ETags.FromRowVersion(dia.RowVersion))) return CalendarioResults.Concurrencia<FeriadoDiaDto>();

        var parseo = ValidarDia(dto);
        if (parseo.Error is not null) return parseo.Error;

        if (await ExisteFechaAsync(calendarioId, parseo.Fecha, diaId, ct))
            return DuplicadoFecha<FeriadoDiaDto>(parseo.Fecha);

        var fechaAnterior = dia.Fecha;
        var retro = await EvaluarVentanaAsync<FeriadoDiaDto>(actor, calendario, [fechaAnterior, parseo.Fecha], dto.Motivo, ct);
        if (retro is not null) return retro;

        var antes = AuditDiff.Snapshot(dia);
        dia.Fecha = parseo.Fecha;
        dia.Nombre = dto.Nombre!.Trim();
        dia.Alcance = parseo.Alcance;
        dia.Intervalos = parseo.Intervalos;
        dia.ReferenciaNormativa = LimpiarTexto(dto.ReferenciaNormativa);
        dia.Notas = LimpiarTexto(dto.Notas);

        var diff = AuditDiff.Compute(antes, AuditDiff.Snapshot(dia));
        if (diff is not null)
        {
            dia.UpdatedAt = Ahora;
            auditoria.RegistrarFeriados(actor.OrganizationId, calendarioId, CalendarioFeriadosAcciones.DiaUpdated, actor.UserId,
                actor.UserName, actor.Canal, LimpiarTexto(dto.Motivo), diff);
            await cambios.RegistrarSuscriptoresAsync(calendarioId, Min(fechaAnterior, parseo.Fecha), Max(fechaAnterior, parseo.Fecha), ct);
        }

        var guardado = await GuardarAsync(ct);
        if (guardado is not null) return Reencauzar<FeriadoDiaDto>(guardado);

        var guardada = await Ctx.CalendarioFeriadosDias.AsNoTracking().FirstAsync(d => d.Id == diaId, ct);
        return HttpResult<FeriadoDiaDto>.Success(MapDia(guardada));
    }

    public async Task<IHttpResult> DeleteDiaAsync(
        CalendarioActor actor, Guid calendarioId, Guid diaId, string? motivo, string? ifMatch, CancellationToken ct = default)
    {
        var calendario = await CargarCalendarioAsync(actor, calendarioId, ct);
        if (calendario is null) return NoEncontrado("Calendario de feriados");
        if (!await PuedeGestionarEntradasAsync(actor, calendario, ct)) return CalendarioResults.SinPermiso();

        var dia = await Ctx.CalendarioFeriadosDias.FirstOrDefaultAsync(
            d => d.Id == diaId && d.CalendarioFeriadosId == calendarioId && d.DeletedAt == null, ct);
        if (dia is null) return NoEncontrado("Feriado");
        if (!ETags.IfMatchSatisfied(ifMatch, ETags.FromRowVersion(dia.RowVersion))) return CalendarioResults.Concurrencia();

        var retro = await EvaluarVentanaAsync<bool>(actor, calendario, [dia.Fecha], motivo, ct);
        if (retro is not null) return retro;

        var detalle = AuditDiff.Compute(dia, null);
        dia.DeletedAt = Ahora;
        dia.UpdatedAt = dia.DeletedAt.Value;
        auditoria.RegistrarFeriados(actor.OrganizationId, calendarioId, CalendarioFeriadosAcciones.DiaRemoved, actor.UserId, actor.UserName,
            actor.Canal, LimpiarTexto(motivo), detalle);
        await cambios.RegistrarSuscriptoresAsync(calendarioId, dia.Fecha, dia.Fecha, ct);

        var guardado = await GuardarAsync(ct);
        return guardado is not null ? Reencauzar(guardado) : HttpResult.Success();
    }

    public async Task<IHttpResult<ImportacionFeriadosResultadoDto>> ImportDiasAsync(
        CalendarioActor actor, Guid calendarioId, IReadOnlyList<UpsertFeriadoDiaDto>? filas, ModoImportacionFeriados modo,
        bool validarSolamente, string? motivo, CancellationToken ct = default)
    {
        var calendario = await CargarCalendarioAsync(actor, calendarioId, ct);
        if (calendario is null) return NoEncontrado<ImportacionFeriadosResultadoDto>("Calendario de feriados");
        if (!await PuedeGestionarEntradasAsync(actor, calendario, ct)) return CalendarioResults.SinPermiso<ImportacionFeriadosResultadoDto>();

        if (filas is not { Count: > 0 })
            return CalendarioResults.Fail<ImportacionFeriadosResultadoDto>("Datos inválidos de la importación", "dias",
                "Indique al menos un feriado para importar.", HttpStatusCode.BadRequest);
        if (filas.Count > MaxFilasImportacion)
            return CalendarioResults.Fail<ImportacionFeriadosResultadoDto>("Datos inválidos de la importación", "dias",
                $"La importación no puede superar {MaxFilasImportacion} feriados.", HttpStatusCode.BadRequest);

        // 1. Validación línea por línea (base 1). Cualquier error aborta antes de tocar la base.
        var errores = new List<ErrorImportacionFeriadoDto>();
        var validos = new List<(int Linea, DiaValidado Parseo, UpsertFeriadoDiaDto Dto)>();
        var vistas = new Dictionary<DateOnly, int>();
        for (var i = 0; i < filas.Count; i++)
        {
            var linea = i + 1;
            var dto = filas[i];
            if (dto is null)
            {
                errores.Add(new() { Linea = linea, Clave = "fila", Mensaje = "La línea está vacía." });
                continue;
            }

            var parseo = ValidarDia(dto);
            if (parseo.Error is not null)
            {
                foreach (var (clave, mensajes) in parseo.Error.Error.Detail)
                    foreach (var mensaje in mensajes) errores.Add(new() { Linea = linea, Clave = clave, Mensaje = mensaje });
                continue;
            }
            if (vistas.TryGetValue(parseo.Fecha, out var primera))
            {
                errores.Add(new()
                {
                    Linea = linea, Clave = "fecha",
                    Mensaje = $"La fecha {parseo.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} ya aparece en la línea {primera}.",
                });
                continue;
            }
            vistas[parseo.Fecha] = linea;
            validos.Add((linea, parseo, dto));
        }
        if (errores.Count > 0) return HttpResult<ImportacionFeriadosResultadoDto>.Success(new() { Errores = errores });

        // 2. Plan contra lo existente.
        var fechas = validos.Select(v => v.Parseo.Fecha).ToHashSet();
        var anios = validos.Select(v => v.Parseo.Fecha.Year).ToHashSet();
        var existentes = (await Ctx.CalendarioFeriadosDias
                .Where(d => d.CalendarioFeriadosId == calendarioId && d.DeletedAt == null)
                .ToListAsync(ct))
            .Where(d => fechas.Contains(d.Fecha) || (modo == ModoImportacionFeriados.ReemplazarAnio && anios.Contains(d.Fecha.Year)))
            .ToDictionary(d => d.Fecha);

        var ahora = Ahora;
        var resultado = new ImportacionFeriadosResultadoDto();
        var nuevos = new List<CalendarioFeriadosDia>();
        var actualizaciones = new List<(CalendarioFeriadosDia Dia, CalendarioFeriadosDia Candidato)>();
        var afectadas = new List<DateOnly>();

        foreach (var (_, parseo, dto) in validos)
        {
            var candidato = new CalendarioFeriadosDia
            {
                Id = existentes.TryGetValue(parseo.Fecha, out var actual) ? actual.Id : Guid.NewGuid(),
                CalendarioFeriadosId = calendarioId,
                Fecha = parseo.Fecha,
                Nombre = dto.Nombre!.Trim(),
                Alcance = parseo.Alcance,
                Intervalos = parseo.Intervalos,
                ReferenciaNormativa = LimpiarTexto(dto.ReferenciaNormativa),
                Notas = LimpiarTexto(dto.Notas),
                CreatedAt = ahora,
                UpdatedAt = ahora,
            };

            if (actual is null)
            {
                nuevos.Add(candidato);
                resultado.Creados++;
                afectadas.Add(parseo.Fecha);
            }
            else if (AuditDiff.Compute(AuditDiff.Snapshot(actual), AuditDiff.Snapshot(candidato)) is null)
            {
                resultado.Omitidos++;
            }
            else
            {
                actualizaciones.Add((actual, candidato));
                resultado.Actualizados++;
                afectadas.Add(parseo.Fecha);
            }
        }

        var eliminados = modo == ModoImportacionFeriados.ReemplazarAnio
            ? existentes.Values.Where(d => !fechas.Contains(d.Fecha)).ToList()
            : [];
        resultado.Eliminados = eliminados.Count;
        afectadas.AddRange(eliminados.Select(d => d.Fecha));

        // 3. RF-LO-33: la ventana retroactiva se evalúa sobre todo lo que cambia de verdad.
        if (afectadas.Count > 0)
        {
            var retro = await EvaluarVentanaAsync<ImportacionFeriadosResultadoDto>(actor, calendario, afectadas, motivo, ct);
            if (retro is not null) return retro;
        }

        if (validarSolamente || afectadas.Count == 0) return HttpResult<ImportacionFeriadosResultadoDto>.Success(resultado);

        // 4. Aplicación atómica: una sola bitácora, un solo rango del feed y un solo SaveChanges.
        foreach (var (dia, candidato) in actualizaciones)
        {
            dia.Nombre = candidato.Nombre;
            dia.Alcance = candidato.Alcance;
            dia.Intervalos = candidato.Intervalos;
            dia.ReferenciaNormativa = candidato.ReferenciaNormativa;
            dia.Notas = candidato.Notas;
            dia.UpdatedAt = ahora;
        }
        foreach (var dia in eliminados)
        {
            dia.DeletedAt = ahora;
            dia.UpdatedAt = ahora;
        }
        Ctx.CalendarioFeriadosDias.AddRange(nuevos);

        var detalle = System.Text.Json.JsonSerializer.Serialize(new
        {
            modo = modo.ToString(), resultado.Creados, resultado.Actualizados, resultado.Omitidos, resultado.Eliminados,
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
        auditoria.RegistrarFeriados(actor.OrganizationId, calendarioId, CalendarioFeriadosAcciones.DiasImported, actor.UserId, actor.UserName,
            actor.Canal, LimpiarTexto(motivo), detalle);
        await cambios.RegistrarSuscriptoresAsync(calendarioId, afectadas.Min(), afectadas.Max(), ct);

        var guardado = await GuardarAsync(ct);
        return guardado is not null
            ? Reencauzar<ImportacionFeriadosResultadoDto>(guardado)
            : HttpResult<ImportacionFeriadosResultadoDto>.Success(resultado);
    }

    // ───────────────────────────── Validaciones ─────────────────────────────

    private async Task<IHttpResult<T>?> ValidarCabeceraAsync<T>(CalendarioActor actor, UpsertCalendarioFeriadosDto dto, string? codigo, CancellationToken ct)
    {
        var errores = new Dictionary<string, IEnumerable<string>>();
        if (string.IsNullOrWhiteSpace(codigo)) errores["codigo"] = ["El código es obligatorio."];
        else if (codigo.Length > MaxCodigo) errores["codigo"] = [$"El código no puede superar {MaxCodigo} caracteres."];

        if (string.IsNullOrWhiteSpace(dto.Nombre)) errores["nombre"] = ["El nombre es obligatorio."];
        else if (dto.Nombre.Trim().Length > MaxNombre) errores["nombre"] = [$"El nombre no puede superar {MaxNombre} caracteres."];

        if (dto.Descripcion is { Length: > MaxDescripcion }) errores["descripcion"] = [$"La descripción no puede superar {MaxDescripcion} caracteres."];

        if (dto.Ambito is not { } ambito || !Enum.IsDefined(ambito)) errores["ambito"] = ["El ámbito es obligatorio y debe ser un valor válido."];

        if (dto.PaisId is { } paisId && !await Ctx.Paises.AnyAsync(p => p.Id == paisId, ct))
            errores["paisId"] = ["El país indicado no existe."];

        if (dto.LocalidadGestoraId is { } gestoraId &&
            !await Ctx.Localidades.AnyAsync(l => l.Id == gestoraId && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct))
            errores["localidadGestoraId"] = ["La localidad gestora indicada no existe."];

        return errores.Count == 0
            ? null
            : HttpResult<T>.Failure("Datos inválidos del calendario de feriados", errores, HttpStatusCode.BadRequest);
    }

    /// <summary>Resultado de validar el cuerpo de un feriado.</summary>
    private sealed record DiaValidado(
        IHttpResult<FeriadoDiaDto>? Error, DateOnly Fecha, AlcanceFeriado Alcance, List<IntervaloLaboral>? Intervalos);

    private static DiaValidado ValidarDia(UpsertFeriadoDiaDto dto)
    {
        IHttpResult<FeriadoDiaDto> Fallo(string key, string msg) => CalendarioResults.Fail<FeriadoDiaDto>("Datos inválidos del feriado", key, msg, HttpStatusCode.BadRequest);

        if (dto.Fecha is not { } fecha) return new(Fallo("fecha", "La fecha es obligatoria."), default, default, null);
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return new(Fallo("nombre", "El nombre es obligatorio."), fecha, default, null);
        if (dto.Nombre.Trim().Length > MaxNombre) return new(Fallo("nombre", $"El nombre no puede superar {MaxNombre} caracteres."), fecha, default, null);
        if (dto.Alcance is not { } alcance || !Enum.IsDefined(alcance)) return new(Fallo("alcance", "El alcance es obligatorio (DiaCompleto o JornadaReducida)."), fecha, default, null);
        if (dto.ReferenciaNormativa is { Length: > MaxReferencia }) return new(Fallo("referenciaNormativa", $"La referencia normativa no puede superar {MaxReferencia} caracteres."), fecha, alcance, null);
        if (dto.Notas is { Length: > MaxNotas }) return new(Fallo("notas", $"Las notas no pueden superar {MaxNotas} caracteres."), fecha, alcance, null);

        // RF-LO-28: «intervalos» es obligatorio solo en JornadaReducida (el horario que se trabaja) y no aplica a DiaCompleto.
        if (alcance == AlcanceFeriado.DiaCompleto)
        {
            return dto.Intervalos is { Count: > 0 }
                ? new(Fallo("intervalos", "Un feriado de día completo no lleva intervalos."), fecha, alcance, null)
                : new(null, fecha, alcance, null);
        }

        if (dto.Intervalos is not { Count: > 0 })
            return new(Fallo("intervalos", "Una jornada reducida requiere al menos un intervalo de trabajo."), fecha, alcance, null);

        var parseo = CalendarioMapper.ParseIntervalos(dto.Intervalos);
        if (!parseo.EsValido)
            return new(Fallo("intervalos", parseo.ErrorFormato ?? parseo.ErrorSemantico!), fecha, alcance, null);

        return new(null, fecha, alcance, parseo.Valores.ToList());
    }

    private async Task<IHttpResult<T>?> EvaluarVentanaAsync<T>(
        CalendarioActor actor, CalendarioFeriados calendario, IEnumerable<DateOnly> fechas, string? motivo, CancellationToken ct)
    {
        string? zona = null;
        if (calendario.LocalidadGestoraId is { } gestoraId)
            zona = await Ctx.Localidades.AsNoTracking().Where(l => l.Id == gestoraId).Select(l => l.ZonaHoraria).FirstOrDefaultAsync(ct);

        var evaluacion = ventana.Evaluar(guard.CanManage(actor.Permissions), zona, fechas, motivo);
        return evaluacion.Veredicto switch
        {
            RetroactivoVeredicto.FueraDeVentana => CalendarioResults.Fail<T>("Fuera de la ventana retroactiva", "fecha",
                $"Fuera del plazo para cambios retroactivos ({ventana.VentanaDias} días).", HttpStatusCode.Forbidden),
            RetroactivoVeredicto.RequiereMotivo => CalendarioResults.Fail<T>("Motivo requerido", "motivo",
                "Un cambio sobre fechas pasadas requiere indicar el motivo.", HttpStatusCode.BadRequest),
            _ => null,
        };
    }

    private async Task<bool> PuedeGestionarEntradasAsync(CalendarioActor actor, CalendarioFeriados calendario, CancellationToken ct) =>
        await guard.PuedeGestionarCalendarioFeriadosAsync(
            actor.Permissions, calendario.Id, calendario.LocalidadGestoraId, actor.UserId, actor.OrganizationId, ct);

    private Task<bool> ExisteFechaAsync(Guid calendarioId, DateOnly fecha, Guid? excluirDiaId, CancellationToken ct) =>
        Ctx.CalendarioFeriadosDias.AnyAsync(
            d => d.CalendarioFeriadosId == calendarioId && d.DeletedAt == null && d.Fecha == fecha && (excluirDiaId == null || d.Id != excluirDiaId), ct);

    private static IHttpResult<T> DuplicadoFecha<T>(DateOnly fecha) =>
        CalendarioResults.Fail<T>("Feriado duplicado", "fecha",
            $"Ya existe un feriado el {fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} en este calendario.", HttpStatusCode.Conflict);

    // ───────────────────────────── Carga y mapeo ─────────────────────────────

    private Task<CalendarioFeriados?> CargarCalendarioAsync(CalendarioActor actor, Guid id, CancellationToken ct) =>
        Ctx.CalendariosFeriados.AsNoTracking().FirstOrDefaultAsync(
            c => c.Id == id && c.OrganizationId == actor.OrganizationId && c.DeletedAt == null, ct);

    private Task<bool> ExisteCalendarioAsync(CalendarioActor actor, Guid id, CancellationToken ct) =>
        Ctx.CalendariosFeriados.AnyAsync(c => c.Id == id && c.OrganizationId == actor.OrganizationId && c.DeletedAt == null, ct);

    private sealed class Fila
    {
        public Guid Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public Guid? PaisId { get; init; }
        public string? PaisNombre { get; init; }
        public AmbitoCalendarioFeriados Ambito { get; init; }
        public Guid? LocalidadGestoraId { get; init; }
        public string? LocalidadGestoraNombre { get; init; }
        public bool Activo { get; init; }
        public int Suscritas { get; init; }
        public int AnioActual { get; init; }
        public int AnioSiguiente { get; init; }
        public byte[]? RowVersion { get; init; }
    }

    private IQueryable<Fila> Proyectar(IQueryable<CalendarioFeriados> query)
    {
        var anio = Ahora.Year;
        var inicioActual = new DateOnly(anio, 1, 1);
        var finActual = new DateOnly(anio, 12, 31);
        var inicioSiguiente = new DateOnly(anio + 1, 1, 1);
        var finSiguiente = new DateOnly(anio + 1, 12, 31);
        var suscripciones = Ctx.LocalidadCalendariosFeriados;

        return query.Select(c => new Fila
        {
            Id = c.Id,
            Codigo = c.Codigo,
            Nombre = c.Nombre,
            Descripcion = c.Descripcion,
            PaisId = c.PaisId,
            PaisNombre = c.Pais != null ? c.Pais.Nombre : null,
            Ambito = c.Ambito,
            LocalidadGestoraId = c.LocalidadGestoraId,
            LocalidadGestoraNombre = c.LocalidadGestora != null ? c.LocalidadGestora.NombreOficial : null,
            Activo = c.Activo,
            Suscritas = suscripciones.Count(s => s.CalendarioFeriadosId == c.Id && s.Localidad!.DeletedAt == null),
            AnioActual = c.Dias.Count(d => d.DeletedAt == null && d.Fecha >= inicioActual && d.Fecha <= finActual),
            AnioSiguiente = c.Dias.Count(d => d.DeletedAt == null && d.Fecha >= inicioSiguiente && d.Fecha <= finSiguiente),
            RowVersion = c.RowVersion,
        });
    }

    private async Task<CalendarioFeriadosListItemDto> MapItemAsync(CalendarioActor actor, Fila f, CancellationToken ct)
    {
        var dto = new CalendarioFeriadosListItemDto();
        await RellenarItemAsync(actor, dto, f, ct);
        return dto;
    }

    private async Task RellenarItemAsync(CalendarioActor actor, CalendarioFeriadosListItemDto dto, Fila f, CancellationToken ct)
    {
        dto.Id = f.Id;
        dto.Codigo = f.Codigo;
        dto.Nombre = f.Nombre;
        dto.Descripcion = f.Descripcion;
        dto.PaisId = f.PaisId;
        dto.PaisNombre = f.PaisNombre;
        dto.Ambito = f.Ambito;
        dto.LocalidadGestoraId = f.LocalidadGestoraId;
        dto.LocalidadGestoraNombre = f.LocalidadGestoraNombre;
        dto.Activo = f.Activo;
        dto.LocalidadesSuscritas = f.Suscritas;
        dto.FeriadosAnioActual = f.AnioActual;
        dto.FeriadosAnioSiguiente = f.AnioSiguiente;
        dto.PuedeGestionar = await guard.PuedeGestionarCalendarioFeriadosAsync(
            actor.Permissions, f.Id, f.LocalidadGestoraId, actor.UserId, actor.OrganizationId, ct);
    }

    private async Task<CalendarioFeriadosDetailDto?> CargarDetalleAsync(CalendarioActor actor, Guid id, CancellationToken ct)
    {
        var query = Ctx.CalendariosFeriados.AsNoTracking()
            .Where(c => c.Id == id && c.OrganizationId == actor.OrganizationId && c.DeletedAt == null);
        var fila = await Proyectar(query).FirstOrDefaultAsync(ct);
        if (fila is null) return null;

        var detalle = new CalendarioFeriadosDetailDto
        {
            VentanaRetroactivaDias = ventana.VentanaDias,
            ETag = ETags.FromRowVersion(fila.RowVersion),
        };
        await RellenarItemAsync(actor, detalle, fila, ct);

        detalle.Suscriptores = await Ctx.LocalidadCalendariosFeriados.AsNoTracking()
            .Where(s => s.CalendarioFeriadosId == id && s.Localidad!.DeletedAt == null)
            .OrderBy(s => s.Localidad!.NombreOficial)
            .Select(s => new SuscriptorCalendarioDto
            {
                LocalidadId = s.LocalidadId,
                Codigo = s.Localidad!.Codigo,
                Nombre = s.Localidad.NombreOficial,
                Pais = s.Localidad.Pais != null ? s.Localidad.Pais.Nombre : null,
                VigenteDesde = s.VigenteDesde,
                VigenteHasta = s.VigenteHasta,
            })
            .ToListAsync(ct);
        return detalle;
    }

    private static FeriadoDiaDto MapDia(CalendarioFeriadosDia d) => new()
    {
        Id = d.Id,
        Fecha = d.Fecha,
        Nombre = d.Nombre,
        Alcance = d.Alcance,
        Intervalos = d.Alcance == AlcanceFeriado.JornadaReducida ? CalendarioMapper.ToDtos(d.Intervalos) : null,
        ReferenciaNormativa = d.ReferenciaNormativa,
        Notas = d.Notas,
        ETag = ETags.FromRowVersion(d.RowVersion),
    };

    private static bool CambiaCalendarioEfectivo(IReadOnlyDictionary<string, string?> antes, CalendarioFeriados despues)
    {
        string? Antes(string k) => antes.TryGetValue(k, out var v) ? v : null;
        return Antes("nombre") != despues.Nombre ||
               Antes("paisId") != AuditDiff.Describir(despues.PaisId) ||
               Antes("ambito") != AuditDiff.Describir(despues.Ambito) ||
               Antes("activo") != AuditDiff.Describir(despues.Activo);
    }

    private static string? LimpiarTexto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static DateOnly Min(DateOnly a, DateOnly b) => a <= b ? a : b;
    private static DateOnly Max(DateOnly a, DateOnly b) => a >= b ? a : b;

    private static IHttpResult<T> NoEncontrado<T>(string recurso) =>
        CalendarioResults.Fail<T>($"{recurso} no encontrado", "id", $"{recurso} no encontrado.", HttpStatusCode.NotFound);

    private static IHttpResult NoEncontrado(string recurso) =>
        CalendarioResults.Fail($"{recurso} no encontrado", "id", $"{recurso} no encontrado.", HttpStatusCode.NotFound);

    // ───────────────────────────── Persistencia ─────────────────────────────

    /// <summary>Guarda y traduce una colisión de concurrencia (rowversion o versión del calendario) a 412 (RF-LO-42).</summary>
    private async Task<IHttpResult?> GuardarAsync(CancellationToken ct)
    {
        try
        {
            await Ctx.SaveChangesAsync(ct);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CalendarioResults.Concurrencia();
        }
    }

    private static IHttpResult<T> Reencauzar<T>(IHttpResult error) =>
        HttpResult<T>.Failure(error.Error.Title, error.Error.Detail, error.StatusCode);

    private static IHttpResult Reencauzar(IHttpResult error) =>
        HttpResult.Failure(error.Error.Title, error.Error.Detail, error.StatusCode);
}
