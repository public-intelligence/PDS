using PDS.Domain.Enums;

namespace PDS.Domain.Models;

/// <summary>
/// Feed de cambios del calendario (RF-LO-43), append-only. El <see cref="Id"/> (identity) es el cursor que usan los
/// consumidores M2M. Se escribe una fila por localidad afectada, en la misma transacción que el cambio.
/// </summary>
public class CalendarioCambio
{
    public long Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid LocalidadId { get; set; }
    /// <summary>Versión del calendario de la localidad DESPUÉS del cambio.</summary>
    public long CalendarioVersion { get; set; }
    public MotivoCambioCalendario Motivo { get; set; }
    /// <summary>Rango de fechas afectado; nulo = sin límite.</summary>
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public DateTime OcurridoEn { get; set; }

    public Localidad? Localidad { get; set; }
}
