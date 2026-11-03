using System.Globalization;

namespace PDS.Domain.ValueObjects;

/// <summary>
/// Intervalo laboral [<see cref="InicioMinuto"/>, <see cref="FinMinuto"/>) en minutos desde la medianoche local
/// (SPEC-LOC-CAL-01 §9.4): 0 ≤ inicio &lt; fin ≤ 1440. La API usa <c>"HH:mm"</c>; <c>"24:00"</c> solo es válido como fin.
/// Es el tipo que EF persiste como JSON (owned type con <c>ToJson()</c>).
/// </summary>
public sealed record IntervaloLaboral(int InicioMinuto, int FinMinuto)
{
    public const int MinutosPorDia = 1440;

    public int DuracionMinutos => FinMinuto - InicioMinuto;

    /// <summary>0 ≤ inicio &lt; fin ≤ 1440.</summary>
    public bool EsValido => InicioMinuto >= 0 && InicioMinuto < FinMinuto && FinMinuto <= MinutosPorDia;

    public bool Overlaps(IntervaloLaboral otro) => InicioMinuto < otro.FinMinuto && otro.InicioMinuto < FinMinuto;

    public bool Contains(int minuto) => minuto >= InicioMinuto && minuto < FinMinuto;

    public override string ToString() => $"{FormatMinuto(InicioMinuto)}-{FormatMinuto(FinMinuto)}";

    /// <summary>Formatea minutos como <c>HH:mm</c> (1440 → <c>24:00</c>).</summary>
    public static string FormatMinuto(int minuto) => $"{minuto / 60:00}:{minuto % 60:00}";

    /// <summary>Convierte <c>HH:mm</c> a minutos. <c>24:00</c> solo se acepta con <paramref name="permitir24"/> (fin).</summary>
    public static bool TryParseMinuto(string? valor, bool permitir24, out int minuto)
    {
        minuto = 0;
        if (valor is null || valor.Length != 5 || valor[2] != ':') return false;
        if (!int.TryParse(valor.AsSpan(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var h) ||
            !int.TryParse(valor.AsSpan(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var m) || m > 59)
            return false;
        if (h == 24 && m == 0 && permitir24) { minuto = MinutosPorDia; return true; }
        if (h > 23) return false;
        minuto = h * 60 + m;
        return true;
    }

    /// <summary>Convierte un par <c>HH:mm</c> a intervalo; falla si el formato es inválido o inicio ≥ fin.</summary>
    public static bool TryParse(string? inicio, string? fin, out IntervaloLaboral intervalo)
    {
        intervalo = new IntervaloLaboral(0, 0);
        if (!TryParseMinuto(inicio, false, out var i) || !TryParseMinuto(fin, true, out var f)) return false;
        var candidato = new IntervaloLaboral(i, f);
        if (!candidato.EsValido) return false;
        intervalo = candidato;
        return true;
    }

    /// <summary>Verdadero si algún par de intervalos se solapa (contiguos no se solapan).</summary>
    public static bool HaySolapamiento(IEnumerable<IntervaloLaboral> intervalos)
    {
        var orden = intervalos.OrderBy(i => i.InicioMinuto).ToList();
        for (var k = 1; k < orden.Count; k++)
            if (orden[k].InicioMinuto < orden[k - 1].FinMinuto) return true;
        return false;
    }

    /// <summary>Ordena y fusiona intervalos solapados o contiguos.</summary>
    public static List<IntervaloLaboral> Normalizar(IEnumerable<IntervaloLaboral> intervalos)
    {
        var resultado = new List<IntervaloLaboral>();
        foreach (var i in intervalos.OrderBy(x => x.InicioMinuto))
        {
            if (resultado.Count > 0 && i.InicioMinuto <= resultado[^1].FinMinuto)
                resultado[^1] = resultado[^1] with { FinMinuto = Math.Max(resultado[^1].FinMinuto, i.FinMinuto) };
            else
                resultado.Add(i);
        }
        return resultado;
    }

    /// <summary>Intersección de dos conjuntos de intervalos (cada uno se normaliza antes).</summary>
    public static List<IntervaloLaboral> Interseccion(IEnumerable<IntervaloLaboral> a, IEnumerable<IntervaloLaboral> b)
    {
        var resultado = new List<IntervaloLaboral>();
        var segundo = Normalizar(b);
        foreach (var x in Normalizar(a))
            foreach (var y in segundo)
            {
                var inicio = Math.Max(x.InicioMinuto, y.InicioMinuto);
                var fin = Math.Min(x.FinMinuto, y.FinMinuto);
                if (inicio < fin) resultado.Add(new IntervaloLaboral(inicio, fin));
            }
        return Normalizar(resultado);
    }
}
