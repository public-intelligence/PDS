using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Plantilla versionable del plan anual (M2). Cada organización puede tener múltiples
/// plantillas (ej: "Formulario Cadena de Valor DPD-FO-01"), pero solo una activa a la vez.
/// Las columnas configurables del wizard pertenecen a la plantilla.
/// </summary>
public class PlanningTemplate : ISoftDeletable
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Versión incremental de la plantilla (permite evolucionar sin perder el histórico).</summary>
    public int Version { get; set; } = 1;

    /// <summary>Solo una plantilla activa por organización; la activación desactiva la anterior.</summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<PlanningTemplateColumn> Columns { get; } = [];
}
