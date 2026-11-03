using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Unidad de medida de un producto estratégico del PEI (M3), con su área responsable
/// (siempre un ÁREA del organigrama, nunca una persona) y metas anuales normalizadas
/// en <see cref="StrategicAnnualTarget"/>. Con historia se descontinúa, no se elimina.
/// </summary>
public class StrategicMeasureUnit
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    /// <summary>Identidad estable entre versiones (se conserva al clonar la enmienda).</summary>
    public Guid LineageId { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    /// <summary>Área del organigrama responsable del logro (FK + nombre snapshot).</summary>
    public Guid? ResponsibleAreaId { get; set; }

    [MaxLength(250)]
    public string? ResponsibleAreaName { get; set; }

    /// <summary>Año a partir del cual la unidad deja de medirse.</summary>
    public int? DiscontinuedFromYear { get; set; }

    [MaxLength(1000)]
    public string? DiscontinuationReason { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StrategicProduct? Product { get; set; }
    public virtual ICollection<StrategicAnnualTarget> AnnualTargets { get; } = [];
}
