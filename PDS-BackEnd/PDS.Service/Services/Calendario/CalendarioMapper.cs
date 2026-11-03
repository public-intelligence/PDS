using PDS.Domain.DTOs.Calendario;
using PDS.Domain.ValueObjects;

namespace PDS.Service.Services.Calendario;

/// <summary>Resultado de convertir intervalos <c>HH:mm</c> de la API a <see cref="IntervaloLaboral"/>.</summary>
/// <param name="Valores">Intervalos válidos, ordenados por inicio (vacío si hubo error).</param>
/// <param name="ErrorFormato">Hora con formato inválido (clave de error <c>intervalos</c>).</param>
/// <param name="ErrorSemantico">Inicio ≥ fin o intervalos solapados (clave de error propia de cada recurso).</param>
public sealed record IntervalosParseados(IReadOnlyList<IntervaloLaboral> Valores, string? ErrorFormato, string? ErrorSemantico)
{
    public bool EsValido => ErrorFormato is null && ErrorSemantico is null;
}

/// <summary>Conversión entre intervalos de la API (<c>HH:mm</c>) y del dominio (minutos), y tokens de día de la semana.</summary>
public static class CalendarioMapper
{
    /// <summary>Tokens de la API para los días ISO 1..7 (SPEC-LOC-CAL-01 §9.5).</summary>
    public static readonly IReadOnlyList<string> TokensDia = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"];

    public static int? DiaDesdeToken(string? token)
    {
        var i = TokensDia.ToList().FindIndex(t => string.Equals(t, token?.Trim(), StringComparison.OrdinalIgnoreCase));
        return i < 0 ? null : i + 1;
    }

    public static string TokenDeDia(int diaIso) => TokensDia[diaIso - 1];

    public static List<IntervaloDto> ToDtos(IEnumerable<IntervaloLaboral>? intervalos) =>
        (intervalos ?? []).OrderBy(i => i.InicioMinuto)
            .Select(i => new IntervaloDto(IntervaloLaboral.FormatMinuto(i.InicioMinuto), IntervaloLaboral.FormatMinuto(i.FinMinuto)))
            .ToList();

    public static IntervalosParseados ParseIntervalos(IEnumerable<IntervaloDto>? dtos)
    {
        var lista = new List<IntervaloLaboral>();
        foreach (var dto in dtos ?? [])
        {
            if (dto is null ||
                !IntervaloLaboral.TryParseMinuto(dto.Inicio, false, out var inicio) ||
                !IntervaloLaboral.TryParseMinuto(dto.Fin, true, out var fin))
                return new([], $"Formato de hora inválido en el intervalo «{dto?.Inicio}»-«{dto?.Fin}»; use HH:mm (24:00 solo como fin).", null);

            var candidato = new IntervaloLaboral(inicio, fin);
            if (!candidato.EsValido)
                return new([], null, "La hora de fin debe ser posterior a la de inicio.");
            lista.Add(candidato);
        }

        if (IntervaloLaboral.HaySolapamiento(lista))
            return new([], null, "Los intervalos de un mismo día no pueden solaparse.");
        return new(lista.OrderBy(i => i.InicioMinuto).ToList(), null, null);
    }
}
