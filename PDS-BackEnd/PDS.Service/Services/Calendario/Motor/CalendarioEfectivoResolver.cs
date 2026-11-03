using NodaTime;
using NodaTime.TimeZones;
using PDS.Domain.Enums;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario.Motor;

/// <summary>
/// RF-LO-34: motor puro que resuelve el calendario efectivo de una localidad (SPEC-LOC-CAL-01 §10.2). Sin EF, sin E/S y sin reloj:
/// todo llega en el <see cref="CalendarioSnapshot"/>.
/// </summary>
public interface ICalendarioEfectivoResolver
{
    DiaEfectivo ResolverDia(CalendarioSnapshot calendario, DateOnly fechaLocal);

    /// <summary>Resuelve cada día del rango, ambos extremos inclusive.</summary>
    IReadOnlyList<DiaEfectivo> ResolverRango(CalendarioSnapshot calendario, DateOnly desde, DateOnly hasta);

    /// <summary>
    /// RF-LO-34 <c>estado</c>: si la localidad trabaja en <paramref name="instante"/>, su intervalo actual y el próximo instante
    /// laborable (inicio del siguiente intervalo posterior al instante), buscando hasta <paramref name="horizonteDias"/>.
    /// </summary>
    EstadoCalendario ObtenerEstado(CalendarioSnapshot calendario, Instant instante, int horizonteDias = 366);
}

public sealed class CalendarioEfectivoResolver : ICalendarioEfectivoResolver
{
    private static readonly string[] Tokens = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"];
    private readonly IDateTimeZoneProvider _tz;

    public CalendarioEfectivoResolver() : this(DateTimeZoneProviders.Tzdb) { }

    public CalendarioEfectivoResolver(IDateTimeZoneProvider tz) => _tz = tz;

    public DiaEfectivo ResolverDia(CalendarioSnapshot calendario, DateOnly fechaLocal) =>
        new Contexto(calendario, ObtenerZona(calendario)).Resolver(fechaLocal);

    public IReadOnlyList<DiaEfectivo> ResolverRango(CalendarioSnapshot calendario, DateOnly desde, DateOnly hasta)
    {
        if (desde > hasta) throw new ArgumentException("'desde' no puede ser posterior a 'hasta'.", nameof(desde));
        var ctx = new Contexto(calendario, ObtenerZona(calendario));
        var dias = new List<DiaEfectivo>();
        for (var d = desde; d <= hasta; d = d.AddDays(1)) dias.Add(ctx.Resolver(d));
        return dias;
    }

    public EstadoCalendario ObtenerEstado(CalendarioSnapshot calendario, Instant instante, int horizonteDias = 366)
    {
        var zona = ObtenerZona(calendario);
        var ctx = new Contexto(calendario, zona);
        var hoy = instante.InZone(zona).Date;
        IntervaloUtc? actual = null;
        Instant? proximo = null;

        // Un día local solo contiene intervalos dentro de ese día; basta recorrer desde el día local del instante.
        for (var k = 0; k <= horizonteDias; k++)
        {
            var d = hoy.PlusDays(k);
            foreach (var iv in ctx.Resolver(new DateOnly(d.Year, d.Month, d.Day)).IntervalosUtc.OrderBy(i => i.Inicio))
            {
                if (actual is null && iv.Inicio <= instante && instante < iv.Fin) { actual = iv; continue; }
                if (iv.Inicio > instante && (proximo is null || iv.Inicio < proximo)) proximo = iv.Inicio;
            }
            if (proximo is not null) break;
        }
        return new EstadoCalendario(actual is not null, actual, proximo);
    }

    private DateTimeZone ObtenerZona(CalendarioSnapshot c) =>
        _tz.GetZoneOrNull(c.ZonaHoraria) ?? throw new ArgumentException($"Zona horaria desconocida: '{c.ZonaHoraria}'.", nameof(c));

    private sealed class Contexto(CalendarioSnapshot cal, DateTimeZone zona)
    {
        private readonly ILookup<DateOnly, FeriadoAplicable> _feriados = cal.Feriados.ToLookup(f => f.Fecha);

        // §10.2
        public DiaEfectivo Resolver(DateOnly d)
        {
            var version = JornadaValidator.VersionVigente(cal.Jornadas, d);
            var baseIv = version is null ? [] : Ordenar(version.Intervalos(DiaIso(d)));

            // 1) Antes de la apertura.
            if (cal.FechaApertura is { } apertura && d < apertura)
                return NoLaborable(d, FuenteDiaCalendario.CicloVida, [new MotivoDia("Apertura", "Antes de la apertura")], baseIv);

            // 2) Excepciones de sistema (suspensión, cierre definitivo): ganan a todo.
            var sistema = cal.Excepciones.Where(e => e.EsSistema && e.Cubre(d)).OrderBy(e => e.FechaInicio).ThenBy(e => e.Id).ToList();
            if (sistema.Count > 0)
                return NoLaborable(d, FuenteDiaCalendario.CicloVida, sistema.Select(MotivoDeSistema).ToList(), baseIv);

            // 3) Excepción de usuario (a lo sumo una; si hubiera datos inválidos con varias, gana la que empezó más tarde).
            var ex = cal.Excepciones.Where(e => !e.EsSistema && e.Cubre(d)).OrderByDescending(e => e.FechaInicio).ThenBy(e => e.Id).FirstOrDefault();
            if (ex is not null)
            {
                var motivo = MotivoDeExcepcion(ex);
                return ex.Tipo == TipoExcepcionCalendario.Cierre || ex.Intervalos.Count == 0
                    ? NoLaborable(d, FuenteDiaCalendario.Excepcion, [motivo], baseIv)
                    : Laborable(d, Ordenar(ex.Intervalos), FuenteDiaCalendario.Excepcion, [motivo], baseIv);
            }

            // 4) Feriados de calendarios suscritos y vigentes ese día.
            var fer = _feriados[d].Where(f => (f.SuscripcionDesde is null || f.SuscripcionDesde <= d) && (f.SuscripcionHasta is null || d <= f.SuscripcionHasta))
                .OrderBy(f => f.CalendarioCodigo, StringComparer.Ordinal).ThenBy(f => f.Nombre, StringComparer.Ordinal).ToList();
            if (fer.Count > 0)
            {
                var motivos = fer.Select(MotivoDeFeriado).ToList();
                if (fer.Any(f => f.Alcance == AlcanceFeriado.DiaCompleto))
                    return NoLaborable(d, FuenteDiaCalendario.Feriado, motivos, baseIv);

                // Jornada reducida: base ∩ (intersección de los intervalos de cada entrada reducida).
                var iv = IntervaloLaboral.Normalizar(baseIv);
                foreach (var f in fer) iv = IntervaloLaboral.Interseccion(iv, f.Intervalos);
                return iv.Count == 0
                    ? NoLaborable(d, FuenteDiaCalendario.Feriado, motivos, baseIv)
                    : Laborable(d, iv, FuenteDiaCalendario.Feriado, motivos, baseIv);
            }

            // 5) Jornada semanal.
            return baseIv.Count == 0
                ? NoLaborable(d, FuenteDiaCalendario.Jornada, [], baseIv)
                : Laborable(d, baseIv, FuenteDiaCalendario.Jornada, [], baseIv);
        }

        private DiaEfectivo Laborable(DateOnly d, IReadOnlyList<IntervaloLaboral> iv, FuenteDiaCalendario fuente, IReadOnlyList<MotivoDia> motivos, IReadOnlyList<IntervaloLaboral> jornada) =>
            new(d, Token(d), true, iv, AUtc(d, iv), fuente, motivos, jornada);

        private DiaEfectivo NoLaborable(DateOnly d, FuenteDiaCalendario fuente, IReadOnlyList<MotivoDia> motivos, IReadOnlyList<IntervaloLaboral> jornada) =>
            new(d, Token(d), false, [], [], fuente, motivos, jornada);

        // §10.1: resolver indulgente (hueco => se adelanta; ambigüedad => offset más temprano); 24:00 = día siguiente 00:00.
        private List<IntervaloUtc> AUtc(DateOnly d, IEnumerable<IntervaloLaboral> iv)
        {
            var fecha = new LocalDate(d.Year, d.Month, d.Day);
            var res = new List<IntervaloUtc>();
            foreach (var i in iv)
            {
                var ini = zona.ResolveLocal(fecha.AtMidnight().PlusMinutes(i.InicioMinuto), Resolvers.LenientResolver).ToInstant();
                var fin = zona.ResolveLocal(fecha.AtMidnight().PlusMinutes(i.FinMinuto), Resolvers.LenientResolver).ToInstant();
                if (fin > ini) res.Add(new IntervaloUtc(ini, fin));     // si el fin no es posterior al inicio (solo dentro de un hueco) se descarta
            }
            return res;
        }

        private static int DiaIso(DateOnly d) => d.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)d.DayOfWeek;
        private static string Token(DateOnly d) => Tokens[DiaIso(d) - 1];
        private static List<IntervaloLaboral> Ordenar(IEnumerable<IntervaloLaboral> iv) => iv.OrderBy(i => i.InicioMinuto).ToList();

        private static MotivoDia MotivoDeSistema(ExcepcionAplicable e) =>
            new(e.Categoria.ToString(), e.Motivo, ExcepcionId: e.Id, ReferenciaNormativa: e.ReferenciaNormativa);

        private static MotivoDia MotivoDeExcepcion(ExcepcionAplicable e) =>
            new(e.Tipo.ToString(), e.Motivo, ExcepcionId: e.Id, ReferenciaNormativa: e.ReferenciaNormativa);

        private static MotivoDia MotivoDeFeriado(FeriadoAplicable f) =>
            new("Feriado", f.Nombre, CalendarioFeriadosId: f.CalendarioFeriadosId, CalendarioCodigo: f.CalendarioCodigo, PaisId: f.PaisId, Ambito: f.Ambito, Alcance: f.Alcance);
    }
}
