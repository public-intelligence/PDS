using System.ComponentModel.DataAnnotations;
namespace PDS.Domain.Models;

/// <summary>
/// Producto del plan anual operativo (M5). Puede originarse del PEI (Pei) o ser operativo (Operational).
/// El código se genera en el servidor para evitar colisiones.
/// </summary>
public class PoaProduct
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }

    /// <summary>Código generado en servidor (ej. "PR-001"). Único por plan.</summary>
    [MaxLength(20)]
    public required string Code { get; set; }

    /// <summary>Pei | Operational</summary>
    [MaxLength(20)]
    public required string Origin { get; set; }

    // FK opcionales al PEI (solo si Origin == "Pei")
    public Guid? StrategicFocusId { get; set; }
    public Guid? StrategicObjectiveId { get; set; }
    public Guid? StrategicProductId { get; set; }

    [MaxLength(500)]
    public required string ProductName { get; set; }

    [MaxLength(2000)]
    public string? OperationalJustification { get; set; }

    [MaxLength(2000)]
    public string? Observation { get; set; }

    public int Order { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual AnnualPlan? Plan { get; set; }
    public virtual ICollection<PoaMeasureUnit> MeasureUnits { get; } = [];
    public virtual ICollection<PoaTask> Tasks { get; } = [];
    public virtual ICollection<PoaProductMunicipality> Municipalities { get; } = [];
    public virtual ICollection<PoaProductInvolvedArea> InvolvedAreas { get; } = [];
    public virtual ICollection<PoaProductCustomValue> CustomValues { get; } = [];
}
