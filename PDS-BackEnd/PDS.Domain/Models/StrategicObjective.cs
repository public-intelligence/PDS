using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Objetivo estratégico del PEI (M3). El código "OE {foco}.{n}" lo genera el servidor
/// según la posición dentro del foco.
/// </summary>
public class StrategicObjective
{
    public Guid Id { get; set; }
    public Guid FocusId { get; set; }

    /// <summary>Identidad estable entre versiones (se conserva al clonar la enmienda).</summary>
    public Guid LineageId { get; set; }

    /// <summary>Código visible, ej. "OE 1.2".</summary>
    [MaxLength(20)]
    public required string Code { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StrategicFocus? Focus { get; set; }
    public virtual ICollection<StrategicProduct> Products { get; } = [];
}
