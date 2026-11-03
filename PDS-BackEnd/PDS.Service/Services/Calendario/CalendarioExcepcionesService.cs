using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using PDS.Application.Data;
using PDS.Application.Repositories;
using PDS.Domain.DTOs.Calendario;
using PDS.Domain.Enums;
using PDS.Domain.Models;
using PDS.Domain.Response;
using PDS.Domain.ValueObjects;
using PDS.Service.Authorization;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// Excepciones del calendario de una localidad (RF-LO-31). Las excepciones de sistema (suspensión, cierre) se listan pero no se
/// crean, editan ni eliminan por aquí (RF-LO-32). Cada escritura deja bitácora (RF-LO-44) y una fila del feed con incremento de
/// versión (RF-LO-43) en la misma transacción.
/// </summary>
public interface ICalendarioExcepcionesService
{
    Task<IHttpResult<IReadOnlyList<ExcepcionCalendarioDto>>> ListAsync(
        CalendarioActor actor, Guid localidadId, DateOnly? desde, DateOnly? hasta, CancellationToken ct = default);
    Task<IHttpResult<ExcepcionCalendarioDto>> GetAsync(CalendarioActor actor, Guid localidadId, Guid excepcionId, CancellationToken ct = default);
    Task<IHttpResult<ExcepcionCalendarioDto>> CreateAsync(CalendarioActor actor, Guid localidadId, UpsertExcepcionDto dto, CancellationToken ct = default);
    Task<IHttpResult<ExcepcionCalendarioDto>> UpdateAsync(
        CalendarioActor actor, Guid localidadId, Guid excepcionId, UpsertExcepcionDto dto, string? ifMatch, CancellationToken ct = default);

    /// <param name="motivo">Obligatorio cuando un administrador elimina una excepción de fechas pasadas (RF-LO-33).</param>
    Task<IHttpResult> DeleteAsync(
        CalendarioActor actor, Guid localidadId, Guid excepcionId, string? motivo, string? ifMatch, CancellationToken ct = default);
}

public class CalendarioExcepcionesService(
    IUnitOfWork<PDSDbContext> uow,
    ICalendarioAccessGuard guard,
    ICalendarioCambioService cambios,
    ICalendarioAuditoriaService auditoria,
    IRetroactiveWindowPolicy ventana,
    TimeProvider clock) : ICalendarioExcepcionesService
{
    private const int MaxDias = 366;
    private const int MaxMotivo = 1000;
    private const int MaxReferencia = 500;

    private PDSDbContext Ctx => uow.Context;
    private DateTime Ahora => clock.GetUtcNow().UtcDateTime;

    // ───────────────────────────── Lectura ─────────────────────────────

    public async Task<IHttpResult<IReadOnlyList<ExcepcionCalendarioDto>>> ListAsync(
        CalendarioActor actor, Guid localidadId, DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
    {
        if (!await ExisteLocalidadAsync(actor, localidadId, ct)) return LocalidadNoEncontrada<IReadOnlyList<ExcepcionCalendarioDto>>();
        if (desde is { } d && hasta is { } h && d > h)
            return Falla<IReadOnlyList<ExcepcionCalendarioDto>>("rango", "«desde» no puede ser posterior a «hasta».");

        var query = Ctx.LocalidadExcepcionesCalendario.AsNoTracking().Where(e => e.LocalidadId == localidadId && e.DeletedAt == null);
        // Solapa con [desde, hasta]; una excepción sin fecha de fin está abierta hacia adelante.
        if (hasta is { } hasta1) query = query.Where(e => e.FechaInicio <= hasta1);
        if (desde is { } desde1) query = query.Where(e => e.FechaFin == null || e.FechaFin >= desde1);

        var lista = await query.OrderBy(e => e.FechaInicio).ThenBy(e => e.CreatedAt).ToListAsync(ct);
        return HttpResult<IReadOnlyList<ExcepcionCalendarioDto>>.Success(lista.Select(e => Mapear(e)).ToList());
    }

    public async Task<IHttpResult<ExcepcionCalendarioDto>> GetAsync(CalendarioActor actor, Guid localidadId, Guid excepcionId, CancellationToken ct = default)
    {
        if (!await ExisteLocalidadAsync(actor, localidadId, ct)) return LocalidadNoEncontrada<ExcepcionCalendarioDto>();
        var e = await Ctx.LocalidadExcepcionesCalendario.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == excepcionId && x.LocalidadId == localidadId && x.DeletedAt == null, ct);
        return e is null ? ExcepcionNoEncontrada<ExcepcionCalendarioDto>() : HttpResult<ExcepcionCalendarioDto>.Success(Mapear(e));
    }

    // ───────────────────────────── Alta ─────────────────────────────

    public async Task<IHttpResult<ExcepcionCalendarioDto>> CreateAsync(
        CalendarioActor actor, Guid localidadId, UpsertExcepcionDto dto, CancellationToken ct = default)
    {
        var loc = await CargarLocalidadAsync(actor, localidadId, ct);
        if (loc is null) return LocalidadNoEncontrada<ExcepcionCalendarioDto>();
        if (!await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct))
            return CalendarioResults.SinPermiso<ExcepcionCalendarioDto>();

        var v = await ValidarAsync(actor, localidadId, dto, excluirId: null, ct);
        if (v.Error is not null) return v.Error;

        var retro = EvaluarVentana<ExcepcionCalendarioDto>(actor, loc, [v.Inicio], dto.Motivo);
        if (retro is not null) return retro;

        var ahora = Ahora;
        var entity = new LocalidadExcepcionCalendario
        {
            Id = Guid.NewGuid(),
            LocalidadId = localidadId,
            FechaInicio = v.Inicio,
            FechaFin = v.Fin,
            Tipo = v.Tipo,
            Categoria = v.Categoria,
            Origen = v.Origen,
            Intervalos = v.Intervalos,
            Motivo = dto.Motivo!.Trim(),
            ReferenciaNormativa = LimpiarTexto(dto.ReferenciaNormativa),
            InstrumentoLegalId = dto.InstrumentoLegalId,
            EsSistema = false,
            CreatedAt = ahora,
            UpdatedAt = ahora,
            CreadoPorId = actor.UserId,
            CreadoPor = actor.UserName,
        };
        Ctx.LocalidadExcepcionesCalendario.Add(entity);

        auditoria.RegistrarLocalidad(actor.OrganizationId, localidadId, LocalidadAccion.CalendarExceptionAdded, actor.UserId, actor.UserName,
            actor.Canal, entity.Motivo, AuditDiff.Compute(null, entity), entity.InstrumentoLegalId);
        var version = await cambios.RegistrarAsync(localidadId, MotivoCambioCalendario.Excepcion, v.Inicio, v.Fin, ct);

        var fallo = await GuardarAsync(ct);
        if (fallo is not null) return Reencauzar<ExcepcionCalendarioDto>(fallo);

        var guardada = await Ctx.LocalidadExcepcionesCalendario.AsNoTracking().FirstAsync(e => e.Id == entity.Id, ct);
        return HttpResult<ExcepcionCalendarioDto>.Success(Mapear(guardada, version), HttpStatusCode.Created);
    }

    // ───────────────────────────── Edición ─────────────────────────────

    public async Task<IHttpResult<ExcepcionCalendarioDto>> UpdateAsync(
        CalendarioActor actor, Guid localidadId, Guid excepcionId, UpsertExcepcionDto dto, string? ifMatch, CancellationToken ct = default)
    {
        var loc = await CargarLocalidadAsync(actor, localidadId, ct);
        if (loc is null) return LocalidadNoEncontrada<ExcepcionCalendarioDto>();
        if (!await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct))
            return CalendarioResults.SinPermiso<ExcepcionCalendarioDto>();

        var entity = await Ctx.LocalidadExcepcionesCalendario.FirstOrDefaultAsync(
            e => e.Id == excepcionId && e.LocalidadId == localidadId && e.DeletedAt == null, ct);
        if (entity is null) return ExcepcionNoEncontrada<ExcepcionCalendarioDto>();
        if (entity.EsSistema) return SistemaNoEditable<ExcepcionCalendarioDto>();
        if (!ETags.IfMatchSatisfied(ifMatch, ETags.FromRowVersion(entity.RowVersion))) return CalendarioResults.Concurrencia<ExcepcionCalendarioDto>();

        var v = await ValidarAsync(actor, localidadId, dto, excluirId: excepcionId, ct);
        if (v.Error is not null) return v.Error;

        // Se evalúan el inicio anterior y el nuevo: un gestor no puede tocar una excepción vieja ni mover una nueva al pasado.
        var retro = EvaluarVentana<ExcepcionCalendarioDto>(actor, loc, [entity.FechaInicio, v.Inicio], dto.Motivo);
        if (retro is not null) return retro;

        var inicioAnterior = entity.FechaInicio;
        var finAnterior = entity.FechaFin;
        var antes = AuditDiff.Snapshot(entity);

        entity.FechaInicio = v.Inicio;
        entity.FechaFin = v.Fin;
        entity.Tipo = v.Tipo;
        entity.Categoria = v.Categoria;
        entity.Origen = v.Origen;
        entity.Intervalos = v.Intervalos;
        entity.Motivo = dto.Motivo!.Trim();
        entity.ReferenciaNormativa = LimpiarTexto(dto.ReferenciaNormativa);
        entity.InstrumentoLegalId = dto.InstrumentoLegalId;

        var diff = AuditDiff.Compute(antes, AuditDiff.Snapshot(entity));
        long? version = null;
        if (diff is not null)
        {
            entity.UpdatedAt = Ahora;
            entity.ModificadoPorId = actor.UserId;
            entity.ModificadoPor = actor.UserName;
            auditoria.RegistrarLocalidad(actor.OrganizationId, localidadId, LocalidadAccion.CalendarExceptionUpdated, actor.UserId,
                actor.UserName, actor.Canal, entity.Motivo, diff, entity.InstrumentoLegalId);

            // Rango afectado: la unión del rango anterior y el nuevo.
            var desde = inicioAnterior <= v.Inicio ? inicioAnterior : v.Inicio;
            DateOnly? hasta = finAnterior is { } fa ? (fa >= v.Fin ? fa : v.Fin) : null;
            version = await cambios.RegistrarAsync(localidadId, MotivoCambioCalendario.Excepcion, desde, hasta, ct);
        }

        var fallo = await GuardarAsync(ct);
        if (fallo is not null) return Reencauzar<ExcepcionCalendarioDto>(fallo);

        var guardada = await Ctx.LocalidadExcepcionesCalendario.AsNoTracking().FirstAsync(e => e.Id == excepcionId, ct);
        return HttpResult<ExcepcionCalendarioDto>.Success(Mapear(guardada, version ?? loc.CalendarioVersion));
    }

    // ───────────────────────────── Baja ─────────────────────────────

    public async Task<IHttpResult> DeleteAsync(
        CalendarioActor actor, Guid localidadId, Guid excepcionId, string? motivo, string? ifMatch, CancellationToken ct = default)
    {
        var loc = await CargarLocalidadAsync(actor, localidadId, ct);
        if (loc is null) return CalendarioResults.Fail("Localidad no encontrada", "id", "Localidad no encontrada.", HttpStatusCode.NotFound);
        if (!await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct))
            return CalendarioResults.SinPermiso();

        var entity = await Ctx.LocalidadExcepcionesCalendario.FirstOrDefaultAsync(
            e => e.Id == excepcionId && e.LocalidadId == localidadId && e.DeletedAt == null, ct);
        if (entity is null) return CalendarioResults.Fail("Excepción no encontrada", "id", "Excepción no encontrada.", HttpStatusCode.NotFound);
        if (entity.EsSistema) return CalendarioResults.Fail("Excepción de sistema", "permiso", MensajeSistema, HttpStatusCode.Forbidden);
        if (!ETags.IfMatchSatisfied(ifMatch, ETags.FromRowVersion(entity.RowVersion))) return CalendarioResults.Concurrencia();

        var retro = EvaluarVentana(actor, loc, [entity.FechaInicio], motivo);
        if (retro is not null) return retro;

        var detalle = AuditDiff.Compute(entity, null);
        entity.DeletedAt = Ahora;
        entity.UpdatedAt = entity.DeletedAt.Value;
        entity.ModificadoPorId = actor.UserId;
        entity.ModificadoPor = actor.UserName;

        auditoria.RegistrarLocalidad(actor.OrganizationId, localidadId, LocalidadAccion.CalendarExceptionRemoved, actor.UserId, actor.UserName,
            actor.Canal, LimpiarTexto(motivo), detalle, entity.InstrumentoLegalId);
        await cambios.RegistrarAsync(localidadId, MotivoCambioCalendario.Excepcion, entity.FechaInicio, entity.FechaFin, ct);

        var fallo = await GuardarAsync(ct);
        return fallo is not null ? Reencauzar(fallo) : HttpResult.Success();
    }

    // ───────────────────────────── Validación ─────────────────────────────

    private const string MensajeSistema =
        "Las excepciones de sistema (suspensión o cierre) las genera el ciclo de vida de la localidad y no se modifican por la API.";

    private sealed record Validada(
        IHttpResult<ExcepcionCalendarioDto>? Error, DateOnly Inicio, DateOnly Fin, TipoExcepcionCalendario Tipo,
        CategoriaExcepcionCalendario Categoria, OrigenExcepcionCalendario Origen, List<IntervaloLaboral>? Intervalos);

    private async Task<Validada> ValidarAsync(CalendarioActor actor, Guid localidadId, UpsertExcepcionDto dto, Guid? excluirId, CancellationToken ct)
    {
        Validada Fallo(IHttpResult<ExcepcionCalendarioDto> e) => new(e, default, default, default, default, default, null);
        Validada Falla400(string key, string msg) => Fallo(Falla<ExcepcionCalendarioDto>(key, msg));

        if (dto.FechaInicio is not { } inicio) return Falla400("fechaInicio", "La fecha de inicio es obligatoria.");
        var fin = dto.FechaFin ?? inicio;     // UI-08: «Hasta» vale «Desde» por omisión
        if (fin < inicio) return Falla400("fechaFin", "La fecha de fin no puede ser anterior a la de inicio.");
        if (fin.DayNumber - inicio.DayNumber + 1 > MaxDias) return Falla400("fechaFin", $"Una excepción no puede durar más de {MaxDias} días.");

        if (dto.Tipo is not { } tipo || !Enum.IsDefined(tipo)) return Falla400("tipo", "El tipo es obligatorio (Cierre, JornadaEspecial o DiaLaborableAdicional).");
        if (dto.Categoria is not { } categoria || !Enum.IsDefined(categoria)) return Falla400("categoria", "La categoría es obligatoria.");
        if (categoria is CategoriaExcepcionCalendario.Suspension or CategoriaExcepcionCalendario.CierreDefinitivo)
            return Fallo(CalendarioResults.Fail<ExcepcionCalendarioDto>("Excepción de sistema", "permiso", MensajeSistema, HttpStatusCode.Forbidden));
        if (dto.Origen is not { } origen || !Enum.IsDefined(origen)) return Falla400("origen", "El origen es obligatorio (PaisAnfitrion, PaisPropio o Institucional).");

        if (string.IsNullOrWhiteSpace(dto.Motivo)) return Falla400("motivo", "El motivo es obligatorio.");
        if (dto.Motivo.Trim().Length > MaxMotivo) return Falla400("motivo", $"El motivo no puede superar {MaxMotivo} caracteres.");
        if (dto.ReferenciaNormativa is { Length: > MaxReferencia }) return Falla400("referenciaNormativa", $"La referencia normativa no puede superar {MaxReferencia} caracteres.");

        // RF-LO-31: los intervalos están prohibidos en un cierre y son obligatorios en los demás tipos.
        List<IntervaloLaboral>? intervalos = null;
        if (tipo == TipoExcepcionCalendario.Cierre)
        {
            if (dto.Intervalos is { Count: > 0 }) return Falla400("intervalos", "Un cierre no lleva intervalos.");
        }
        else
        {
            if (dto.Intervalos is not { Count: > 0 }) return Falla400("intervalos", "Este tipo de excepción requiere al menos un intervalo de trabajo.");
            var parseo = CalendarioMapper.ParseIntervalos(dto.Intervalos);
            if (!parseo.EsValido) return Falla400("intervalos", parseo.ErrorFormato ?? parseo.ErrorSemantico!);
            intervalos = parseo.Valores.ToList();
        }

        if (dto.InstrumentoLegalId is { } instrumentoId &&
            !await Ctx.LegalInstruments.AnyAsync(l => l.Id == instrumentoId && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct))
            return Falla400("instrumentoLegalId", "El instrumento legal indicado no existe.");

        // Las excepciones de usuario de una localidad no se solapan (las de sistema tienen su propia precedencia).
        var choque = await Ctx.LocalidadExcepcionesCalendario.AsNoTracking()
            .Where(e => e.LocalidadId == localidadId && e.DeletedAt == null && !e.EsSistema && (excluirId == null || e.Id != excluirId)
                        && e.FechaInicio <= fin && (e.FechaFin == null || e.FechaFin >= inicio))
            .OrderBy(e => e.FechaInicio)
            .FirstOrDefaultAsync(ct);
        if (choque is not null)
        {
            var hasta = choque.FechaFin is { } f ? Fmt(f) : "sin fin";
            return Fallo(CalendarioResults.Fail<ExcepcionCalendarioDto>("Excepción solapada", "solapamiento",
                $"Se solapa con la excepción {choque.Id} ({Fmt(choque.FechaInicio)} a {hasta}).", HttpStatusCode.Conflict));
        }

        return new Validada(null, inicio, fin, tipo, categoria, origen, intervalos);
    }

    /// <summary>RF-LO-33: devuelve el error de ventana (403 <c>fechaInicio</c>) o de motivo (400), o nulo si se puede seguir.</summary>
    private IHttpResult<T>? EvaluarVentana<T>(CalendarioActor actor, Localidad loc, IEnumerable<DateOnly> fechas, string? motivo)
    {
        var evaluacion = ventana.Evaluar(guard.CanManage(actor.Permissions), loc.ZonaHoraria, fechas, motivo);
        return evaluacion.Veredicto switch
        {
            RetroactivoVeredicto.FueraDeVentana => CalendarioResults.Fail<T>("Fuera de la ventana retroactiva", "fechaInicio",
                $"Fuera del plazo para cambios retroactivos ({ventana.VentanaDias} días).", HttpStatusCode.Forbidden),
            RetroactivoVeredicto.RequiereMotivo => CalendarioResults.Fail<T>("Motivo requerido", "motivo",
                "Un cambio sobre fechas pasadas requiere indicar el motivo.", HttpStatusCode.BadRequest),
            _ => null,
        };
    }

    private IHttpResult? EvaluarVentana(CalendarioActor actor, Localidad loc, IEnumerable<DateOnly> fechas, string? motivo) =>
        EvaluarVentana<bool>(actor, loc, fechas, motivo);

    // ───────────────────────────── Carga y mapeo ─────────────────────────────

    private Task<Localidad?> CargarLocalidadAsync(CalendarioActor actor, Guid id, CancellationToken ct) =>
        Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct);

    private Task<bool> ExisteLocalidadAsync(CalendarioActor actor, Guid id, CancellationToken ct) =>
        Ctx.Localidades.AnyAsync(l => l.Id == id && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct);

    private static ExcepcionCalendarioDto Mapear(LocalidadExcepcionCalendario e, long? calendarioVersion = null) => new()
    {
        Id = e.Id,
        FechaInicio = e.FechaInicio,
        FechaFin = e.FechaFin,
        Tipo = e.Tipo,
        Categoria = e.Categoria,
        Origen = e.Origen,
        Intervalos = e.Tipo == TipoExcepcionCalendario.Cierre ? null : CalendarioMapper.ToDtos(e.Intervalos),
        Motivo = e.Motivo,
        ReferenciaNormativa = e.ReferenciaNormativa,
        InstrumentoLegalId = e.InstrumentoLegalId,
        EsSistema = e.EsSistema,
        CreadoPor = e.CreadoPor,
        CreadoEn = e.CreatedAt,
        ModificadoPor = e.ModificadoPor,
        ModificadoEn = e.ModificadoPorId is null ? null : e.UpdatedAt,
        CalendarioVersion = calendarioVersion,
        ETag = ETags.FromRowVersion(e.RowVersion),
    };

    private static string Fmt(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    private static string? LimpiarTexto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static IHttpResult<T> Falla<T>(string key, string msg) =>
        CalendarioResults.Fail<T>("Datos inválidos de la excepción", key, msg, HttpStatusCode.BadRequest);

    private static IHttpResult<T> LocalidadNoEncontrada<T>() =>
        CalendarioResults.Fail<T>("Localidad no encontrada", "id", "Localidad no encontrada.", HttpStatusCode.NotFound);

    private static IHttpResult<T> ExcepcionNoEncontrada<T>() =>
        CalendarioResults.Fail<T>("Excepción no encontrada", "id", "Excepción no encontrada.", HttpStatusCode.NotFound);

    private static IHttpResult<T> SistemaNoEditable<T>() =>
        CalendarioResults.Fail<T>("Excepción de sistema", "permiso", MensajeSistema, HttpStatusCode.Forbidden);

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
