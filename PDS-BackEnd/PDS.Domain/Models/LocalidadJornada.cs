using PDS.Domain.ValueObjects;

namespace PDS.Domain.Models;

/// <summary>
/// Versión de la jornada semanal de una localidad (RF-LO-24, RF-LO-25). La versión vigente el día <i>d</i> es la de mayor
/// <see cref="VigenteDesde"/> ≤ <i>d</i>. Las versiones ya vigentes son inmutables; las futuras se eliminan físicamente.
/// </summary>
public class LocalidadJornada
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public DateOnly VigenteDesde { get; set; }

    /// <summary>Semana en JSON (owned type): un elemento por día ISO (1 = lunes). Un día sin intervalos es no laborable.</summary>
    public List<JornadaDia> Semana { get; set; } = new();

    /// <summary>Opcional; obligatorio cuando un administrador retrodata la versión.</summary>
    public string? Motivo { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreadoPorId { get; set; }
    public string? CreadoPor { get; set; }

    public Localidad? Localidad { get; set; }
}

/// <summary>Día de la semana (ISO 1..7) de una jornada con sus intervalos laborales locales.</summary>
public class JornadaDia
{
    public int Dia { get; set; }
    public List<IntervaloLaboral> Intervalos { get; set; } = new();
}
