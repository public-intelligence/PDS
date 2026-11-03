using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Área organizacional involucrada en un producto estratégico del PEI (M3).
/// FK al organigrama + nombre denormalizado (snapshot): el dato histórico sobrevive
/// a reestructuraciones.
/// </summary>
public class StrategicProductInvolvedArea
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    public Guid AreaId { get; set; }

    [MaxLength(250)]
    public string? AreaName { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StrategicProduct? Product { get; set; }
}
