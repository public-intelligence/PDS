using System;
using System.ComponentModel.DataAnnotations;

namespace PDS.Domain.Models;

/// <summary>
/// Bitácora de Reprogramación y Ajustes Motivados del Ciclo de Planificación (M4/M10).
/// Registra cualquier modificación efectuada sobre la estructura o marco financiero del ciclo una vez desplegado.
/// </summary>
public class PlanningCycleAmendmentLog
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public Guid CycleId { get; set; }

    /// <summary>CycleUpdate | CeilingAdjustment | AreaChange | TemplateChange</summary>
    [MaxLength(50)]
    public required string AmendmentType { get; set; }

    [MaxLength(2000)]
    public required string Justification { get; set; }

    public string? PreviousValuesJson { get; set; }
    public string? NewValuesJson { get; set; }

    public Guid CreatedBy { get; set; }
    [MaxLength(250)]
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }

    // ───────────── Navegación ─────────────
    public virtual PlanningCycle? Cycle { get; set; }
}
