namespace PDS.Domain.Models;

/// <summary>Suscripción de una localidad a un calendario de feriados, con vigencia opcional (RF-LO-30).</summary>
public class LocalidadCalendarioFeriados
{
    public Guid Id { get; set; }
    public Guid LocalidadId { get; set; }
    public Guid CalendarioFeriadosId { get; set; }
    /// <summary>Nulo = sin límite inferior.</summary>
    public DateOnly? VigenteDesde { get; set; }
    /// <summary>Nulo = sin límite superior.</summary>
    public DateOnly? VigenteHasta { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreadoPorId { get; set; }
    public string? CreadoPor { get; set; }

    public Localidad? Localidad { get; set; }
    public CalendarioFeriados? CalendarioFeriados { get; set; }
}
