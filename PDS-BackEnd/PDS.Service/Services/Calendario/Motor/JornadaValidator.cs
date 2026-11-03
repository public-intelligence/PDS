using PDS.Domain.Models;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario.Motor;

/// <summary>
/// RF-LO-24: reglas de la jornada semanal. Devuelve mensajes en español (clave de error <c>jornada</c>); lista vacía = válida.
/// </summary>
public static class JornadaValidator
{
    public const int MaxIntervalosPorDia = 4;

    public static IReadOnlyList<string> Validar(IEnumerable<JornadaDia> semana)
    {
        var dias = semana.ToList();
        var errores = new List<string>();
        foreach (var g in dias.GroupBy(d => d.Dia).Where(g => g.Count() > 1))
            errores.Add($"El día {g.Key} está repetido en la jornada.");
        errores.AddRange(Validar(dias.GroupBy(d => d.Dia).ToDictionary(g => g.Key, g => (IReadOnlyList<IntervaloLaboral>)g.SelectMany(d => d.Intervalos).ToList())));
        return errores;
    }

    public static IReadOnlyList<string> Validar(IReadOnlyDictionary<int, IReadOnlyList<IntervaloLaboral>> semana)
    {
        var errores = new List<string>();
        foreach (var (dia, intervalos) in semana.OrderBy(kv => kv.Key))
        {
            if (dia is < 1 or > 7)
            {
                errores.Add($"El día {dia} no es válido; use 1 (lunes) a 7 (domingo).");
                continue;
            }
            if (intervalos.Count > MaxIntervalosPorDia)
                errores.Add($"El día {dia} tiene {intervalos.Count} intervalos; el máximo es {MaxIntervalosPorDia}.");
            foreach (var i in intervalos.Where(i => !i.EsValido))
                errores.Add($"Intervalo inválido en el día {dia} ({IntervaloLaboral.FormatMinuto(i.InicioMinuto)}-{IntervaloLaboral.FormatMinuto(i.FinMinuto)}): el inicio debe ser anterior al fin y el fin no puede pasar de 24:00.");
            if (intervalos.All(i => i.EsValido) && IntervaloLaboral.HaySolapamiento(intervalos))
                errores.Add($"Los intervalos del día {dia} se solapan.");
        }
        if (errores.Count == 0 && semana.Values.All(v => v.Count == 0))
            errores.Add("La jornada debe tener al menos un intervalo laboral en la semana.");
        return errores;
    }

    /// <summary>RF-LO-25: la versión vigente el día <paramref name="fecha"/> es la de mayor <c>VigenteDesde</c> ≤ fecha (o nula si ninguna).</summary>
    public static JornadaVersion? VersionVigente(IEnumerable<JornadaVersion> versiones, DateOnly fecha) =>
        versiones.Where(v => v.VigenteDesde <= fecha).OrderByDescending(v => v.VigenteDesde).FirstOrDefault();
}
