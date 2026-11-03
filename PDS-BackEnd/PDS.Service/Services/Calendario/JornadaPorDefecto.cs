using PDS.Domain.Models;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario;

/// <summary>
/// RF-LO-26 / SPEC-LOC-CAL-01 §13.1 (OQ-09): horario estándar de partida cuando no hay otra jornada que copiar.
/// Lunes a viernes 08:00–16:00; sábado y domingo cerrados. Es un valor por defecto cambiable (constantes), no una regla de negocio fija.
/// </summary>
public static class JornadaPorDefecto
{
    public const int InicioMinuto = 8 * 60;
    public const int FinMinuto = 16 * 60;
    public const string Descripcion = "lunes a viernes 08:00-16:00";

    /// <summary>Semana ISO completa (1 = lunes … 7 = domingo): cinco días laborables y fin de semana sin intervalos.</summary>
    public static List<JornadaDia> LunesAViernes() =>
        Enumerable.Range(1, 7)
            .Select(dia => new JornadaDia
            {
                Dia = dia,
                Intervalos = dia <= 5 ? [new IntervaloLaboral(InicioMinuto, FinMinuto)] : [],
            })
            .ToList();

    /// <summary>Copia profunda de una semana (la jornada nueva no comparte listas con la de la localidad origen).</summary>
    public static List<JornadaDia> Copiar(IEnumerable<JornadaDia> semana) =>
        semana.Select(d => new JornadaDia
        {
            Dia = d.Dia,
            Intervalos = d.Intervalos.Select(i => new IntervaloLaboral(i.InicioMinuto, i.FinMinuto)).ToList(),
        }).ToList();
}
