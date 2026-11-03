using NodaTime;

namespace PDS.Service.Services.Calendario.Motor;

/// <summary>Unidades de plazo del vocabulario de WF (SPEC-LOC-CAL-01 §10.3).</summary>
public enum UnidadTiempo { Min, Hours, Bizdays, Caldays }

/// <summary>Vencimiento calculado: instante UTC y su equivalente local en la zona de la localidad.</summary>
public sealed record VencimientoCalculado(Instant Instante, LocalDateTime Local, string ZonaHoraria);

/// <summary>
/// RF-LO-35: aritmética de tiempo laborable, convención v1 (SPEC-LOC-CAL-01 §10.3). Motor puro: sin EF, sin E/S y sin reloj
/// (RNF-LO-08); trabaja sobre el calendario efectivo que resuelve <see cref="ICalendarioEfectivoResolver"/>. Los endpoints M2M y
/// las pruebas del Apéndice B ejecutan exactamente este código.
/// </summary>
public interface ITiempoLaborableCalculator
{
    /// <summary>
    /// Fecha de vencimiento de un plazo que arranca en <paramref name="inicio"/>. Devuelve <c>null</c> si no se alcanza dentro de
    /// <paramref name="horizonteDias"/> (el llamador responde 422 <c>horizonte</c>). <paramref name="cantidad"/> debe ser ≥ 1.
    /// </summary>
    VencimientoCalculado? CalcularVencimiento(CalendarioSnapshot calendario, Instant inicio, int cantidad, UnidadTiempo unidad, int horizonteDias = 366);

    /// <summary>
    /// Minutos laborables enteros en <c>[desde, hasta)</c>. Devuelve <c>null</c> si el rango supera <paramref name="horizonteDias"/> días
    /// locales. <paramref name="desde"/> no puede ser posterior a <paramref name="hasta"/>.
    /// </summary>
    long? MinutosLaborables(CalendarioSnapshot calendario, Instant desde, Instant hasta, int horizonteDias = 366);
}

public sealed class TiempoLaborableCalculator : ITiempoLaborableCalculator
{
    // Los días se resuelven por tandas: ResolverRango arma sus índices una vez por llamada, no una vez por día.
    private const int TamanoTanda = 32;

    private readonly ICalendarioEfectivoResolver _resolver;
    private readonly IDateTimeZoneProvider _tz;

    public TiempoLaborableCalculator(ICalendarioEfectivoResolver resolver) : this(resolver, DateTimeZoneProviders.Tzdb) { }

    public TiempoLaborableCalculator(ICalendarioEfectivoResolver resolver, IDateTimeZoneProvider tz)
    {
        _resolver = resolver;
        _tz = tz;
    }

    public VencimientoCalculado? CalcularVencimiento(CalendarioSnapshot calendario, Instant inicio, int cantidad, UnidadTiempo unidad, int horizonteDias = 366)
    {
        if (cantidad < 1) throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad debe ser al menos 1.");
        var zona = ObtenerZona(calendario);
        var d0 = FechaLocal(inicio, zona);

        switch (unidad)
        {
            case UnidadTiempo.Min:
            case UnidadTiempo.Hours:
            {
                var restante = Duration.FromMinutes((long)cantidad * (unidad == UnidadTiempo.Hours ? 60 : 1));
                foreach (var dia in Dias(calendario, d0, d0.AddDays(horizonteDias)))
                foreach (var iv in dia.IntervalosUtc.OrderBy(i => i.Inicio))
                {
                    if (iv.Fin <= inicio) continue;
                    var arranque = iv.Inicio > inicio ? iv.Inicio : inicio;
                    var disponible = iv.Fin - arranque;
                    if (disponible >= restante) return Construir(arranque + restante, zona, calendario);
                    restante -= disponible;
                }
                return null;
            }

            case UnidadTiempo.Bizdays:
            {
                // El n-ésimo día laborable estrictamente posterior al día local de inicio; vence al cierre de su último intervalo.
                var contados = 0;
                foreach (var dia in Dias(calendario, d0.AddDays(1), d0.AddDays(horizonteDias)))
                {
                    if (dia.IntervalosUtc.Count == 0) continue;
                    if (++contados == cantidad) return Construir(dia.IntervalosUtc.Max(i => i.Fin), zona, calendario);
                }
                return null;
            }

            case UnidadTiempo.Caldays:
            {
                if (cantidad > horizonteDias) return null;
                // Medianoche local al final del día D0 + n (inicio de D0 + n + 1).
                var fin = zona.AtStartOfDay(ALocalDate(d0.AddDays(cantidad + 1))).ToInstant();
                return Construir(fin, zona, calendario);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(unidad), unidad, "Unidad desconocida.");
        }
    }

    public long? MinutosLaborables(CalendarioSnapshot calendario, Instant desde, Instant hasta, int horizonteDias = 366)
    {
        if (desde > hasta) throw new ArgumentException("'desde' no puede ser posterior a 'hasta'.", nameof(desde));
        if (desde == hasta) return 0;

        var zona = ObtenerZona(calendario);
        var primero = FechaLocal(desde, zona);
        var ultimo = FechaLocal(hasta, zona);
        if (ultimo.DayNumber - primero.DayNumber > horizonteDias) return null;

        var total = Duration.Zero;
        foreach (var dia in Dias(calendario, primero, ultimo))
        foreach (var iv in dia.IntervalosUtc)
        {
            var a = iv.Inicio > desde ? iv.Inicio : desde;
            var b = iv.Fin < hasta ? iv.Fin : hasta;
            if (b > a) total += b - a;
        }
        return (long)Math.Floor(total.TotalMinutes);
    }

    private IEnumerable<DiaEfectivo> Dias(CalendarioSnapshot calendario, DateOnly desde, DateOnly hasta)
    {
        for (var d = desde; d <= hasta; d = d.AddDays(TamanoTanda))
        {
            var finTanda = d.AddDays(TamanoTanda - 1);
            foreach (var dia in _resolver.ResolverRango(calendario, d, finTanda < hasta ? finTanda : hasta)) yield return dia;
        }
    }

    private static VencimientoCalculado Construir(Instant instante, DateTimeZone zona, CalendarioSnapshot c) =>
        new(instante, instante.InZone(zona).LocalDateTime, c.ZonaHoraria);

    private static DateOnly FechaLocal(Instant instante, DateTimeZone zona)
    {
        var f = instante.InZone(zona).Date;
        return new DateOnly(f.Year, f.Month, f.Day);
    }

    private static LocalDate ALocalDate(DateOnly d) => new(d.Year, d.Month, d.Day);

    private DateTimeZone ObtenerZona(CalendarioSnapshot c) =>
        _tz.GetZoneOrNull(c.ZonaHoraria) ?? throw new ArgumentException($"Zona horaria desconocida: '{c.ZonaHoraria}'.", nameof(c));
}
