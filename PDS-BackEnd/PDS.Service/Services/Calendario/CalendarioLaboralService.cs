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
using PDS.Service.Services.Calendario.Motor;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// Calendario laboral de una localidad: definición (RF-LO-24 a 31), versiones de la jornada semanal (RF-LO-25) y, en los
/// siguientes tickets, suscripciones a feriados (RF-LO-30). Cada escritura deja bitácora (RF-LO-44) y una fila del feed con
/// incremento de versión (RF-LO-43) en la misma transacción.
/// </summary>
public interface ICalendarioLaboralService
{
    Task<IHttpResult<LocalidadCalendarioDto>> GetCalendarioAsync(CalendarioActor actor, Guid localidadId, CancellationToken ct = default);
    Task<IHttpResult<JornadaCreadaDto>> CrearJornadaAsync(CalendarioActor actor, Guid localidadId, CrearJornadaDto dto, CancellationToken ct = default);
    Task<IHttpResult> EliminarJornadaAsync(CalendarioActor actor, Guid localidadId, Guid jornadaId, CancellationToken ct = default);

    /// <summary>
    /// RF-LO-30: reemplaza el CONJUNTO de calendarios de feriados de la localidad. <paramref name="motivo"/>: obligatorio cuando un
    /// administrador cambia fechas pasadas (RF-LO-33).
    /// </summary>
    Task<IHttpResult<SuscripcionesActualizadasDto>> ReemplazarSuscripcionesAsync(
        CalendarioActor actor, Guid localidadId, IReadOnlyList<SuscripcionInputDto>? suscripciones, string? motivo, CancellationToken ct = default);

    /// <summary>
    /// RF-LO-36: marca la localidad como la predeterminada de calendario de su organización (una sola; solo localidades ACTIVAS; solo el
    /// administrador de calendario). Idempotente: si ya lo es no cambia nada ni audita. No cambia el calendario efectivo, así que no sube
    /// <c>CalendarioVersion</c> ni escribe en el feed (RF-LO-43).
    /// </summary>
    Task<IHttpResult> MarcarPredeterminadaAsync(CalendarioActor actor, Guid localidadId, CancellationToken ct = default);

    /// <summary>RF-LO-36 (M2M): la localidad predeterminada de la organización; 404 si todavía no hay ninguna. ETag/304 como las demás lecturas.</summary>
    Task<IHttpResult<Lectura<LocalidadPredeterminadaDto>>> GetLocalidadPredeterminadaAsync(
        int organizationId, string? ifNoneMatch, CancellationToken ct = default);

    /// <summary>
    /// RF-LO-40: localidades que quien llama puede gestionar. Administrador de calendario: todas las localidades ACTIVAS de la
    /// organización (<c>rol = Admin</c>). Gestor: exactamente aquellas donde cumple la regla RF-LO-38 (<c>rol = Gestor</c>). Otros: vacío.
    /// </summary>
    Task<IHttpResult<IReadOnlyList<LocalidadGestionableDto>>> ListarLocalidadesGestionablesAsync(CalendarioActor actor, CancellationToken ct = default);
}

public class CalendarioLaboralService(
    IUnitOfWork<PDSDbContext> uow,
    ICalendarioAccessGuard guard,
    ICalendarioCambioService cambios,
    ICalendarioAuditoriaService auditoria,
    IRetroactiveWindowPolicy ventana,
    TimeProvider clock) : ICalendarioLaboralService
{
    private PDSDbContext Ctx => uow.Context;
    private DateTime Ahora => clock.GetUtcNow().UtcDateTime;

    // ───────────────────────────── Qué puedo gestionar (RF-LO-40) ─────────────────────────────

    public async Task<IHttpResult<IReadOnlyList<LocalidadGestionableDto>>> ListarLocalidadesGestionablesAsync(
        CalendarioActor actor, CancellationToken ct = default)
    {
        var esAdmin = guard.CanManage(actor.Permissions);
        IReadOnlyList<Guid>? idsGestor = null;
        if (!esAdmin)
        {
            idsGestor = await guard.LocalidadesQueGestionaAsync(actor.UserId, actor.OrganizationId, ct);
            if (idsGestor.Count == 0)
                return HttpResult<IReadOnlyList<LocalidadGestionableDto>>.Success([]);
        }

        var query = Ctx.Localidades.AsNoTracking().Where(l => l.OrganizationId == actor.OrganizationId && l.DeletedAt == null);
        query = esAdmin
            ? query.Where(l => l.Estado!.Codigo == EstadoActiva)
            : query.Where(l => idsGestor!.Contains(l.Id));

        var filas = await query
            .OrderBy(l => l.NombreOficial).ThenBy(l => l.Codigo)
            .Select(l => new LocalidadGestionableDto
            {
                Id = l.Id, Codigo = l.Codigo, NombreOficial = l.NombreOficial, Pais = l.Pais!.Nombre, Ciudad = l.Ciudad,
                ZonaHoraria = l.ZonaHoraria, Rol = esAdmin ? "Admin" : "Gestor",
            })
            .ToListAsync(ct);
        return HttpResult<IReadOnlyList<LocalidadGestionableDto>>.Success(filas);
    }

    // ───────────────────────────── Localidad predeterminada (RF-LO-36) ─────────────────────────────

    private const string EstadoActiva = "ACTIVA";

    public async Task<IHttpResult> MarcarPredeterminadaAsync(CalendarioActor actor, Guid localidadId, CancellationToken ct = default)
    {
        // §10.4: solo el administrador de calendario; los gestores de una localidad no pueden elegir la predeterminada.
        if (!guard.CanManage(actor.Permissions))
            return CalendarioResults.Fail("Sin permiso", "permiso",
                "Solo los administradores de calendario pueden elegir la localidad predeterminada.", HttpStatusCode.Forbidden);

        var loc = await Ctx.Localidades.Include(l => l.Estado).FirstOrDefaultAsync(
            l => l.Id == localidadId && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct);
        if (loc is null)
            return CalendarioResults.Fail("Localidad no encontrada", "localidad", "La localidad no existe.", HttpStatusCode.NotFound);

        if (loc.EsLocalidadCalendarioPredeterminada)
            return HttpResult.Success(); // Idempotente: sin cambios ni auditoría.

        if (!string.Equals(loc.Estado?.Codigo, EstadoActiva, StringComparison.OrdinalIgnoreCase))
            return CalendarioResults.Fail("Localidad no activa", "estado",
                "Solo una localidad activa puede ser la predeterminada de calendario.", HttpStatusCode.Conflict);

        var estrategia = Ctx.Database.CreateExecutionStrategy();
        try
        {
            return await estrategia.ExecuteAsync(async () =>
            {
                await using var tx = await Ctx.Database.BeginTransactionAsync(ct);

                // El índice único filtrado falla si dos filas quedan en true aunque sea un instante: primero se desmarca la anterior.
                var anterior = await Ctx.Localidades.FirstOrDefaultAsync(l =>
                    l.OrganizationId == actor.OrganizationId && l.EsLocalidadCalendarioPredeterminada
                    && l.DeletedAt == null && l.Id != localidadId, ct);
                if (anterior is not null)
                {
                    anterior.EsLocalidadCalendarioPredeterminada = false;
                    auditoria.RegistrarLocalidad(actor.OrganizationId, anterior.Id, LocalidadAccion.Modified, actor.UserId, actor.UserName,
                        actor.Canal, detalle: DiffPredeterminada(from: true, to: false));
                    await Ctx.SaveChangesAsync(ct);
                }

                loc.EsLocalidadCalendarioPredeterminada = true;
                auditoria.RegistrarLocalidad(actor.OrganizationId, loc.Id, LocalidadAccion.Modified, actor.UserId, actor.UserName,
                    actor.Canal, detalle: DiffPredeterminada(from: false, to: true));
                await Ctx.SaveChangesAsync(ct);

                await tx.CommitAsync(ct);
                return HttpResult.Success();
            });
        }
        catch (DbUpdateException ex) when (EsColisionDePredeterminada(ex))
        {
            // Dos administradores marcaron localidades distintas a la vez: el índice único hizo fallar a uno (incluye la ficha de
            // concurrencia de CalendarioVersion de la localidad).
            Ctx.ChangeTracker.Clear();
            return CalendarioResults.Fail("Conflicto de concurrencia", "concurrencia",
                "Otra persona cambió la localidad predeterminada. Recargue e intente de nuevo.", HttpStatusCode.Conflict);
        }
    }

    public async Task<IHttpResult<Lectura<LocalidadPredeterminadaDto>>> GetLocalidadPredeterminadaAsync(
        int organizationId, string? ifNoneMatch, CancellationToken ct = default)
    {
        var loc = await Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(
            l => l.OrganizationId == organizationId && l.EsLocalidadCalendarioPredeterminada && l.DeletedAt == null, ct);
        if (loc is null)
            return CalendarioResults.Fail<Lectura<LocalidadPredeterminadaDto>>("Sin localidad predeterminada", "localidad",
                "La organización todavía no tiene localidad predeterminada de calendario.", HttpStatusCode.NotFound);

        // El cuerpo incluye código, nombre y zona, que pueden cambiar sin subir la versión del calendario: el ETag los cubre.
        var etag = ETags.FromParts("calendario-predeterminada", loc.Id.ToString("N"), loc.Codigo, loc.NombreOficial, loc.ZonaHoraria,
            loc.CalendarioVersion.ToString(CultureInfo.InvariantCulture));
        if (ETags.Matches(ifNoneMatch, etag))
            return HttpResult<Lectura<LocalidadPredeterminadaDto>>.Success(new Lectura<LocalidadPredeterminadaDto>(null, etag, true));

        var dto = new LocalidadPredeterminadaDto
        {
            LocalidadId = loc.Id,
            Codigo = loc.Codigo,
            NombreOficial = loc.NombreOficial,
            ZonaHoraria = loc.ZonaHoraria,
            CalendarioVersion = loc.CalendarioVersion,
        };
        return HttpResult<Lectura<LocalidadPredeterminadaDto>>.Success(new Lectura<LocalidadPredeterminadaDto>(dto, etag, false));
    }

    /// <summary>Diff de bitácora {esLocalidadCalendarioPredeterminada:{from,to}} con booleanos JSON (contrato del frontend).</summary>
    private static string DiffPredeterminada(bool from, bool to) =>
        System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["esLocalidadCalendarioPredeterminada"] = new Dictionary<string, bool> { ["from"] = from, ["to"] = to },
        });

    // SQL Server nombra el índice; SQLite nombra las columnas de la restricción.
    private static bool EsColisionDePredeterminada(DbUpdateException ex)
    {
        if (ex is DbUpdateConcurrencyException) return true;
        var texto = ex.ToString();
        return texto.Contains("CalendarioPredeterminada", StringComparison.OrdinalIgnoreCase)
            || texto.Contains("Localidades.OrganizationId", StringComparison.OrdinalIgnoreCase);
    }

    // ───────────────────────────── Definición ─────────────────────────────

    public async Task<IHttpResult<LocalidadCalendarioDto>> GetCalendarioAsync(CalendarioActor actor, Guid localidadId, CancellationToken ct = default)
    {
        var loc = await Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(
            l => l.Id == localidadId && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct);
        if (loc is null) return NoEncontrada<LocalidadCalendarioDto>();

        var jornadas = await Ctx.LocalidadJornadas.AsNoTracking().Where(j => j.LocalidadId == localidadId).OrderBy(j => j.VigenteDesde).ToListAsync(ct);
        var suscripciones = await Ctx.LocalidadCalendariosFeriados.AsNoTracking()
            .Where(s => s.LocalidadId == localidadId && s.CalendarioFeriados!.DeletedAt == null)
            .OrderBy(s => s.CalendarioFeriados!.Codigo)
            .Select(s => new SuscripcionDto
            {
                Id = s.Id,
                CalendarioFeriadosId = s.CalendarioFeriadosId,
                Codigo = s.CalendarioFeriados!.Codigo,
                Nombre = s.CalendarioFeriados.Nombre,
                VigenteDesde = s.VigenteDesde,
                VigenteHasta = s.VigenteHasta,
            })
            .ToListAsync(ct);

        var hoy = ventana.HoyLocal(loc.ZonaHoraria);
        var dto = new LocalidadCalendarioDto
        {
            LocalidadId = loc.Id,
            Codigo = loc.Codigo,
            NombreOficial = loc.NombreOficial,
            ZonaHoraria = loc.ZonaHoraria,
            TzdbVersion = TzdbInfo.Version,
            CalendarioVersion = loc.CalendarioVersion,
            FechaApertura = loc.FechaApertura,
            EsLocalidadCalendarioPredeterminada = loc.EsLocalidadCalendarioPredeterminada,
            HoyLocal = hoy,
            VentanaRetroactivaDias = ventana.VentanaDias,
            PuedeGestionar = await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct),
            Jornadas = MapVersiones(jornadas, hoy),
            Suscripciones = suscripciones,
            // RF-LO-42: la versión del calendario cambia con cualquier cambio de la definición, así que sirve de ETag.
            ETag = ETags.FromParts("calendario", loc.Id.ToString("N"), loc.CalendarioVersion.ToString(CultureInfo.InvariantCulture)),
        };
        return HttpResult<LocalidadCalendarioDto>.Success(dto);
    }

    // ───────────────────────────── Versiones de jornada (RF-LO-25) ─────────────────────────────

    public async Task<IHttpResult<JornadaCreadaDto>> CrearJornadaAsync(
        CalendarioActor actor, Guid localidadId, CrearJornadaDto dto, CancellationToken ct = default)
    {
        var loc = await CargarLocalidadAsync(actor, localidadId, ct);
        if (loc is null) return NoEncontrada<JornadaCreadaDto>();
        if (!await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct))
            return CalendarioResults.SinPermiso<JornadaCreadaDto>();

        if (dto.VigenteDesde is not { } desde)
            return Falla<JornadaCreadaDto>("vigenteDesde", "La fecha «vigente desde» es obligatoria.");
        if (dto.Motivo is { Length: > 1000 })
            return Falla<JornadaCreadaDto>("motivo", "El motivo no puede superar 1000 caracteres.");

        var (semana, error) = ParsearSemana<JornadaCreadaDto>(dto.Dias);
        if (error is not null) return error;

        // RF-LO-25: los gestores no administradores solo programan versiones desde hoy (fecha local); un administrador puede
        // retrodatar, con motivo obligatorio.
        var hoy = ventana.HoyLocal(loc.ZonaHoraria);
        var esAdmin = guard.CanManage(actor.Permissions);
        if (desde < hoy)
        {
            if (!esAdmin)
                return CalendarioResults.Fail<JornadaCreadaDto>("Fuera de la ventana", "vigenteDesde",
                    $"Solo se pueden programar horarios desde hoy ({Fmt(hoy)}) en adelante.", HttpStatusCode.Forbidden);
            if (string.IsNullOrWhiteSpace(dto.Motivo))
                return Falla<JornadaCreadaDto>("motivo", "Cambiar el horario de fechas pasadas requiere indicar el motivo.");
        }

        var existentes = await Ctx.LocalidadJornadas.AsNoTracking().Where(j => j.LocalidadId == localidadId)
            .Select(j => j.VigenteDesde).ToListAsync(ct);
        if (existentes.Contains(desde))
            return CalendarioResults.Fail<JornadaCreadaDto>("Versión duplicada", "vigenteDesde",
                $"Ya existe una versión de horario vigente desde el {Fmt(desde)}.", HttpStatusCode.Conflict);

        var jornada = new LocalidadJornada
        {
            Id = Guid.NewGuid(),
            LocalidadId = localidadId,
            VigenteDesde = desde,
            Semana = semana!,
            Motivo = LimpiarTexto(dto.Motivo),
            CreatedAt = Ahora,
            CreadoPorId = actor.UserId,
            CreadoPor = actor.UserName,
        };
        Ctx.LocalidadJornadas.Add(jornada);

        auditoria.RegistrarLocalidad(actor.OrganizationId, localidadId, LocalidadAccion.ScheduleChanged, actor.UserId, actor.UserName,
            actor.Canal, jornada.Motivo, AuditDiff.Compute(null, jornada));
        var version = await cambios.RegistrarAsync(
            localidadId, MotivoCambioCalendario.Jornada, desde, UltimoDiaDeVigencia(desde, existentes), ct);

        var fallo = await GuardarAsync(ct);
        if (fallo is not null) return Reencauzar<JornadaCreadaDto>(fallo);

        return HttpResult<JornadaCreadaDto>.Success(
            new JornadaCreadaDto { Id = jornada.Id, VigenteDesde = desde, Dias = DiasDto(semana!), CalendarioVersion = version },
            HttpStatusCode.Created);
    }

    public async Task<IHttpResult> EliminarJornadaAsync(CalendarioActor actor, Guid localidadId, Guid jornadaId, CancellationToken ct = default)
    {
        var loc = await CargarLocalidadAsync(actor, localidadId, ct);
        if (loc is null) return NoEncontrada();
        if (!await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct))
            return CalendarioResults.SinPermiso();

        var jornada = await Ctx.LocalidadJornadas.FirstOrDefaultAsync(j => j.Id == jornadaId && j.LocalidadId == localidadId, ct);
        if (jornada is null)
            return CalendarioResults.Fail("Versión no encontrada", "id", "Versión de horario no encontrada.", HttpStatusCode.NotFound);

        // RF-LO-25 AC3: solo se eliminan versiones que todavía no entraron en vigor (las vigentes y pasadas son historia).
        var hoy = ventana.HoyLocal(loc.ZonaHoraria);
        if (jornada.VigenteDesde <= hoy)
            return CalendarioResults.Fail("Versión en vigor", "vigenteDesde",
                "No se puede eliminar una versión de horario que ya está en vigor o ya estuvo vigente.", HttpStatusCode.Conflict);

        var otras = await Ctx.LocalidadJornadas.AsNoTracking().Where(j => j.LocalidadId == localidadId && j.Id != jornadaId)
            .Select(j => j.VigenteDesde).ToListAsync(ct);
        var desde = jornada.VigenteDesde;
        var detalle = AuditDiff.Compute(jornada, null);

        Ctx.LocalidadJornadas.Remove(jornada);   // sin borrado lógico: las versiones futuras se eliminan físicamente
        auditoria.RegistrarLocalidad(actor.OrganizationId, localidadId, LocalidadAccion.ScheduleChanged, actor.UserId, actor.UserName,
            actor.Canal, detalle: detalle);
        await cambios.RegistrarAsync(localidadId, MotivoCambioCalendario.Jornada, desde, UltimoDiaDeVigencia(desde, otras), ct);

        var fallo = await GuardarAsync(ct);
        return fallo is not null ? Reencauzar(fallo) : HttpResult.Success();
    }

    // ───────────────────────────── Suscripciones (RF-LO-30) ─────────────────────────────

    public async Task<IHttpResult<SuscripcionesActualizadasDto>> ReemplazarSuscripcionesAsync(
        CalendarioActor actor, Guid localidadId, IReadOnlyList<SuscripcionInputDto>? suscripciones, string? motivo, CancellationToken ct = default)
    {
        var loc = await CargarLocalidadAsync(actor, localidadId, ct);
        if (loc is null) return NoEncontrada<SuscripcionesActualizadasDto>();
        if (!await guard.PuedeGestionarLocalidadAsync(actor.Permissions, loc.Id, actor.UserId, actor.OrganizationId, ct))
            return CalendarioResults.SinPermiso<SuscripcionesActualizadasDto>();
        if (suscripciones is null)
            return Falla<SuscripcionesActualizadasDto>("suscripciones", "El cuerpo debe ser la lista completa de suscripciones (puede ser vacía).");
        if (motivo is { Length: > 1000 })
            return Falla<SuscripcionesActualizadasDto>("motivo", "El motivo no puede superar 1000 caracteres.");

        // Validación de forma de cada elemento.
        var ids = new HashSet<Guid>();
        foreach (var s in suscripciones)
        {
            if (s?.CalendarioFeriadosId is not { } calId)
                return Falla<SuscripcionesActualizadasDto>("calendarioFeriadosId", "Cada suscripción debe indicar el calendario de feriados.");
            if (!ids.Add(calId))
                return Falla<SuscripcionesActualizadasDto>("calendarioFeriadosId", "Un calendario de feriados no puede aparecer dos veces entre las suscripciones.");
            if (s.VigenteDesde is { } d && s.VigenteHasta is { } h && h < d)
                return Falla<SuscripcionesActualizadasDto>("vigenteHasta", "«Vigente hasta» no puede ser anterior a «vigente desde».");
        }

        var calendarios = await Ctx.CalendariosFeriados.AsNoTracking()
            .Where(c => ids.Contains(c.Id) && c.OrganizationId == actor.OrganizationId && c.DeletedAt == null)
            .ToDictionaryAsync(c => c.Id, ct);
        var existentes = await Ctx.LocalidadCalendariosFeriados
            .Include(s => s.CalendarioFeriados)
            .Where(s => s.LocalidadId == localidadId)
            .ToListAsync(ct);
        var porCalendario = existentes.ToDictionary(s => s.CalendarioFeriadosId);

        foreach (var s in suscripciones)
        {
            var id = s.CalendarioFeriadosId!.Value;
            if (!calendarios.TryGetValue(id, out var cal))
                return Falla<SuscripcionesActualizadasDto>("calendarioFeriadosId", $"El calendario de feriados {id} no existe.");
            // Un calendario que se desactivó después de suscribirse no impide dejar esa suscripción como está.
            var sinCambios = porCalendario.TryGetValue(id, out var previa) && previa.VigenteDesde == s.VigenteDesde && previa.VigenteHasta == s.VigenteHasta;
            if (!cal.Activo && !sinCambios)
                return Falla<SuscripcionesActualizadasDto>("calendarioFeriadosId", $"El calendario «{cal.Codigo}» está inactivo.");
        }

        // Diferencias respecto del conjunto actual.
        var agregadas = suscripciones.Where(s => !porCalendario.ContainsKey(s.CalendarioFeriadosId!.Value)).ToList();
        var quitadas = existentes.Where(e => !ids.Contains(e.CalendarioFeriadosId)).ToList();
        var modificadas = suscripciones
            .Where(s => porCalendario.TryGetValue(s.CalendarioFeriadosId!.Value, out var e) && (e.VigenteDesde != s.VigenteDesde || e.VigenteHasta != s.VigenteHasta))
            .ToList();

        if (agregadas.Count == 0 && quitadas.Count == 0 && modificadas.Count == 0)
        {
            // Sin cambios: ni versión, ni bitácora, ni feed.
            return HttpResult<SuscripcionesActualizadasDto>.Success(new SuscripcionesActualizadasDto
            {
                Suscripciones = await LeerSuscripcionesAsync(localidadId, ct),
                CalendarioVersion = loc.CalendarioVersion,
            });
        }

        // Rangos de fechas cuyo calendario efectivo cambia: la suscripción nueva o quitada completa, o la diferencia simétrica de las vigencias.
        var afectados = new List<(Guid CalendarioId, Rango Rango)>();
        foreach (var s in agregadas) afectados.Add((s.CalendarioFeriadosId!.Value, Rango.De(s.VigenteDesde, s.VigenteHasta)));
        foreach (var e in quitadas) afectados.Add((e.CalendarioFeriadosId, Rango.De(e.VigenteDesde, e.VigenteHasta)));
        foreach (var s in modificadas)
        {
            var e = porCalendario[s.CalendarioFeriadosId!.Value];
            var anterior = Rango.De(e.VigenteDesde, e.VigenteHasta);
            var nuevo = Rango.De(s.VigenteDesde, s.VigenteHasta);
            foreach (var r in anterior.Menos(nuevo).Concat(nuevo.Menos(anterior))) afectados.Add((e.CalendarioFeriadosId, r));
        }

        // RF-LO-33: las fechas afectadas son las de los feriados del calendario que caen dentro de esos rangos.
        var fechasAfectadas = new List<DateOnly>();
        foreach (var grupo in afectados.GroupBy(a => a.CalendarioId))
        {
            var fechas = await Ctx.CalendarioFeriadosDias.AsNoTracking()
                .Where(d => d.CalendarioFeriadosId == grupo.Key && d.DeletedAt == null)
                .Select(d => d.Fecha).ToListAsync(ct);
            fechasAfectadas.AddRange(fechas.Where(f => grupo.Any(g => g.Rango.Contiene(f))));
        }
        var evaluacion = ventana.Evaluar(guard.CanManage(actor.Permissions), loc.ZonaHoraria, fechasAfectadas, motivo);
        if (evaluacion.Veredicto == RetroactivoVeredicto.FueraDeVentana)
            return CalendarioResults.Fail<SuscripcionesActualizadasDto>("Fuera de la ventana retroactiva", "suscripciones",
                $"Fuera del plazo para cambios retroactivos ({ventana.VentanaDias} días).", HttpStatusCode.Forbidden);
        if (evaluacion.Veredicto == RetroactivoVeredicto.RequiereMotivo)
            return Falla<SuscripcionesActualizadasDto>("motivo", "Un cambio sobre fechas pasadas requiere indicar el motivo.");

        var antes = DescribirSuscripciones(existentes.Select(e => (e.CalendarioFeriados!.Codigo, e.VigenteDesde, e.VigenteHasta)));
        var despues = DescribirSuscripciones(suscripciones.Select(s => (calendarios[s.CalendarioFeriadosId!.Value].Codigo, s.VigenteDesde, s.VigenteHasta)));

        var ahora = Ahora;
        foreach (var e in quitadas) Ctx.LocalidadCalendariosFeriados.Remove(e);
        foreach (var s in agregadas)
        {
            Ctx.LocalidadCalendariosFeriados.Add(new LocalidadCalendarioFeriados
            {
                Id = Guid.NewGuid(),
                LocalidadId = localidadId,
                CalendarioFeriadosId = s.CalendarioFeriadosId!.Value,
                VigenteDesde = s.VigenteDesde,
                VigenteHasta = s.VigenteHasta,
                CreatedAt = ahora,
                CreadoPorId = actor.UserId,
                CreadoPor = actor.UserName,
            });
        }
        foreach (var s in modificadas)
        {
            var e = porCalendario[s.CalendarioFeriadosId!.Value];
            e.VigenteDesde = s.VigenteDesde;
            e.VigenteHasta = s.VigenteHasta;
        }

        auditoria.RegistrarLocalidad(actor.OrganizationId, localidadId, LocalidadAccion.HolidaySubscriptionsChanged, actor.UserId,
            actor.UserName, actor.Canal, LimpiarTexto(motivo),
            AuditDiff.Compute(new Dictionary<string, string?> { ["suscripciones"] = antes }, new Dictionary<string, string?> { ["suscripciones"] = despues }));

        var desde = afectados.Min(a => a.Rango.Desde);
        var hasta = afectados.Max(a => a.Rango.Hasta);
        var version = await cambios.RegistrarAsync(
            localidadId, MotivoCambioCalendario.Suscripcion,
            desde == DateOnly.MinValue ? null : desde, hasta == DateOnly.MaxValue ? null : hasta, ct);

        var fallo = await GuardarAsync(ct);
        if (fallo is not null) return Reencauzar<SuscripcionesActualizadasDto>(fallo);

        return HttpResult<SuscripcionesActualizadasDto>.Success(new SuscripcionesActualizadasDto
        {
            Suscripciones = await LeerSuscripcionesAsync(localidadId, ct),
            CalendarioVersion = version,
        });
    }

    private async Task<List<SuscripcionDto>> LeerSuscripcionesAsync(Guid localidadId, CancellationToken ct) =>
        await Ctx.LocalidadCalendariosFeriados.AsNoTracking()
            .Where(s => s.LocalidadId == localidadId && s.CalendarioFeriados!.DeletedAt == null)
            .OrderBy(s => s.CalendarioFeriados!.Codigo)
            .Select(s => new SuscripcionDto
            {
                Id = s.Id,
                CalendarioFeriadosId = s.CalendarioFeriadosId,
                Codigo = s.CalendarioFeriados!.Codigo,
                Nombre = s.CalendarioFeriados.Nombre,
                VigenteDesde = s.VigenteDesde,
                VigenteHasta = s.VigenteHasta,
            })
            .ToListAsync(ct);

    private static string DescribirSuscripciones(IEnumerable<(string Codigo, DateOnly? Desde, DateOnly? Hasta)> items) =>
        string.Join("; ", items.OrderBy(i => i.Codigo, StringComparer.Ordinal)
            .Select(i => $"{i.Codigo} [{AuditDiff.Describir(i.Desde) ?? "…"}..{AuditDiff.Describir(i.Hasta) ?? "…"}]"));

    /// <summary>Rango cerrado de fechas; los límites abiertos (nulos) se representan con <see cref="DateOnly.MinValue"/> / <see cref="DateOnly.MaxValue"/>.</summary>
    private readonly record struct Rango(DateOnly Desde, DateOnly Hasta)
    {
        public static Rango De(DateOnly? desde, DateOnly? hasta) => new(desde ?? DateOnly.MinValue, hasta ?? DateOnly.MaxValue);
        public bool Contiene(DateOnly fecha) => fecha >= Desde && fecha <= Hasta;

        /// <summary>Partes de este rango que no están en <paramref name="otro"/> (0, 1 o 2 rangos).</summary>
        public IEnumerable<Rango> Menos(Rango otro)
        {
            if (otro.Hasta < Desde || otro.Desde > Hasta) { yield return this; yield break; }
            if (otro.Desde > Desde) yield return new(Desde, otro.Desde.AddDays(-1));
            if (otro.Hasta < Hasta) yield return new(otro.Hasta.AddDays(1), Hasta);
        }
    }

    // ───────────────────────────── Validación y mapeo ─────────────────────────────

    /// <summary>Convierte los días <c>mon</c>…<c>sun</c> de la API en la semana ISO 1..7 y aplica las reglas de RF-LO-24.</summary>
    private static (List<JornadaDia>? Semana, IHttpResult<T>? Error) ParsearSemana<T>(Dictionary<string, List<IntervaloDto>>? dias)
    {
        if (dias is null || dias.Count == 0)
            return (null, Falla<T>("jornada", "La jornada debe tener al menos un intervalo laboral en la semana."));

        var porDia = new Dictionary<int, List<IntervaloLaboral>>();
        foreach (var (token, lista) in dias)
        {
            if (CalendarioMapper.DiaDesdeToken(token) is not { } dia)
                return (null, Falla<T>("jornada", $"Día desconocido «{token}»; use mon, tue, wed, thu, fri, sat o sun."));
            if (porDia.ContainsKey(dia))
                return (null, Falla<T>("jornada", $"El día «{token}» está repetido."));

            var parseo = CalendarioMapper.ParseIntervalos(lista);
            if (parseo.ErrorFormato is not null)
                return (null, Falla<T>("intervalos", parseo.ErrorFormato));
            if (parseo.ErrorSemantico is not null)
                return (null, Falla<T>("jornada", $"{CalendarioMapper.TokensDia[dia - 1]}: {parseo.ErrorSemantico}"));
            porDia[dia] = parseo.Valores.ToList();
        }

        var semana = Enumerable.Range(1, 7)
            .Select(d => new JornadaDia { Dia = d, Intervalos = porDia.GetValueOrDefault(d) ?? [] })
            .ToList();
        var errores = JornadaValidator.Validar(semana);
        return errores.Count == 0
            ? (semana, null)
            : (null, CalendarioResults.FailMany<T>("Jornada inválida", "jornada", errores, HttpStatusCode.BadRequest));
    }

    private static List<JornadaVersionDto> MapVersiones(IReadOnlyList<LocalidadJornada> ordenadas, DateOnly hoy)
    {
        var vigente = ordenadas.Where(j => j.VigenteDesde <= hoy).Select(j => (DateOnly?)j.VigenteDesde).DefaultIfEmpty(null).Max();
        var resultado = new List<JornadaVersionDto>(ordenadas.Count);
        for (var i = 0; i < ordenadas.Count; i++)
        {
            var j = ordenadas[i];
            resultado.Add(new JornadaVersionDto
            {
                Id = j.Id,
                VigenteDesde = j.VigenteDesde,
                VigenteHasta = i + 1 < ordenadas.Count ? ordenadas[i + 1].VigenteDesde.AddDays(-1) : null,
                Estado = j.VigenteDesde > hoy ? EstadoJornada.Programada : j.VigenteDesde == vigente ? EstadoJornada.Vigente : EstadoJornada.Pasada,
                Dias = DiasDto(j.Semana),
                Motivo = j.Motivo,
                CreadoPor = j.CreadoPor,
                CreadoEn = j.CreatedAt,
            });
        }
        return resultado;
    }

    private static Dictionary<string, List<IntervaloDto>> DiasDto(IEnumerable<JornadaDia> semana)
    {
        var porDia = semana.ToDictionary(d => d.Dia, d => d.Intervalos);
        return Enumerable.Range(1, 7).ToDictionary(
            d => CalendarioMapper.TokenDeDia(d),
            d => CalendarioMapper.ToDtos(porDia.GetValueOrDefault(d)));
    }

    /// <summary>Último día en que rige una versión que empieza en <paramref name="desde"/>: la víspera de la siguiente (nulo si es la última).</summary>
    private static DateOnly? UltimoDiaDeVigencia(DateOnly desde, IEnumerable<DateOnly> otrasVersiones)
    {
        var siguiente = otrasVersiones.Where(v => v > desde).Select(v => (DateOnly?)v).Min();
        return siguiente?.AddDays(-1);
    }

    private Task<Localidad?> CargarLocalidadAsync(CalendarioActor actor, Guid id, CancellationToken ct) =>
        Ctx.Localidades.AsNoTracking().FirstOrDefaultAsync(
            l => l.Id == id && l.OrganizationId == actor.OrganizationId && l.DeletedAt == null, ct);

    private static string Fmt(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
    private static string? LimpiarTexto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static IHttpResult<T> Falla<T>(string key, string msg) =>
        CalendarioResults.Fail<T>("Datos inválidos del calendario", key, msg, HttpStatusCode.BadRequest);

    private static IHttpResult<T> NoEncontrada<T>() =>
        CalendarioResults.Fail<T>("Localidad no encontrada", "id", "Localidad no encontrada.", HttpStatusCode.NotFound);

    private static IHttpResult NoEncontrada() =>
        CalendarioResults.Fail("Localidad no encontrada", "id", "Localidad no encontrada.", HttpStatusCode.NotFound);

    /// <summary>Guarda y traduce una colisión de concurrencia (versión del calendario) a 412 (RF-LO-42).</summary>
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
