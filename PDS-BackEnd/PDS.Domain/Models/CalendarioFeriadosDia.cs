using PDS.Domain.Enums;
using PDS.Domain.ValueObjects;

namespace PDS.Domain.Models;

/// <summary>
/// Entrada (fecha concreta) de un calendario de feriados (RF-LO-28). PDS no calcula feriados móviles: cada fecha se
/// carga tal como se publica oficialmente. Para <see cref="AlcanceFeriado.JornadaReducida"/>,
/// <see cref="Intervalos"/> son los intervalos laborales que QUEDAN ABIERTOS ese día.
/// </summary>
public class CalendarioFeriadosDia : ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid CalendarioFeriadosId { get; set; }
    public DateOnly Fecha { get; set; }
    public required string Nombre { get; set; }
    public AlcanceFeriado Alcance { get; set; }
    /// <summary>JSON; nulo salvo en <see cref="AlcanceFeriado.JornadaReducida"/>.</summary>
    public List<IntervaloLaboral>? Intervalos { get; set; }
    public string? ReferenciaNormativa { get; set; }
    public string? Notas { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public byte[]? RowVersion { get; set; }

    public CalendarioFeriados? CalendarioFeriados { get; set; }
}
