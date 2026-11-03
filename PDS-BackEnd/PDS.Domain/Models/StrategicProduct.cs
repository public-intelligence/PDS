using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Producto estratégico del PEI (M3). Un producto con historia (metas comprometidas en años
/// transcurridos) no se elimina: se DESCONTINÚA desde un año futuro y sus metas pasadas
/// siguen siendo consultables.
/// </summary>
public class StrategicProduct
{
    public Guid Id { get; set; }
    public Guid ObjectiveId { get; set; }

    /// <summary>Identidad estable entre versiones (se conserva al clonar la enmienda).</summary>
    public Guid LineageId { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    /// <summary>Año a partir del cual el producto deja de medirse (siempre futuro al descontinuar).</summary>
    public int? DiscontinuedFromYear { get; set; }

    [MaxLength(1000)]
    public string? DiscontinuationReason { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StrategicObjective? Objective { get; set; }
    public virtual ICollection<StrategicMeasureUnit> MeasureUnits { get; } = [];
    public virtual ICollection<StrategicProductInvolvedArea> InvolvedAreas { get; } = [];
}
