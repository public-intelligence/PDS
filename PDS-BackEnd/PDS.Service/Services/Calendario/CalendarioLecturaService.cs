using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using NodaTime;
using PDS.Domain.DTOs.Calendario;
using PDS.Domain.Response;
using PDS.Domain.ValueObjects;
using PDS.Service.Services.Calendario.Motor;
using PDS.Service.Settings;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// Respuesta cacheable (RNF-LO-02): <see cref="ETag"/> siempre presente; si el <c>If-None-Match</c> del cliente ya lo incluye,
/// <see cref="NoModificado"/> es verdadero y <see cref="Valor"/> es nulo (el cuerpo ni siquiera se calcula).
/// </summary>
public sealed record Lectura<T>(T? Valor, string ETag, bool NoModificado) where T : class;

/// <summary>
/// Lectura del calendario efectivo de una localidad para usuarios, M2M y SDS (SPEC-LOC-CAL-01 RF-LO-34, RF-LO-43; spec M2M de SDS #2
/// y #3). Solo lectura: el tenant lo decide quien llama (token de usuario o ruta M2M) y una localidad de otra organización responde 404.
/// El ETag deriva de <c>CalendarioVersion</c> y los parámetros de la consulta, así que cambia exactamente cuando puede cambiar la respuesta.
/// </summary>
public interface ICalendarioLecturaService
{
    /// <summary>RF-LO-34: calendario efectivo de <paramref name="desde"/> a <paramref name="hasta"/> (ambos obligatorios, ≤ <c>MaxRangoDias</c>).</summary>
    Task<IHttpResult<Lectura<CalendarioEfectivoDto>>> GetEfectivoAsync(
        int organizationId, Guid localidadId, DateOnly? desde, DateOnly? hasta, string? ifNoneMatch, CancellationToken ct = default);

    /// <summary>RF-LO-34 <c>estado</c>: ¿trabaja la localidad en <paramref name="instante"/> (por defecto ahora)?</summary>
    Task<IHttpResult<Lectura<EstadoCalendarioDto>>> GetEstadoAsync(
        int organizationId, Guid localidadId, DateTimeOffset? instante, string? ifNoneMatch, CancellationToken ct = default);

    /// <summary>RF-LO-43: feed de cambios de la organización posterior a <paramref name="cursor"/> (<paramref name="limite"/> se limita a 1..1000).</summary>
    Task<IHttpResult<Lectura<CalendarioCambiosDto>>> GetCambiosAsync(
        int organizationId, long cursor, int? limite, string? ifNoneMatch, CancellationToken ct = default);

    /// <summary>Spec M2M #2: horario semanal vigente en <paramref name="on"/> (por defecto hoy en la zona de la localidad).</summary>
    Task<IHttpResult<Lectura<WorkingHoursDto>>> GetWorkingHoursAsync(
        int organizationId, Guid localidadId, DateOnly? on, string? ifNoneMatch, CancellationToken ct = default);

    /// <summary>Spec M2M #3: cierres de <paramref name="from"/> a <paramref name="to"/> (ambos obligatorios, ≤ <c>MaxRangoDias</c>).</summary>
    Task<IHttpResult<Lectura<LocalityClosuresDto>>> GetClosuresAsync(
        int organizationId, Guid localidadId, DateOnly? from, DateOnly? to, string? ifNoneMatch, CancellationToken ct = default);
}

public class CalendarioLecturaService(
    ICalendarioSnapshotService snapshots,
    ICalendarioCambioService cambios,
    ICalendarioEfectivoResolver resolver,
    IRetroactiveWindowPolicy hoy,
    TimeProvider clock,
    IOptions<CalendarioSettings> settings) : ICalendarioLecturaService
{
    private CalendarioSettings Config => settings.Value;

    // ───────────────────────────── Calendario efectivo ─────────────────────────────

    public async Task<IHttpResult<Lectura<CalendarioEfectivoDto>>> GetEfectivoAsync(
        int organizationId, Guid localidadId, DateOnly? desde, DateOnly? hasta, string? ifNoneMatch, CancellationToken ct = default)
    {
        var rango = ValidarRango<CalendarioEfectivoDto>(desde, hasta, "desde", "hasta");
        if (rango is not null) return rango;

        var loc = await snapshots.GetBaseAsync(organizationId, localidadId, ct);
        if (loc is null) return NoEncontrada<CalendarioEfectivoDto>();

        var etag = ETags.FromParts("calendario-efectivo", Id(loc), Version(loc), Fecha(desde!.Value), Fecha(hasta!.Value));
        if (ETags.Matches(ifNoneMatch, etag)) return Sin304<CalendarioEfectivoDto>(etag);

        var cargado = await snapshots.CargarAsync(loc, ct);
        var dias = resolver.ResolverRango(cargado.Snapshot, desde.Value, hasta.Value);
        var dto = new CalendarioEfectivoDto
        {
            LocalidadId = loc.Id,
            ZonaHoraria = loc.ZonaHoraria,
            TzdbVersion = TzdbInfo.Version,
            CalendarioVersion = loc.CalendarioVersion,
            Desde = desde.Value,
            Hasta = hasta.Value,
            Dias = dias.Select(MapDia).ToList(),
        };
        return Ok(dto, etag);
    }

    public async Task<IHttpResult<Lectura<EstadoCalendarioDto>>> GetEstadoAsync(
        int organizationId, Guid localidadId, DateTimeOffset? instante, string? ifNoneMatch, CancellationToken ct = default)
    {
        var loc = await snapshots.GetBaseAsync(organizationId, localidadId, ct);
        if (loc is null) return NoEncontrada<EstadoCalendarioDto>();

        var momento = Instant.FromDateTimeOffset(instante ?? clock.GetUtcNow());
        var etag = ETags.FromParts("calendario-estado", Id(loc), Version(loc), momento.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
        if (ETags.Matches(ifNoneMatch, etag)) return Sin304<EstadoCalendarioDto>(etag);

        var cargado = await snapshots.CargarAsync(loc, ct);
        var estado = resolver.ObtenerEstado(cargado.Snapshot, momento, Math.Max(1, Config.HorizonteCalculoDias));
        var zona = DateTimeZoneProviders.Tzdb[loc.ZonaHoraria];
        var local = momento.InZone(zona).Date;
        var dto = new EstadoCalendarioDto
        {
            LocalidadId = loc.Id,
            ZonaHoraria = loc.ZonaHoraria,
            TzdbVersion = TzdbInfo.Version,
            CalendarioVersion = loc.CalendarioVersion,
            Instante = momento.ToDateTimeUtc(),
            FechaLocal = new DateOnly(local.Year, local.Month, local.Day),
            Laborando = estado.Laborando,
            IntervaloActual = estado.IntervaloActual is { } iv ? MapUtc(iv) : null,
            ProximoInstanteLaborable = estado.ProximoInstanteLaborable?.ToDateTimeUtc(),
        };
        return Ok(dto, etag);
    }

    // ───────────────────────────── Feed de cambios ─────────────────────────────

    public async Task<IHttpResult<Lectura<CalendarioCambiosDto>>> GetCambiosAsync(
        int organizationId, long cursor, int? limite, string? ifNoneMatch, CancellationToken ct = default)
    {
        if (cursor < 0)
            return Falla<CalendarioCambiosDto>("Cursor inválido", "cursor", "El cursor no puede ser negativo.", HttpStatusCode.BadRequest);

        var pagina = await cambios.ObtenerCambiosAsync(organizationId, cursor, limite ?? 200, ct);
        if (pagina.CursorExpirado)
            return Falla<CalendarioCambiosDto>("Cursor expirado", "cursor",
                "El cursor ya no está disponible porque los cambios se purgaron; resincronice el calendario completo.", HttpStatusCode.Gone);

        var siguiente = pagina.SiguienteCursor(cursor);
        var etag = ETags.FromParts("calendario-cambios", organizationId.ToString(CultureInfo.InvariantCulture),
            cursor.ToString(CultureInfo.InvariantCulture), siguiente.ToString(CultureInfo.InvariantCulture),
            pagina.Cambios.Count.ToString(CultureInfo.InvariantCulture), pagina.HayMas ? "1" : "0");
        if (ETags.Matches(ifNoneMatch, etag)) return Sin304<CalendarioCambiosDto>(etag);

        var dto = new CalendarioCambiosDto
        {
            Items = pagina.Cambios.Select(c => new CalendarioCambioDto
            {
                Cursor = c.Id,
                LocalidadId = c.LocalidadId,
                CalendarioVersion = c.CalendarioVersion,
                Motivo = c.Motivo,
                FechaDesde = c.FechaDesde,
                FechaHasta = c.FechaHasta,
                OcurridoEn = DateTime.SpecifyKind(c.OcurridoEn, DateTimeKind.Utc),
            }).ToList(),
            SiguienteCursor = siguiente,
            HayMas = pagina.HayMas,
        };
        return Ok(dto, etag);
    }

    // ───────────────────────────── Spec M2M de SDS #2: working-hours ─────────────────────────────

    public async Task<IHttpResult<Lectura<WorkingHoursDto>>> GetWorkingHoursAsync(
        int organizationId, Guid localidadId, DateOnly? on, string? ifNoneMatch, CancellationToken ct = default)
    {
        var loc = await snapshots.GetBaseAsync(organizationId, localidadId, ct);
        if (loc is null) return NoEncontrada<WorkingHoursDto>();

        var fecha = on ?? hoy.HoyLocal(loc.ZonaHoraria);
        var etag = ETags.FromParts("working-hours", Id(loc), Version(loc), Fecha(fecha));
        if (ETags.Matches(ifNoneMatch, etag)) return Sin304<WorkingHoursDto>(etag);

        var cargado = await snapshots.CargarAsync(loc, ct);
        var jornadas = cargado.Snapshot.Jornadas;

        // RF-LO-25: rige la versión de mayor VigenteDesde ≤ fecha. Si ninguna rige (fecha anterior a la primera versión) no hay
        // horario que informar: 404 con clave 'jornada' (no se inventa un horario ni se usa la primera versión «hacia atrás»).
        var version = JornadaValidator.VersionVigente(jornadas, fecha);
        if (version is null)
            return Falla<WorkingHoursDto>("Sin horario vigente", "jornada",
                $"La localidad no tiene un horario laboral vigente el {Fecha(fecha)}.", HttpStatusCode.NotFound);

        var siguiente = jornadas.Where(j => j.VigenteDesde > version.VigenteDesde).Select(j => (DateOnly?)j.VigenteDesde).Min();
        var dto = new WorkingHoursDto
        {
            LocalityId = loc.Id,
            ValidFrom = version.VigenteDesde,
            ValidTo = siguiente?.AddDays(-1),
            Version = Version(loc),
            // Siempre los siete días (1 = lunes … 7 = domingo); un día cerrado lleva la lista vacía.
            Days = Enumerable.Range(1, 7).Select(dia => new WorkingHoursDayDto
            {
                Weekday = dia,
                Intervals = version.Intervalos(dia).OrderBy(i => i.InicioMinuto)
                    .Select(i => new List<string> { IntervaloLaboral.FormatMinuto(i.InicioMinuto), IntervaloLaboral.FormatMinuto(i.FinMinuto) })
                    .ToList(),
            }).ToList(),
        };
        return Ok(dto, etag);
    }

    // ───────────────────────────── Spec M2M de SDS #3: calendar ─────────────────────────────

    public async Task<IHttpResult<Lectura<LocalityClosuresDto>>> GetClosuresAsync(
        int organizationId, Guid localidadId, DateOnly? from, DateOnly? to, string? ifNoneMatch, CancellationToken ct = default)
    {
        var rango = ValidarRango<LocalityClosuresDto>(from, to, "from", "to");
        if (rango is not null) return rango;

        var loc = await snapshots.GetBaseAsync(organizationId, localidadId, ct);
        if (loc is null) return NoEncontrada<LocalityClosuresDto>();

        var etag = ETags.FromParts("calendar-closures", Id(loc), Version(loc), Fecha(from!.Value), Fecha(to!.Value));
        if (ETags.Matches(ifNoneMatch, etag)) return Sin304<LocalityClosuresDto>(etag);

        var cargado = await snapshots.CargarAsync(loc, ct);
        var dias = resolver.ResolverRango(cargado.Snapshot, from.Value, to.Value);
        var cierres = CierresCalendario.Construir(cargado.Snapshot, cargado.LocalidadPaisId, cargado.PaisCodigos, dias);

        var dto = new LocalityClosuresDto
        {
            LocalityId = loc.Id,
            From = from.Value,
            To = to.Value,
            Version = Version(loc),
            Closures = cierres.Select(c => new LocalityClosureDto
            {
                Date = c.Fecha,
                Kind = c.Completo ? "full" : "partial",
                From = c.Ventana is { } v ? IntervaloLaboral.FormatMinuto(v.InicioMinuto) : null,
                To = c.Ventana is { } w ? IntervaloLaboral.FormatMinuto(w.FinMinuto) : null,
                Reason = c.Motivo,
                Source = c.Fuente switch { FuenteCierre.Host => "host", FuenteCierre.Home => "home", _ => "mission" },
            }).ToList(),
        };
        return Ok(dto, etag);
    }

    // ───────────────────────────── Utilidades ─────────────────────────────

    // §10.5 'rango' (400): ambos extremos obligatorios, desde ≤ hasta y a lo sumo MaxRangoDias días (inclusive).
    private IHttpResult<Lectura<T>>? ValidarRango<T>(DateOnly? desde, DateOnly? hasta, string nombreDesde, string nombreHasta) where T : class
    {
        if (desde is null || hasta is null)
            return Falla<T>("Rango inválido", "rango", $"Los parámetros «{nombreDesde}» y «{nombreHasta}» son obligatorios (yyyy-MM-dd).", HttpStatusCode.BadRequest);
        if (desde > hasta)
            return Falla<T>("Rango inválido", "rango", $"«{nombreDesde}» no puede ser posterior a «{nombreHasta}».", HttpStatusCode.BadRequest);
        var max = Math.Max(1, Config.MaxRangoDias);
        if (hasta.Value.DayNumber - desde.Value.DayNumber + 1 > max)
            return Falla<T>("Rango inválido", "rango", $"El rango no puede superar {max} días.", HttpStatusCode.BadRequest);
        return null;
    }

    private static DiaEfectivoDto MapDia(DiaEfectivo d) => new()
    {
        Fecha = d.Fecha,
        DiaSemana = d.DiaSemana,
        Laborable = d.Laborable,
        Intervalos = CalendarioMapper.ToDtos(d.Intervalos),
        IntervalosUtc = d.IntervalosUtc.Select(MapUtc).ToList(),
        Fuente = d.Fuente,
        Motivos = d.Motivos.Select(m => new MotivoDiaDto
        {
            Tipo = m.Tipo,
            Nombre = m.Nombre,
            CalendarioFeriadosId = m.CalendarioFeriadosId,
            ExcepcionId = m.ExcepcionId,
            ReferenciaNormativa = m.ReferenciaNormativa,
        }).ToList(),
    };

    private static IntervaloUtcDto MapUtc(IntervaloUtc iv) => new(iv.Inicio.ToDateTimeUtc(), iv.Fin.ToDateTimeUtc());

    private static string Id(LocalidadCalendarioBase l) => l.Id.ToString("N");
    private static string Version(LocalidadCalendarioBase l) => l.CalendarioVersion.ToString(CultureInfo.InvariantCulture);
    private static string Fecha(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static IHttpResult<Lectura<T>> Ok<T>(T valor, string etag) where T : class =>
        HttpResult<Lectura<T>>.Success(new Lectura<T>(valor, etag, false));

    private static IHttpResult<Lectura<T>> Sin304<T>(string etag) where T : class =>
        HttpResult<Lectura<T>>.Success(new Lectura<T>(null, etag, true));

    private static IHttpResult<Lectura<T>> NoEncontrada<T>() where T : class =>
        Falla<T>("Localidad no encontrada", "id", "Localidad no encontrada.", HttpStatusCode.NotFound);

    private static IHttpResult<Lectura<T>> Falla<T>(string titulo, string clave, string mensaje, HttpStatusCode codigo) where T : class =>
        CalendarioResults.Fail<Lectura<T>>(titulo, clave, mensaje, codigo);
}
