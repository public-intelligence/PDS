using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Foco estratégico del PEI (M3): primer nivel del árbol Foco → Objetivo → Producto → Unidad.
/// </summary>
public class StrategicFocus
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }

    /// <summary>
    /// Identidad estable entre versiones del PEI: se conserva al clonar la enmienda
    /// (patrón SourceXId del versionado de estructura). Permite comparar versiones y
    /// re-apuntar referencias externas.
    /// </summary>
    public Guid LineageId { get; set; }

    [MaxLength(500)]
    public required string Name { get; set; }

    /// <summary>Alineación con la Estrategia Nacional de Desarrollo (ej. "END 2030 — Eje 1: …").</summary>
    [MaxLength(500)]
    public string? EndAlignment { get; set; }

    public int Order { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual StrategicPlan? Plan { get; set; }
    public virtual ICollection<StrategicObjective> Objectives { get; } = [];
}
