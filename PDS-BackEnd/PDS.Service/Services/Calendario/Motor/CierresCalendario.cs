using PDS.Domain.Enums;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario.Motor;

/// <summary>Quién decidió el cierre, desde el punto de vista de la localidad (spec M2M de SDS #3 <c>source</c>).</summary>
public enum FuenteCierre
{
    /// <summary>País anfitrión.</summary>
    Host,

    /// <summary>País propio (República Dominicana).</summary>
    Home,

    /// <summary>La propia representación o institución (decisión de la oficina, ciclo de vida, organismos).</summary>
    Mission,
}

/// <summary>Un cierre de un día: completo, o una ventana local [Inicio, Fin) cerrada dentro de la jornada.</summary>
public sealed record CierreDia(DateOnly Fecha, IntervaloLaboral? Ventana, string Motivo, FuenteCierre Fuente)
{
    public bool Completo => Ventana is null;
}

/// <summary>
/// Deriva los cierres que SDS necesita (spec M2M #3) a partir del calendario efectivo ya resuelto por el motor. Puro: sin EF ni reloj.
/// <para>
/// Qué es un cierre: lo que le QUITA tiempo a una jornada semanal laborable. Por eso (decisiones documentadas):
/// </para>
/// <list type="bullet">
/// <item>Los días no laborables de la semana NO son cierres: viven en <c>working-hours</c>.</item>
/// <item>Un día laborable por jornada que queda no laborable (feriado completo, cierre, ciclo de vida) es un cierre <c>full</c>.</item>
/// <item>Un día cuyos intervalos efectivos dejan fuera parte de la jornada (feriado reducido, jornada especial) es un cierre
/// <c>partial</c> por cada ventana contigua cerrada (complemento de lo efectivo dentro de la jornada); si el complemento cubre
/// toda la jornada se informa como <c>full</c>.</item>
/// <item>Un <c>DiaLaborableAdicional</c> (día de semana normalmente libre que se trabaja) y las ampliaciones de horario NO se pueden
/// expresar como cierres: quien necesite ese dato debe leer el calendario efectivo (<c>/calendario/efectivo</c>).</item>
/// <item>Si varias fuentes distintas cierran el mismo día (p. ej. feriado del anfitrión y del país propio) se emite un cierre por fuente.</item>
/// </list>
/// <para>
/// Fuente: excepciones de usuario por su <c>origen</c> (PaisAnfitrion→host, PaisPropio→home, Institucional→mission); ciclo de vida →
/// mission; feriados: ámbito Institucional u OrganismoInternacional → mission; si no, país del calendario == país de la localidad →
/// host; país del calendario == República Dominicana (<c>DO</c>) → home; en cualquier otro caso → host.
/// </para>
/// Ley 139-97 no se calcula: las entradas de los calendarios son fechas concretas.
/// </summary>
public static class CierresCalendario
{
    public const string CodigoPaisPropio = "DO";

    public static IReadOnlyList<CierreDia> Construir(
        CalendarioSnapshot calendario,
        Guid localidadPaisId,
        IReadOnlyDictionary<Guid, string> paisCodigos,
        IEnumerable<DiaEfectivo> dias)
    {
        var cierres = new List<CierreDia>();
        foreach (var dia in dias)
        {
            var jornada = IntervaloLaboral.Normalizar(dia.IntervalosJornada);
            if (jornada.Count == 0) continue;   // día no laborable de la semana (o día adicional): no es un cierre

            switch (dia.Fuente)
            {
                case FuenteDiaCalendario.CicloVida:
                    cierres.Add(new CierreDia(dia.Fecha, null, Razon(dia.Motivos.Select(m => m.Nombre)), FuenteCierre.Mission));
                    break;

                case FuenteDiaCalendario.Excepcion:
                    AgregarExcepcion(cierres, calendario, dia, jornada);
                    break;

                case FuenteDiaCalendario.Feriado:
                    AgregarFeriados(cierres, calendario, localidadPaisId, paisCodigos, dia, jornada);
                    break;
            }
        }

        return cierres
            .OrderBy(c => c.Fecha).ThenBy(c => c.Fuente).ThenBy(c => c.Ventana?.InicioMinuto ?? -1)
            .ToList();
    }

    /// <summary>Fuente de un feriado según el ámbito y el país de su calendario (ver la documentación del tipo).</summary>
    public static FuenteCierre FuenteDeFeriado(
        AmbitoCalendarioFeriados ambito, Guid? calendarioPaisId, Guid localidadPaisId, IReadOnlyDictionary<Guid, string> paisCodigos)
    {
        if (ambito is AmbitoCalendarioFeriados.Institucional or AmbitoCalendarioFeriados.OrganismoInternacional) return FuenteCierre.Mission;
        if (calendarioPaisId is not { } pais) return FuenteCierre.Host;
        if (pais == localidadPaisId) return FuenteCierre.Host;
        return paisCodigos.TryGetValue(pais, out var codigo) && string.Equals(codigo, CodigoPaisPropio, StringComparison.OrdinalIgnoreCase)
            ? FuenteCierre.Home
            : FuenteCierre.Host;
    }

    public static FuenteCierre FuenteDeExcepcion(OrigenExcepcionCalendario origen) => origen switch
    {
        OrigenExcepcionCalendario.PaisAnfitrion => FuenteCierre.Host,
        OrigenExcepcionCalendario.PaisPropio => FuenteCierre.Home,
        _ => FuenteCierre.Mission,
    };

    private static void AgregarExcepcion(List<CierreDia> cierres, CalendarioSnapshot cal, DiaEfectivo dia, List<IntervaloLaboral> jornada)
    {
        var motivo = dia.Motivos.FirstOrDefault();
        var ex = motivo?.ExcepcionId is { } id ? cal.Excepciones.FirstOrDefault(e => e.Id == id) : null;
        var fuente = ex is null ? FuenteCierre.Mission : FuenteDeExcepcion(ex.Origen);
        var razon = Razon(dia.Motivos.Select(m => m.Nombre));

        var cerrado = dia.Laborable ? Restar(jornada, dia.Intervalos) : jornada;
        Emitir(cierres, dia.Fecha, jornada, cerrado, razon, fuente);
    }

    private static void AgregarFeriados(
        List<CierreDia> cierres, CalendarioSnapshot cal, Guid localidadPaisId, IReadOnlyDictionary<Guid, string> paisCodigos,
        DiaEfectivo dia, List<IntervaloLaboral> jornada)
    {
        // Entradas que explican el día, con su fuente (el motivo del motor no trae los intervalos: se buscan en el snapshot).
        var entradas = new List<(FeriadoAplicable Feriado, FuenteCierre Fuente)>();
        foreach (var m in dia.Motivos)
        {
            var f = cal.Feriados.FirstOrDefault(x =>
                x.CalendarioFeriadosId == m.CalendarioFeriadosId && x.Fecha == dia.Fecha && x.Nombre == m.Nombre);
            if (f is null) continue;
            entradas.Add((f, FuenteDeFeriado(f.Ambito, f.PaisId, localidadPaisId, paisCodigos)));
        }
        if (entradas.Count == 0)
        {
            // Defensa: sin entradas reconocibles no hay forma de saber la ventana; se informa el cierre efectivo tal cual.
            var cerradoEfectivo = dia.Laborable ? Restar(jornada, dia.Intervalos) : jornada;
            Emitir(cierres, dia.Fecha, jornada, cerradoEfectivo, Razon(dia.Motivos.Select(m => m.Nombre)), FuenteCierre.Host);
            return;
        }

        // Si algún grupo cierra el día completo, el día entero está cerrado: solo se informan los grupos con día completo.
        var hayDiaCompleto = entradas.Any(e => e.Feriado.Alcance == AlcanceFeriado.DiaCompleto);

        foreach (var grupo in entradas.GroupBy(e => e.Fuente).OrderBy(g => g.Key))
        {
            var razon = Razon(grupo.Select(e => e.Feriado.Nombre));
            if (grupo.Any(e => e.Feriado.Alcance == AlcanceFeriado.DiaCompleto))
            {
                cierres.Add(new CierreDia(dia.Fecha, null, razon, grupo.Key));
                continue;
            }
            if (hayDiaCompleto) continue;

            // Solo jornadas reducidas: lo permitido es la intersección de los intervalos de cada entrada del grupo.
            List<IntervaloLaboral>? permitido = null;
            foreach (var e in grupo)
                permitido = permitido is null ? IntervaloLaboral.Normalizar(e.Feriado.Intervalos) : IntervaloLaboral.Interseccion(permitido, e.Feriado.Intervalos);
            Emitir(cierres, dia.Fecha, jornada, Restar(jornada, permitido ?? []), razon, grupo.Key);
        }
    }

    // Emite un cierre completo si lo cerrado cubre toda la jornada, o uno parcial por ventana contigua.
    private static void Emitir(List<CierreDia> cierres, DateOnly fecha, List<IntervaloLaboral> jornada, List<IntervaloLaboral> cerrado,
        string razon, FuenteCierre fuente)
    {
        var ventanas = IntervaloLaboral.Normalizar(cerrado);
        if (ventanas.Count == 0) return;
        if (ventanas.SequenceEqual(jornada))
        {
            cierres.Add(new CierreDia(fecha, null, razon, fuente));
            return;
        }
        foreach (var v in ventanas) cierres.Add(new CierreDia(fecha, v, razon, fuente));
    }

    /// <summary>Partes de <paramref name="a"/> que no están en <paramref name="b"/>.</summary>
    public static List<IntervaloLaboral> Restar(IEnumerable<IntervaloLaboral> a, IEnumerable<IntervaloLaboral> b)
    {
        var resultado = new List<IntervaloLaboral>();
        var restar = IntervaloLaboral.Normalizar(b);
        foreach (var x in IntervaloLaboral.Normalizar(a))
        {
            var cursor = x.InicioMinuto;
            foreach (var y in restar.Where(y => y.FinMinuto > x.InicioMinuto && y.InicioMinuto < x.FinMinuto).OrderBy(y => y.InicioMinuto))
            {
                if (y.InicioMinuto > cursor) resultado.Add(new IntervaloLaboral(cursor, y.InicioMinuto));
                cursor = Math.Max(cursor, y.FinMinuto);
            }
            if (cursor < x.FinMinuto) resultado.Add(new IntervaloLaboral(cursor, x.FinMinuto));
        }
        return resultado;
    }

    private static string Razon(IEnumerable<string> nombres)
    {
        var distintos = nombres.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.Ordinal).ToList();
        return distintos.Count == 0 ? "No laborable" : string.Join("; ", distintos);
    }
}
